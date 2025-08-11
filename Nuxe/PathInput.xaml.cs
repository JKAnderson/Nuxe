using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Nuxe;

public partial class PathInput : UserControl
{
    public enum PathInputMode
    {
        OpenFile,
        SaveFile,
        OpenFolder
    }

    public PathInputMode Mode
    {
        get => (PathInputMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(PathInputMode), typeof(PathInput));

    public string Path
    {
        get => (string)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(string), typeof(PathInput));

    public string Prompt
    {
        get => (string)GetValue(PromptProperty);
        set => SetValue(PromptProperty, value);
    }
    public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(nameof(Prompt), typeof(string), typeof(PathInput));

    public string FileFilter
    {
        get => (string)GetValue(FileFilterProperty);
        set => SetValue(FileFilterProperty, value);
    }
    public static readonly DependencyProperty FileFilterProperty = DependencyProperty.Register(nameof(FileFilter), typeof(string), typeof(PathInput));

    public PathInput()
    {
        InitializeComponent();
    }

    private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            switch (Mode)
            {
                case PathInputMode.OpenFile: BrowseOpenFile(); break;
                case PathInputMode.SaveFile: BrowseSaveFile(); break;
                case PathInputMode.OpenFolder: BrowseOpenFolder(); break;
                default: throw new InvalidOperationException($"Invalid PathInput Mode={Mode}");
            }
        }
        catch (Exception ex)
        {
            Common.DisplayError(ex);
        }
    }

    private void BrowseOpenFile()
    {
        string dir = GetLastExistingDir(Path);
        var dialog = new OpenFileDialog()
        {
            CheckFileExists = true,
            CheckPathExists = true,
            FileName = System.IO.Path.GetFileName(Path),
            Filter = FileFilter,
            InitialDirectory = dir,
            Title = Prompt,
            ValidateNames = true,
        };

        bool? result = dialog.ShowDialog();
        if (result.GetValueOrDefault(false))
        {
            Path = dialog.FileName;
        }
    }

    private void BrowseSaveFile()
    {
        string dir = GetLastExistingDir(Path);
        var dialog = new SaveFileDialog()
        {
            CheckPathExists = true,
            FileName = System.IO.Path.GetFileName(Path),
            Filter = FileFilter,
            InitialDirectory = dir,
            Title = Prompt,
            ValidateNames = true,
        };

        bool? result = dialog.ShowDialog();
        if (result.GetValueOrDefault(false))
        {
            Path = dialog.FileName;
        }
    }

    private void BrowseOpenFolder()
    {
        string dir = GetLastExistingDir(Path);
        var dialog = new OpenFolderDialog()
        {
            FolderName = dir,
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
        try
        {
            string dir = GetLastExistingDir(Path);
            Process.Start("explorer.exe", dir);
        }
        catch (Exception ex)
        {
            Common.DisplayError(ex);
        }
    }

    private static string GetLastExistingDir(string path)
    {
        string dir = path;
        while (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            dir = System.IO.Path.GetDirectoryName(dir);
        return dir;
    }
}
