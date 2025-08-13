using System.Windows;

namespace Nuxe;

/// <summary>
/// Interaction logic for ErrorWindow.xaml
/// </summary>
public partial class ErrorWindow : Window
{
    public ErrorWindow(string message, string details = null)
    {
        InitializeComponent();
        TextBoxMessage.Text = message;
        TextBoxDetails.Text = details;
        TextBoxDetails.Visibility = details == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
