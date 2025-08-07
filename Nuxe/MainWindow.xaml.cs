using Microsoft.Win32;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Nuxe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static string TitleText => $"Nuxe {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";

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

    private void Window_Closed(object sender, EventArgs e)
    {
        ProgressTimer.Stop();
    }

    private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            InitialDirectory = TextBoxGameDir.Text,
            Title = "Select game directory",
            ValidateNames = true,
        };

        bool? result = dialog.ShowDialog();
        if (result.GetValueOrDefault(false))
        {
            TextBoxGameDir.Text = dialog.FolderName;
        }
    }

    private void ButtonExplore_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("explorer", TextBoxGameDir.Text);
    }

    private void ButtonAbort_Click(object sender, RoutedEventArgs e)
    {
        OperationCancellation.Cancel();
    }

    private async Task RunOperation(Operation operation, string operationVerb)
    {
        GroupBoxControls.IsEnabled = false;
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
            OperationProgress.Report(new(1, $"{operationVerb} completed in {sw}!"));
        }
        catch (OperationCanceledException)
        {
            OperationProgress.Report(new(0, $"{operationVerb} aborted."));
        }
        catch (Exception ex)
        {
            SystemSounds.Hand.Play();
            OperationProgress.Report(new(0, $"{operationVerb} failed."));
            MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        GroupBoxControls.IsEnabled = true;
        ButtonAbort.IsEnabled = false;
    }

    private async void ButtonUnpack_Click(object sender, RoutedEventArgs e)
    {
        var operation = new UnpackOperation(TextBoxGameDir.Text);
        await RunOperation(operation, "Unpacking");
    }

    private async void ButtonPatch_Click(object sender, RoutedEventArgs e)
    {
        var operation = new PatchOperation(TextBoxGameDir.Text);
        await RunOperation(operation, "Patching");
    }

    private async void ButtonRestore_Click(object sender, RoutedEventArgs e)
    {
        var operation = new RestoreOperation(TextBoxGameDir.Text);
        await RunOperation(operation, "Restoration");
    }
}
