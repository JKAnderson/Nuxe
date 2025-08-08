using Coremats;
using System.IO;

namespace Nuxe;

internal class BinderKeysReader
{
    public string BinderKeysDir { get; }
    public string BinderKeysGame { get; }
    public string BinderKeysGameDir { get; }

    public BinderKeysReader(string binderKeysDir, string binderKeysGame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(binderKeysDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(binderKeysGame);

        BinderKeysDir = Path.GetFullPath(binderKeysDir);
        BinderKeysGame = binderKeysGame;
        BinderKeysGameDir = Path.Combine(BinderKeysDir, BinderKeysGame);

        Common.AssertDirExists(BinderKeysGameDir, "BinderKeys game directory not found; please ensure you've extracted the program fully.");
    }

    public string ReadKey(string bhdPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bhdPath);

        string bhdName = Path.GetFileNameWithoutExtension(bhdPath);
        string keyPath = Path.Combine(BinderKeysGameDir, "Key", bhdName + ".pem");
        Common.AssertFileExists(keyPath, "Encryption key not found; please ensure you've extracted the program fully.");
        return File.ReadAllText(keyPath);
    }

    public HashDict ReadDict(string bhdPath, BHD5.Bhd5Format bhdFormat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bhdPath);

        string bhdName = Path.GetFileNameWithoutExtension(bhdPath);
        string dictPath = Path.Combine(BinderKeysGameDir, "Hash", bhdName + ".txt");
        Common.AssertFileExists(dictPath, "Hash dictionary not found; please ensure you've extracted the program fully.");
        return new HashDict(dictPath, bhdFormat);
    }
}
