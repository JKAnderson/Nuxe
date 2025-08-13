using Coremats;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nuxe;

internal class GameConfig
{
    [JsonRequired]
    public string Name { get; set; }

    [JsonRequired]
    public HashSet<string> ExpectedFiles { get; set; }

    [JsonRequired]
    public string BinderKeysName { get; set; }

    [JsonRequired]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BHD5.Bhd5Format BinderFormat { get; set; }

    public bool ExpectPems { get; set; }

    [JsonRequired]
    public IReadOnlyList<Binder> Binders { get; set; }

    [JsonRequired]
    public HashSet<string> BackupDirs { get; set; }

    [JsonRequired]
    public HashSet<string> DeletePaths { get; set; }

    public HashSet<string> PatchAliases { get; set; }

    public override string ToString() => Name;

    public class Binder
    {
        [JsonRequired]
        public string HeaderPath { get; set; }

        [JsonRequired]
        public string DataPath { get; set; }

        public string UnpackDir { get; set; }

        public bool Optional { get; set; }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip };

    public static GameConfig[] LoadGameConfigs(string resDir)
    {
        string configsDir = Path.Combine(resDir, "GameConfigs");
        Common.AssertDirExists(configsDir, "Game configs directory not found; please ensure that you've fully extracted the program files.");
        var configs = Directory.GetFiles(configsDir, "*.jsonc").Select(path =>
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameConfig>(json, SerializerOptions);
        }).OrderBy(config => config.Name).ToArray();
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
