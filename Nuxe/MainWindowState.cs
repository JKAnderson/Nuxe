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

        GameDir = Settings.GameDir;
        ManualGame = Array.Find(GameConfigs, config => config.BinderKeysName == Settings.ManualGame);
        UseUnpackDir = Settings.UseUnpackDir;
        UnpackDir = Settings.UnpackDir;
        UseUnpackFilter = Settings.UseUnpackFilter;
        UnpackFilter = Settings.UnpackFilter;
        UnpackOverwrite = Settings.UnpackOverwrite;
    }

    public void Save()
    {
        Settings.GameDir = GameDir;
        Settings.ManualGame = ManualGame?.BinderKeysName;
        Settings.UseUnpackDir = UseUnpackDir;
        Settings.UnpackDir = UnpackDir;
        Settings.UseUnpackFilter = UseUnpackFilter;
        Settings.UnpackFilter = UnpackFilter;
        Settings.UnpackOverwrite = UnpackOverwrite;
        Settings.Save();
    }

    private string _gameDirectory;
    public string GameDir
    {
        get => _gameDirectory;
        set => ChangeProperty(ref _gameDirectory, value);
    }

    private GameConfig _manualGame;
    public GameConfig ManualGame
    {
        get => _manualGame;
        set => ChangeProperty(ref _manualGame, value);
    }

    private bool _useUnpackDir;
    public bool UseUnpackDir
    {
        get => _useUnpackDir;
        set => ChangeProperty(ref _useUnpackDir, value);
    }

    private string _unpackDir;
    public string UnpackDir
    {
        get => _unpackDir;
        set => ChangeProperty(ref _unpackDir, value);
    }

    private bool _useUnpackFilter;
    public bool UseUnpackFilter
    {
        get => _useUnpackFilter;
        set => ChangeProperty(ref _useUnpackFilter, value);
    }

    private string _unpackFilter;
    public string UnpackFilter
    {
        get => _unpackFilter;
        set => ChangeProperty(ref _unpackFilter, value);
    }

    private bool _unpackOverwrite;
    public bool UnpackOverwrite
    {
        get => _unpackOverwrite;
        set => ChangeProperty(ref _unpackOverwrite, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ChangeProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
