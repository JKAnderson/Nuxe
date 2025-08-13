using Coremats;
using System.IO;

namespace Nuxe;

internal class DecryptOperation : Operation
{
    private BinderKeysReader BinderKeys { get; }
    private string GameDir { get; }
    private GameConfig GameConfig { get; }

    public DecryptOperation(string resDir, string gameDir, GameConfig gameConfig)
    {
        Common.AssertDirExists(gameDir, "Game directory not found; please select a valid directory.");
        GameDir = Path.GetFullPath(gameDir);

        string binderKeysDir = Path.Combine(resDir, "BinderKeys");
        BinderKeys = new(binderKeysDir, gameConfig.BinderKeysName);
        GameConfig = gameConfig;
    }

    protected override void Run()
    {
        DecryptHeaders("(Step 1/1) Decrypting headers");
    }

    private void DecryptHeaders(string step)
    {
        for (int i = 0; i < GameConfig.Binders.Count; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            var binderConfig = GameConfig.Binders[i];
            double progress = (double)i / GameConfig.Binders.Count;
            Progress.Report(new(progress, $"{step} - (Header {i}/{GameConfig.Binders.Count}) {binderConfig.HeaderPath}"));

            string bhdPath = Path.Combine(GameDir, binderConfig.HeaderPath);
            if (File.Exists(bhdPath))
            {
                byte[] bytes = File.ReadAllBytes(bhdPath);
                if (!BHD5.Is(bytes))
                {
                    bytes = Common.DecryptBinderHeader(bhdPath, BinderKeys, GameConfig.ExpectPems, bytes);

                    string decDir = Path.GetDirectoryName(bhdPath);
                    string decName = Path.GetFileNameWithoutExtension(bhdPath);
                    string decExt = Path.GetExtension(bhdPath);
                    string decPath = Path.Combine(decDir, $"{decName}-dec{decExt}");
                    File.WriteAllBytes(decPath, bytes);
                }
            }
        }
    }
}
