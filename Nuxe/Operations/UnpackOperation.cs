using Coremats;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nuxe;

internal class UnpackOperation : Operation
{
    private const long MB = 1024 * 1024;
    // A completely arbitrary 10MB buffer because it just feels like a good idea somehow
    private const long REQUIRED_SPACE_PADDING = MB * 10;

    private BinderKeysReader BinderKeys { get; }
    private string GameDir { get; }
    private GameConfig GameConfig { get; }
    private string UnpackDir { get; }
    private Regex UnpackFilter { get; }
    private bool UnpackOverwrite { get; }
    private bool AllowMissingBinders { get; }

    public UnpackOperation(string resDir, string gameDir, GameConfig gameConfig, string unpackDir, string unpackFilter, bool unpackOverwrite, bool allowMissingBinders)
    {
        Common.AssertDirExists(gameDir, "Game directory not found; please select a valid directory.");
        GameDir = Path.GetFullPath(gameDir);
        UnpackDir = unpackDir == null ? GameDir : Path.GetFullPath(unpackDir);

        string binderKeysDir = Path.Combine(resDir, "BinderKeys");
        BinderKeys = new(binderKeysDir, gameConfig.BinderKeysName);
        GameConfig = gameConfig;
        UnpackFilter = unpackFilter == null ? null : new Regex(unpackFilter);
        UnpackOverwrite = unpackOverwrite;
        AllowMissingBinders = allowMissingBinders;
    }

    protected override void Run()
    {
        var binders = ReadBinders("(Step 1/4) Loading headers");
        BackupDirs("(Step 2/4) Backing up files");
        var files = AuditFiles("(Step 3/4) Auditing files", binders);
        UnpackFiles("(Step 4/4) Unpacking files", binders, files);
    }

    private List<BinderLight> ReadBinders(string step)
    {
        var binders = new List<BinderLight>();
        for (int i = 0; i < GameConfig.Binders.Count; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            var binderConfig = GameConfig.Binders[i];
            double progress = (double)i / GameConfig.Binders.Count;
            Progress.Report(new(progress, $"{step} - (File {i}/{GameConfig.Binders.Count}) {binderConfig.HeaderPath}"));

            string bhdPath = Path.Combine(GameDir, binderConfig.HeaderPath);
            if (!AllowMissingBinders && !binderConfig.Optional)
                Common.AssertFileExists(bhdPath, "Header file not found; please verify integrity for Steam games.");
            if (File.Exists(bhdPath))
                binders.Add(new(BinderKeys, GameDir, GameConfig, binderConfig));
        }
        return binders;
    }

    private void BackupDirs(string step)
    {
        if (UnpackDir != GameDir)
            return;

        string[] backupDirs = [.. GameConfig.BackupDirs];
        for (int i = 0; i < backupDirs.Length; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            string backupDir = backupDirs[i];
            double progress = (double)i / backupDirs.Length;
            Progress.Report(new(progress, $"{step} - (Directory {i}/{backupDirs.Length}) {backupDir}"));

            string backupSource = Path.Combine(GameDir, backupDir);
            string backupTarget = Path.Combine(GameDir, "_backup", backupDir);
            if (Directory.Exists(backupSource) && !Directory.Exists(backupTarget))
                Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(backupSource, backupTarget);
        }
    }

    private List<BinderFile> AuditFiles(string step, List<BinderLight> binders)
    {
        long requiredSpace = 0;
        var files = new List<BinderFile>();
        for (int i = 0; i < binders.Count; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            BinderLight binder = binders[i];
            double progress = (double)i / binders.Count;
            Progress.Report(new(progress, $"{step} - (File {i}/{binders.Count}) {binder.Config.HeaderPath}"));

            string binderName = Path.GetFileNameWithoutExtension(binder.Config.HeaderPath);
            string unpackDir = binder.Config.UnpackDir ?? Path.GetDirectoryName(binder.Config.HeaderPath);
            foreach (var headerFile in binder.Header.Buckets.SelectMany(b => b))
            {
                string unpackPath;
                if (binder.Dict.TryGetValue(headerFile.PathHash, out string gamePath))
                {
                    unpackPath = Path.Combine(UnpackDir, unpackDir, gamePath.TrimStart('/'));
                }
                else
                {
                    gamePath = Path.Combine("/_unknown", $"{binderName}_{headerFile.PathHash:x16}");
                    unpackPath = Path.Combine(UnpackDir, gamePath.TrimStart('/'));
                }

                bool passedFilter = UnpackFilter == null || UnpackFilter.IsMatch(gamePath);
                bool passedOverwrite = UnpackOverwrite || !File.Exists(unpackPath);
                if (passedFilter && passedOverwrite)
                {
                    files.Add(new(binder.Config, headerFile, gamePath, unpackPath));
                    if (File.Exists(unpackPath))
                        requiredSpace += headerFile.DataLength - new FileInfo(unpackPath).Length;
                    else
                        requiredSpace += headerFile.DataLength;
                }
            }
        }

        CheckFreeSpace(requiredSpace);
        return files;
    }

