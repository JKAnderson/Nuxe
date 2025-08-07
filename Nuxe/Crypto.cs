using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System.IO;

namespace Nuxe;

internal class Crypto
{
    public static byte[] DecryptRsa(string path, string key)
    {
        ICipherParameters parameters = ReadKey(key);
        byte[] input = File.ReadAllBytes(path);
        return DecryptRsa(input, parameters);
    }

    public static byte[] DecryptRsa(byte[] bytes, string key)
    {
        ICipherParameters parameters = ReadKey(key);
        return DecryptRsa(bytes, parameters);
    }

    private static byte[] DecryptRsa(byte[] input, ICipherParameters parameters)
    {
        var engine = new RsaEngine();
        engine.Init(false, parameters);
        int inputBlockSize = engine.GetInputBlockSize();
        int outputBlockSize = engine.GetOutputBlockSize();
        if (input.Length % inputBlockSize != 0)
            throw new ArgumentException($"Input buffer must be a multiple of block size {inputBlockSize}");

        int blocks = input.Length / inputBlockSize;
        byte[] output = new byte[outputBlockSize * blocks];
        Parallel.For(0, blocks, i =>
        {
            byte[] outputBlock = engine.ProcessBlock(input, i * inputBlockSize, inputBlockSize);
            int padding = outputBlockSize - outputBlock.Length;
            Buffer.BlockCopy(outputBlock, 0, output, i * outputBlockSize + padding, outputBlock.Length);
        });
        return output;
    }

    public static AsymmetricKeyParameter ReadKey(string key)
    {
        var pemReader = new PemReader(new StringReader(key));
        return (AsymmetricKeyParameter)pemReader.ReadObject();
    }
}
