using Coremats;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nuxe;

internal class GameConfig
{
    public string BinderKeysName { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BHD5.Bhd5Format BinderFormat { get; set; }
    public IReadOnlyList<Binder> Binders { get; set; }

    public class Binder
    {
        public string HeaderPath { get; set; }
        public string DataPath { get; set; }
        public bool Optional { get; set; }
    }

    private record GameDetection(string ConfigName, string[] ExpectedFiles);

    private static readonly GameDetection[] Detections = [
        new("ArmoredCore5_PS3", ["EBOOT.BIN", "bind/dvdbnd5.bhd"]),
        new("ArmoredCore5_X360", ["default.xex", "bind/dvdbnd5.bhd"]),
        new("ArmoredCore6_PC", ["armoredcore6.exe"]),
        new("ArmoredCoreVerdictDay_PS3", ["EBOOT.BIN", "bind/dvdbnd5_layer0.bhd"]),
        new("ArmoredCoreVerdictDay_X360", ["default.xex", "bind/dvdbnd5_layer0.bhd"]),
        new("DarkSouls_PC", ["DARKSOULS.exe"]),
        new("DarkSouls_PS3", ["EBOOT.BIN", "dvdbnd.bhd5"]),
        new("DarkSouls_X360", ["default.xex", "dvdbnd0.bhd5"]),
        new("DarkSouls2_PC", ["DarkSoulsII.exe", "HqChrEbl.bhd"]),
        new("DarkSouls2Scholar_PC", ["DarkSoulsII.exe", "LqChrEbl.bhd"]),
        new("DarkSouls3_PC", ["DarkSoulsIII.exe"]),
        //new("DarkSoulsRemastered_NS", []),
        new("EldenRing_PC", ["eldenring.exe"]),
        new("EldenRingNightreign_PC", ["nightreign.exe"]),
        new("Sekiro_PC", ["sekiro.exe"]),
        new("SekiroSoundtrack_PC", ["DigitalArtwork_MiniSoundtrack.exe"]),
        new("SteelBattalionHeavyArmor_X360", ["default.xex", "mdl_chr.bhd5"]),
        ];

    public static GameConfig DetectGameConfig(string gameDir)
    {
        string configName = DetectGame(gameDir);
        string configPath = Path.Combine("res", configName + ".json");
        Common.AssertFileExists(configPath, "Game config not found; please ensure that you've fully extracted the program files.");
        string config = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<GameConfig>(config);
    }

    private static string DetectGame(string gameDir)
    {
        foreach (var detection in Detections)
            if (detection.ExpectedFiles.All(file => File.Exists(Path.Combine(gameDir, file))))
                return detection.ConfigName;

        throw new FriendlyException("Failed to detect game type; please ensure that you've selected a valid game directory.");
    }
}
