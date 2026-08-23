using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace Nuxe.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    private static Properties.Settings Settings => Properties.Settings.Default;

    public string TitleText { get; }
    public string ResDir { get; }
    public GameConfig[] GameConfigs { get; }

    [ObservableProperty] public partial string GameDir { get; set; }
    [ObservableProperty] public partial string GameExe { get; set; }
    [ObservableProperty] public partial GameConfig ManualGame { get; set; }
    [ObservableProperty] public partial bool UseUnpackDir { get; set; }
    [ObservableProperty] public partial string UnpackDir { get; set; }
    [ObservableProperty] public partial bool UseUnpackFilter { get; set; }
    [ObservableProperty] public partial string UnpackFilter { get; set; }
    [ObservableProperty] public partial bool UnpackOverwrite { get; set; }
    [ObservableProperty] public partial bool UsePatchOutputPath { get; set; }
    [ObservableProperty] public partial string PatchOutputPath { get; set; }

    public MainViewModel()
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
}
