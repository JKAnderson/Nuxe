using Coremats;
using System.IO;
using System.Security.Cryptography;

namespace Nuxe;

internal class UnpackOperation : Operation
{
    private string GameDir { get; set; }
    private GameConfig GameConfig { get; set; }

    public UnpackOperation(string gameDir)
    {
        GameDir = gameDir;
    }

    protected override void Run()
    {
        Common.AssertDirExists(GameDir, "Game directory not found; please select a valid directory.");
        GameDir = Path.GetFullPath(GameDir);
        GameConfig = GameConfig.DetectGameConfig(GameDir);

        var binders = ReadBinders("(Step 1/3) Loading headers");

        Progress.Report(new(0, "(Step 2/3) Auditing files"));
        var files = AuditFiles(binders);

        UnpackFiles("(Step 3/3) Unpacking files", binders, files);
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
            if (!binderConfig.Optional)
                Common.AssertFileExists(bhdPath, "Header file not found; please verify integrity for Steam games.");
            if (File.Exists(bhdPath))
                binders.Add(new(GameDir, GameConfig, binderConfig));
        }
        return binders;
    }

    private List<BinderFile> AuditFiles(List<BinderLight> binders)
    {
        var files = new List<BinderFile>();
        foreach (var binder in binders)
        {
            foreach (var headerFile in binder.Header.Buckets.SelectMany(b => b))
            {
                string binderDir = Path.GetDirectoryName(binder.Config.HeaderPath);
                string gamePath = binder.Dict.GetValueOrDefault(headerFile.PathHash, null);
                string unpackPath;
                if (gamePath == null)
                    unpackPath = Path.Combine(GameDir, "_unknown", $"{headerFile.PathHash:x16}");
                else
                    unpackPath = Path.Combine(GameDir, binderDir, gamePath.TrimStart('/'));

                if (!File.Exists(unpackPath))
                    files.Add(new(binder.Config, headerFile, gamePath, unpackPath));
            }
        }
        return files;
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
                Progress.Report(new(progress, $"{step} - (File {i}/{files.Count}) {file.GamePath ?? $"{file.HeaderFile.PathHash:x16}"}"));

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

        public BinderLight(string gameDir, GameConfig config, GameConfig.Binder binderConfig)
        {
            Config = binderConfig;

            string binderKeysDir = Path.GetFullPath(Path.Combine("res", "BinderKeys", config.BinderKeysName));
            string binderName = Path.GetFileNameWithoutExtension(binderConfig.HeaderPath);

            string bhdPath = Path.Combine(gameDir, binderConfig.HeaderPath);
            byte[] bytes = File.ReadAllBytes(bhdPath);
            if (!BHD5.Is(bytes))
            {
                string keyPath = Path.Combine(binderKeysDir, "Key", binderName + ".pem");
                Common.AssertFileExists(keyPath, "Encryption key not found; please ensure that you've fully extracted the program.");
                string key = File.ReadAllText(keyPath);
                bytes = Crypto.DecryptRsa(bytes, key);
            }
            Header = BHD5.Read(bytes, config.BinderFormat);

            string dictPath = Path.Combine(binderKeysDir, "Hash", binderName + ".txt");
            Common.AssertFileExists(dictPath, "Hash dictionary not found; please ensure that you've fully extracted the program.");
            Dict = new HashDict(dictPath, config.BinderFormat);
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
