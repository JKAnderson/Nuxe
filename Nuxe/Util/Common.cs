using Coremats;
using System.IO;

namespace Nuxe;

internal static class Common
{
    public static void AssertDirExists(string dir, string message)
    {
        if (!Directory.Exists(dir))
            throw new FriendlyException($"{message}\nPath: \"{dir}\"");
    }

    public static void AssertFileExists(string path, string message)
    {
        if (!File.Exists(path))
            throw new FriendlyException($"{message}\nPath: \"{path}\"");
    }

    public static BHD5 ReadBinderHeader(string bhdPath, BHD5.Bhd5Format bhdFormat, string binderKeysDir, bool expectPems)
    {
        byte[] bytes = File.ReadAllBytes(bhdPath);
        if (!BHD5.Is(bytes))
        {
            string key;
            if (expectPems)
            {
                string dir = Path.GetDirectoryName(bhdPath);
                string keyFile = Path.GetFileName(bhdPath).Replace("Ebl.bhd", "KeyCode.pem");
                string keyPath = Path.Combine(dir, keyFile);
                AssertFileExists(keyPath, "Encryption key not found; please verify integrity for Steam games.");
                key = File.ReadAllText(keyPath);
            }
            else
            {
                string binderName = Path.GetFileNameWithoutExtension(bhdPath);
                string keyPath = Path.Combine(binderKeysDir, "Key", binderName + ".pem");
                AssertFileExists(keyPath, "Encryption key not found; please ensure that you've fully extracted the program.");
                key = File.ReadAllText(keyPath);
            }
            bytes = Crypto.DecryptRsa(bytes, key);
        }
        return BHD5.Read(bytes, bhdFormat);
    }
}