    private void CheckFreeSpace(long requiredSpace)
    {
        requiredSpace += REQUIRED_SPACE_PADDING;
        long availableSpace = new DriveInfo(Path.GetPathRoot(UnpackDir)).AvailableFreeSpace;
        if (availableSpace < requiredSpace)
        {
            double requiredGb = (double)requiredSpace / (MB * 1024);
            double availableGb = (double)availableSpace / (MB * 1024);
            throw new FriendlyException($"Not enough disk space to unpack files; {requiredGb:F2} GB required, {availableGb:F2} GB available.");
        }
    }

    private void UnpackFiles(string step, List<BinderLight> binders, List<BinderFile> files)
    {
        if (files.Count == 0)
            return;

        var bdtStreams = new Dictionary<string, Stream>();
        try
        {
            foreach (var binder in binders)
            {
                string bdtPath = Path.Combine(GameDir, binder.Config.DataPath);
                Common.AssertFileExists(bdtPath, "Data file not found; please verify integrity for Steam games.");
                bdtStreams[binder.Config.DataPath] = File.OpenRead(bdtPath);
            }

            int maxLength = files.Select(file => file.HeaderFile.DataLength).Max();
            byte[] buffer = new byte[maxLength];
            for (int i = 0; i < files.Count; i++)
            {
                CancellationToken.ThrowIfCancellationRequested();
                BinderFile file = files[i];
                double progress = (double)i / files.Count;
                Progress.Report(new(progress, $"{step} - (File {i}/{files.Count}) {file.GamePath}"));

                var span = ReadFile(file, bdtStreams, buffer);
                string unpackDir = Path.GetDirectoryName(file.UnpackPath);
                Directory.CreateDirectory(unpackDir);
                File.WriteAllBytes(file.UnpackPath, span);
            }
        }
        finally
        {
            foreach (var stream in bdtStreams.Values)
            {
                stream.Dispose();
            }
        }
    }

    private static ReadOnlySpan<byte> ReadFile(BinderFile file, Dictionary<string, Stream> bdtStreams, byte[] buffer)
    {
        var span = buffer.AsSpan(0, file.HeaderFile.DataLength);
        var stream = bdtStreams[file.BinderConfig.DataPath];
        stream.Position = file.HeaderFile.DataOffset;
        stream.ReadExactly(span);
        if (file.HeaderFile.Encryption != null)
            DecryptFile(file.HeaderFile.Encryption, buffer);

        int finalLength = file.HeaderFile.UnpaddedDataLength != 0 ? file.HeaderFile.UnpaddedDataLength : file.HeaderFile.DataLength;
        return buffer.AsSpan(0, finalLength);
    }

    private static void DecryptFile(BHD5.FileEncryption encryption, byte[] buffer)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.KeySize = 128;

        using ICryptoTransform decryptor = aes.CreateDecryptor(encryption.Key, new byte[16]);
        foreach (BHD5.Range range in encryption.Ranges.Where(r => r.Start != -1 && r.End != -1 && r.Start != r.End))
        {
            int start = (int)range.Start;
            int count = (int)(range.End - range.Start);
            decryptor.TransformBlock(buffer, start, count, buffer, start);
        }
    }

    private class BinderLight
    {
        public GameConfig.Binder Config { get; }
        public BHD5 Header { get; }
        public HashDict Dict { get; }

        public BinderLight(BinderKeysReader binderKeys, string gameDir, GameConfig config, GameConfig.Binder binderConfig)
        {
            string bhdPath = Path.Combine(gameDir, binderConfig.HeaderPath);
            Config = binderConfig;
            Header = Common.ReadBinderHeader(bhdPath, config.BinderFormat, binderKeys, config.ExpectPems);
            Dict = binderKeys.ReadDict(bhdPath, config.BinderFormat);
        }
    }

    private class BinderFile
    {
        public GameConfig.Binder BinderConfig { get; }
        public BHD5.File HeaderFile { get; }
        public string GamePath { get; }
        public string UnpackPath { get; }

        public BinderFile(GameConfig.Binder binderConfig, BHD5.File headerFile, string gamePath, string unpackPath)
        {
            BinderConfig = binderConfig;
            HeaderFile = headerFile;
            GamePath = gamePath;
            UnpackPath = unpackPath;
        }
    }
}
