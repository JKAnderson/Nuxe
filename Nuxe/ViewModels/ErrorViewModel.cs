using System.Windows;

namespace Nuxe.ViewModels;

internal class ErrorViewModel
{
    public string Message { get; }
    public string Details { get; }
    public Visibility DetailsVisibility => Details == null ? Visibility.Collapsed : Visibility.Visible;

    public ErrorViewModel(string message, string details = null)
    {
        Message = message;
        Details = details;
    }
}
