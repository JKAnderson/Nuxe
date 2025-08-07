using Coremats;
using System.IO;

namespace Nuxe;

public class HashDict : Dictionary<ulong, string>
{
    private const uint PRIME32 = 37;
    private const ulong PRIME64 = 133;

    public HashDict(string dictPath, BHD5.Bhd5Format format)
    {
        foreach (string line in File.ReadAllLines(dictPath))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                string path = Normalize(line);
                ulong hash = FromHash(path, format);
                this[hash] = path;
            }
        }
    }

    public static string Normalize(string path)
    {
        if (path.Contains(':'))
            path = path[(path.IndexOf(':') + 1)..];

        path = path.ToLowerInvariant().Replace('\\', '/').Trim();

        if (!path.StartsWith('/'))
            path = '/' + path;

        return path;
    }

    public static ulong FromHash(string path, BHD5.Bhd5Format format)
    {
        if (format == BHD5.Bhd5Format.EldenRing)
            return path.Aggregate(0ul, (a, c) => a * PRIME64 + c);
        else
            return path.Aggregate(0u, (a, c) => a * PRIME32 + c);
    }
}
