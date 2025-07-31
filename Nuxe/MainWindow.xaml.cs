using System.Reflection;
using System.Windows;

namespace Nuxe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static string TitleText => $"Nuxe {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";

    public MainWindow()
    {
        InitializeComponent();
    }
}
