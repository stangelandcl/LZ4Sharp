using System;
namespace LZ4Sharp
{
    public interface ILZ4Compressor
    {
        int CalculateMaxCompressedLength(int uncompressedLength);
        byte[] Compress(byte[] source);
        int Compress(byte[] source, byte[] dest);
        int Compress(byte[] source, int srcOffset, int count, byte[] dest, int dstOffset);
    }
}
