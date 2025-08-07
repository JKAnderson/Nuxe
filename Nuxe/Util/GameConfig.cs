using Coremats;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nuxe;

internal class GameConfig
{
    public string Name { get; set; }
    public HashSet<string> ExpectedFiles { get; set; }
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

    public static GameConfig[] LoadGameConfigs(string resDir)
    {
        string configsDir = Path.Combine(resDir, "GameConfigs");
        Common.AssertDirExists(configsDir, "Game configs directory not found; please ensure that you've fully extracted the program files.");
        var configs = Directory.GetFiles(configsDir, "*.json").Select(path =>
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameConfig>(json);
        }).ToArray();
        return configs;
    }

    public static GameConfig DetectGameConfig(GameConfig[] gameConfigs, string gameDir)
    {
        foreach (var config in gameConfigs)
            if (config.ExpectedFiles.All(file => File.Exists(Path.Combine(gameDir, file))))
                return config;

        throw new FriendlyException("Failed to detect game type; please ensure that you've selected a valid game directory.");
    }
}
