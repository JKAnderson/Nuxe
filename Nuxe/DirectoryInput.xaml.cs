using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Nuxe;

/// <summary>
/// Interaction logic for DirectoryInput.xaml
/// </summary>
public partial class DirectoryInput : UserControl
{
    public string Prompt
    {
        get => (string)GetValue(PromptProperty);
        set => SetValue(PromptProperty, value);
    }
    public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(nameof(Prompt), typeof(string), typeof(DirectoryInput));

    public string Path
    {
        get => (string)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(string), typeof(DirectoryInput));

    public DirectoryInput()
    {
        InitializeComponent();
    }

    private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            InitialDirectory = Path,
            Title = Prompt,
            ValidateNames = true,
        };

        bool? result = dialog.ShowDialog();
        if (result.GetValueOrDefault(false))
        {
            Path = dialog.FolderName;
        }
    }

    private void ButtonExplore_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("explorer", Path);
    }
}
