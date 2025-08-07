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
}
