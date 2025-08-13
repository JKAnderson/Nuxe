using System.IO;
using System.Text;

namespace Nuxe;

internal class PatchOperation : Operation
{
    private string ExePath { get; }
    private GameConfig GameConfig { get; }
    private string OutputPath { get; }

    public PatchOperation(string exePath, GameConfig gameConfig, string outputPath)
    {
        if (gameConfig.PatchAliases == null)
            throw new FriendlyException("Executable patching is not supported for this game.");

        Common.AssertFileExists(exePath, "Executable file not found; please select a valid file.");
        ExePath = Path.GetFullPath(exePath);
        GameConfig = gameConfig;
        OutputPath = outputPath == null ? ExePath : Path.GetFullPath(outputPath);
    }

    protected override void Run()
    {
        byte[] bytes = File.ReadAllBytes(ExePath);
        int aliasesPatched = Patch("(Step 1/1) Patching aliases", bytes);
        if (aliasesPatched == 0)
            throw new FriendlyException("No aliases found to patch; the file may already be patched, or you may have selected the wrong file.");

        if (OutputPath == ExePath)
        {
            string backupDir = Path.Combine(Path.GetDirectoryName(ExePath), "_backup");
            string backupPath = Path.Combine(backupDir, Path.GetFileName(ExePath));
            Directory.CreateDirectory(backupDir);
            File.Copy(ExePath, backupPath);
        }
        File.WriteAllBytes(OutputPath, bytes);
    }

    private int Patch(string step, byte[] bytes)
    {
        int aliasesPatched = 0;
        string[] aliases = [.. GameConfig.PatchAliases];
        for (int i = 0; i < aliases.Length; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            string alias = aliases[i];
            double progress = (double)i / aliases.Length;
            Progress.Report(new(progress, $"{step} - (Alias {i}/{aliases.Length}) {alias}"));

            string replace = $"{alias}:/";
            string with = $".{new('/', alias.Length + 1)}";
            aliasesPatched += PatchAlias(bytes, replace, with);
        }
        return aliasesPatched;
    }

    private static int PatchAlias(byte[] bytes, string replace, string with)
    {
        byte[] replaceBytes = Encoding.Unicode.GetBytes(replace);
        byte[] withBytes = Encoding.Unicode.GetBytes(with);
        ArgumentOutOfRangeException.ThrowIfNotEqual(replaceBytes.Length, withBytes.Length);

        // This is hecka slow, but that's fine
        int aliasesPatched = 0;
        for (int i = 0; i < bytes.Length - replaceBytes.Length; i++)
        {
            if (IsMatch(bytes, i, replaceBytes))
            {
                withBytes.CopyTo(bytes, i);
                aliasesPatched++;
            }
        }
        return aliasesPatched;
    }

    private static bool IsMatch(byte[] text, int offset, byte[] pattern)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            if (text[offset + i] != pattern[i])
                return false;
        }
        return true;
    }
}
