using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nuxe;

internal class MainWindowState : INotifyPropertyChanged
{
    private static Properties.Settings Settings => Properties.Settings.Default;

    public string TitleText { get; }
    public string ResDir { get; }
    public GameConfig[] GameConfigs { get; }

    public MainWindowState()
    {
        TitleText = $"Nuxe {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";
        ResDir = Path.GetFullPath(Environment.GetEnvironmentVariable("NUXE_RES_DIR") ?? "res");
        GameConfigs = GameConfig.LoadGameConfigs(ResDir);

        if (!Settings.Upgraded)
        {
            Settings.Upgrade();
            Settings.Upgraded = true;
        }

        GameDirectory = Settings.GameDirectory;
    }

    public void Save()
    {
        Settings.GameDirectory = GameDirectory;
        Settings.Save();
    }

    private string _gameDirectory;
    public string GameDirectory
    {
        get => _gameDirectory;
        set => ChangeProperty(ref _gameDirectory, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ChangeProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
