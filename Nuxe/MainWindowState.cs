using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Nuxe;

internal class MainWindowState : INotifyPropertyChanged
{
    private static Properties.Settings Settings => Properties.Settings.Default;

    public string TitleText { get; }
    public string ResDir { get; }
    public GameConfig[] GameConfigs { get; }

    public string GameDir { get; set => ChangeProperty(ref field, value); }
    public string GameExe { get; set => ChangeProperty(ref field, value); }
    public GameConfig ManualGame { get; set => ChangeProperty(ref field, value); }
    public bool UseUnpackDir { get; set => ChangeProperty(ref field, value); }
    public string UnpackDir { get; set => ChangeProperty(ref field, value); }
    public bool UseUnpackFilter { get; set => ChangeProperty(ref field, value); }
    public string UnpackFilter { get; set => ChangeProperty(ref field, value); }
    public bool UnpackOverwrite { get; set => ChangeProperty(ref field, value); }
    public bool UsePatchOutputPath { get; set => ChangeProperty(ref field, value); }
    public string PatchOutputPath { get; set => ChangeProperty(ref field, value); }

    public MainWindowState()
    {
        TitleText = $"Nuxe {typeof(App).Assembly.GetName().Version.ToString(3)}";

        string resDir = Path.Combine(AppContext.BaseDirectory, "res");
        string resDirOverride = Environment.GetEnvironmentVariable("NUXE_RES_DIR");
        ResDir = Path.GetFullPath(resDirOverride ?? resDir);
        GameConfigs = GameConfig.LoadGameConfigs(ResDir);

        if (!Settings.Upgraded)
        {
            Settings.Upgrade();
            Settings.Upgraded = true;
        }

        GameDir = Settings.GameDir;
        GameExe = Settings.GameExe;
        ManualGame = Array.Find(GameConfigs, config => config.BinderKeysName == Settings.ManualGame);
        UseUnpackDir = Settings.UseUnpackDir;
        UnpackDir = Settings.UnpackDir;
        UseUnpackFilter = Settings.UseUnpackFilter;
        UnpackFilter = Settings.UnpackFilter;
        UnpackOverwrite = Settings.UnpackOverwrite;
        UsePatchOutputPath = Settings.UsePatchOutputPath;
        PatchOutputPath = Settings.PatchOutputPath;
    }

    public void Save()
    {
        Settings.GameDir = GameDir;
        Settings.GameExe = GameExe;
        Settings.ManualGame = ManualGame?.BinderKeysName;
        Settings.UseUnpackDir = UseUnpackDir;
        Settings.UnpackDir = UnpackDir;
        Settings.UseUnpackFilter = UseUnpackFilter;
        Settings.UnpackFilter = UnpackFilter;
        Settings.UnpackOverwrite = UnpackOverwrite;
        Settings.UsePatchOutputPath = UsePatchOutputPath;
        Settings.PatchOutputPath = PatchOutputPath;
        Settings.Save();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ChangeProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
