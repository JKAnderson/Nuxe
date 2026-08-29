using Nuxe.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace Nuxe.Views;

public partial class MainWindow : Window
{
    private MainViewModel State { get; set; }
    private IProgress<OperationProgress> OperationProgress { get; }
    private CancellationTokenSource? OperationCancellation { get; set; }
    private OperationProgress LastProgress { get; set; }
    private DispatcherTimer ProgressTimer { get; }

    public MainWindow()
    {
        InitializeComponent();

        State = new();
        DataContext = State;

        OperationProgress = new Progress<OperationProgress>(r => LastProgress = r);
        LastProgress = new(0, "");
        ProgressTimer = new() { Interval = TimeSpan.FromSeconds(1.0 / 30) };
        ProgressTimer.Tick += (sender, e) =>
        {
            var progress = LastProgress;
            ProgressBar.Value = progress.Value * 100;
            TextBoxStatus.Text = progress.Message;
            TaskbarItemInfo.ProgressValue = progress.Value;
        };
        ProgressTimer.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            State.Load();
        }
        catch (Exception ex)
        {
            Common.DisplayError(ex);
            Close();
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        ProgressTimer.Stop();
        State.Save();
    }

    private void ButtonAbort_Click(object sender, RoutedEventArgs e)
    {
        ButtonAbort.IsEnabled = false;
        OperationCancellation?.Cancel();
    }

    private async void ButtonBasicUnpack_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Unpacking", (progress, token) =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe) ?? throw new ArgumentException($"Malformed exe path: {State.GameExe}");
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new UnpackOperation(State.ResDir, gameDir, gameConfig, null, null, false, false) { Progress = progress, CancellationToken = token };
        });
    }

    private async void ButtonAdvancedUnpack_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Unpacking", (progress, token) =>
        {
            if (State.ManualGame == null)
                throw new FriendlyException("Game type must be selected manually in advanced mode.");

            string? unpackDir = State.UseUnpackDir ? State.UnpackDir : null;
            string? unpackFilter = State.UseUnpackFilter ? State.UnpackFilter : null;
            return new UnpackOperation(State.ResDir, State.GameDir, State.ManualGame, unpackDir, unpackFilter, State.UnpackOverwrite, true) { Progress = progress, CancellationToken = token };
        });
    }

    private async void ButtonBasicPatch_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Patching", (progress, token) =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe) ?? throw new ArgumentException($"Malformed exe path: {State.GameExe}");
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new PatchOperation(State.GameExe, gameConfig, null) { Progress = progress, CancellationToken = token };
        });
    }

    private async void ButtonAdvancedPatch_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Patching", (progress, token) =>
        {
            if (State.ManualGame == null)
                throw new FriendlyException("Game type must be selected manually in advanced mode.");

            string? outputPath = State.UsePatchOutputPath ? State.PatchOutputPath : null;
            return new PatchOperation(State.GameExe, State.ManualGame, outputPath) { Progress = progress, CancellationToken = token };
        });
    }

    private async void ButtonBasicRestore_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Restoration", (progress, token) =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe) ?? throw new ArgumentException($"Malformed exe path: {State.GameExe}");
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new RestoreOperation(gameDir, gameConfig) { Progress = progress, CancellationToken = token };
        });
    }

    private async void ButtonAdvancedRestore_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Restoration", (progress, token) =>
        {
            if (State.ManualGame == null)
                throw new FriendlyException("Game type must be selected manually in advanced mode.");

            return new RestoreOperation(State.GameDir, State.ManualGame) { Progress = progress, CancellationToken = token };
        });
    }

    private async void ButtonAdvancedDecrypt_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Decrypt", (progress, token) =>
        {
            if (State.ManualGame == null)
                throw new FriendlyException("Game type must be selected manually in advanced mode.");

            return new DecryptOperation(State.ResDir, State.GameDir, State.ManualGame) { Progress = progress, CancellationToken = token };
        });
    }

    private async Task RunOperation(string operationVerb, Func<IProgress<OperationProgress>, CancellationToken, Operation> createOperation)
    {
        // Keep this up here so it doesn't dispose before the Abort button is disabled
        using var ctSource = new CancellationTokenSource();
        OperationCancellation = ctSource;

        TabControlSettings.IsEnabled = false;
        ButtonAbort.IsEnabled = true;
        try
        {
            var sw = new Stopwatch();
            sw.Start();

            await Task.Run(() => createOperation(OperationProgress, OperationCancellation.Token).Run());

            sw.Stop();
            var elapsed = sw.Elapsed + TimeSpan.FromSeconds(1); // :3c
            OperationProgress.Report(new(1, $"{operationVerb} completed in {elapsed:mm\\mss\\s}!"));
            SystemSounds.Beep.Play();
        }
        catch (OperationCanceledException)
        {
            OperationProgress.Report(new(0, $"{operationVerb} aborted."));
        }
        catch (Exception ex)
        {
            OperationProgress.Report(new(0, $"{operationVerb} failed."));
            Common.DisplayError(ex);
        }
        TabControlSettings.IsEnabled = true;
        ButtonAbort.IsEnabled = false;
        OperationCancellation = null;
    }
}
