using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace LZ4Sharp
{
    /// <summary>
    /// Static LZ4 Compression and Decompression. This is threadsafe because it creates new instances of the
    /// compression/decompression classes for each method call.
    /// It is recommended to use LZ4Compressor and LZ4Decompressor for repeated compressing/decompressing for speed and less memory allocations.
    /// </summary>
    public static unsafe class LZ4
    {
        public static byte[] Compress(byte[] source)
        {
            return LZ4CompressorFactory.CreateNew().Compress(source);
        }

        /// <summary>
        /// Calculate the max compressed byte[] size given the size of the uncompressed byte[]
        /// </summary>
        /// <param name="uncompressedLength">Length of the uncompressed data</param>
        /// <returns>The maximum required size in bytes of the compressed data</returns>
        public static int CalculateMaxCompressedLength(int uncompressedLength)
        {
            return LZ4CompressorFactory.CreateNew().CalculateMaxCompressedLength(uncompressedLength);
        }

        /// <summary>
        /// Compress source into dest returning compressed length
        /// </summary>
        /// <param name="source">uncompressed data</param>
        /// <param name="dest">array into which source will be compressed</param>
        /// <returns>compressed length</returns>
        public static int Compress(byte[] source, byte[] dest)
        {
            return LZ4CompressorFactory.CreateNew().Compress(source, dest);
        }

        /// <summary>
        /// Compress source into dest returning compressed length
        /// </summary>
        /// <param name="source">uncompressed data</param>
        /// <param name="srcOffset">offset in source array where reading will start</param>
        /// <param name="count">count of bytes in source array to compress</param>
        /// <param name="dest">array into which source will be compressed</param>
        /// <param name="dstOffset">start index in dest array where writing will start</param>
        /// <returns>compressed length</returns>
        public static int Compress(byte[] source, int srcOffset, int count, byte[] dest, int dstOffset)
        {
            return LZ4CompressorFactory.CreateNew().Compress(source, srcOffset, count, dest, dstOffset);
        }


        public static void DecompressKnownSize(byte[] compressed, byte[] decompressed)
        {
            LZ4DecompressorFactory.CreateNew().DecompressKnownSize(compressed, decompressed);
        }

        public static int DecompressKnownSize(byte[] compressed, byte[] decompressedBuffer, int decompressedSize)
        {
            return LZ4DecompressorFactory.CreateNew().DecompressKnownSize(compressed, decompressedBuffer, decompressedSize);
        }

        public static int DecompressKnownSize(byte* compressed, byte* decompressedBuffer, int decompressedSize)
        {
            return LZ4DecompressorFactory.CreateNew().DecompressKnownSize(compressed, decompressedBuffer, decompressedSize);
        }

        public static byte[] Decompress(byte[] compressed)
        {
            return LZ4DecompressorFactory.CreateNew().Decompress(compressed);
        }

        public static int Decompress(byte[] compressed, byte[] decompressedBuffer)
        {
            return LZ4DecompressorFactory.CreateNew().Decompress(compressed, decompressedBuffer);
        }

        public static int Decompress(byte[] compressedBuffer, byte[] decompressedBuffer, int compressedSize)
        {
            return LZ4DecompressorFactory.CreateNew().Decompress(compressedBuffer, decompressedBuffer, compressedSize);
        }

        public static int Decompress(
            byte* compressedBuffer,
            byte* decompressedBuffer,
            int compressedSize,
            int maxDecompressedSize)
        {
            return LZ4DecompressorFactory.CreateNew().Decompress(compressedBuffer, decompressedBuffer, compressedSize, maxDecompressedSize);
        }
    }

}