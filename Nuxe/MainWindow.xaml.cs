using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace Nuxe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
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
            DisplayError(ex);
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

    private async void ButtonUnpack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string gameDir = State.GameDirectory;
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            var operation = new UnpackOperation(State.ResDir, gameDir, gameConfig);
            await RunOperation(operation, "Unpacking");
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private async void ButtonPatch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string gameDir = State.GameDirectory;
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            var operation = new PatchOperation(gameDir);
            await RunOperation(operation, "Patching");
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private async void ButtonRestore_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string gameDir = State.GameDirectory;
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            var operation = new RestoreOperation(gameDir);
            await RunOperation(operation, "Restoration");
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private static void DisplayError(Exception ex)
    {
        MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task RunOperation(Operation operation, string operationVerb)
    {
        TabControlSettings.IsEnabled = false;
        ButtonAbort.IsEnabled = true;
        try
        {
            using var ctSource = new CancellationTokenSource();
            OperationCancellation = ctSource;
            var sw = new Stopwatch();
            sw.Start();
            await Task.Run(() => operation.Run(OperationProgress, OperationCancellation.Token));

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
            SystemSounds.Hand.Play();
            OperationProgress.Report(new(0, $"{operationVerb} failed."));
            DisplayError(ex);
        }
        TabControlSettings.IsEnabled = true;
        ButtonAbort.IsEnabled = false;
    }
}
