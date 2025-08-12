using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace Nuxe;

public partial class MainWindow : Window
{
    internal MainWindowState State { get; set; }

    private IProgress<OperationProgress> OperationProgress { get; }
    private CancellationTokenSource OperationCancellation { get; set; }
    private OperationProgress LastProgress { get; set; }
    private DispatcherTimer ProgressTimer { get; }

    public MainWindow()
    {
        InitializeComponent();

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
            State = new();
            DataContext = State;
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
        OperationCancellation.Cancel();
    }

    private async void ButtonBasicUnpack_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Unpacking", () =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe);
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new UnpackOperation(State.ResDir, gameDir, gameConfig, null, null, false, false);
        });
    }

    private async void ButtonAdvancedUnpack_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Unpacking", () =>
        {
            if (State.ManualGame == null)
                throw new FriendlyException("Game type must be selected manually in advanced mode.");
            string unpackDir = State.UseUnpackDir ? State.UnpackDir : null;
            string unpackFilter = State.UseUnpackFilter ? State.UnpackFilter : null;
            return new UnpackOperation(State.ResDir, State.GameDir, State.ManualGame, unpackDir, unpackFilter, State.UnpackOverwrite, true);
        });
    }

    private async void ButtonBasicPatch_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Patching", () =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe);
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new PatchOperation(gameDir);
        });
    }

    private async void ButtonBasicRestore_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Restoration", () =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe);
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new RestoreOperation(gameDir, gameConfig);
        });
    }

    private async void ButtonAdvancedRestore_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Restoration", () =>
        {
            if (State.ManualGame == null)
                throw new FriendlyException("Game type must be selected manually in advanced mode.");
            return new RestoreOperation(State.GameDir, State.ManualGame);
        });
    }

    private async Task RunOperation(string operationVerb, Func<Operation> createOperation)
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

            await Task.Run(() => createOperation().Run(OperationProgress, OperationCancellation.Token));

            sw.Stop();
            SystemSounds.Beep.Play();
#if DEBUG
            OperationProgress.Report(new(1, $"{operationVerb} completed in {sw.Elapsed:hh\\:mm\\:ss}!"));
#else
            OperationProgress.Report(new(1, $"{operationVerb} completed!"));
#endif
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
    }
}
