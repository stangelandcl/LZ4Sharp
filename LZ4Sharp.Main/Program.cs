using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace LZ4Sharp.Main
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("LZ4Sharp performance test");
                Console.WriteLine("usage: LZ4Sharp directory");
                Console.WriteLine("This will time compressing and decompressing all the files in 'directory' ignoring file read time");
                Console.WriteLine("Done. Press a key.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("This application is running as a " + (IntPtr.Size == 4 ? "32" : "64") + " bit process.");
            Console.WriteLine();

            Console.WriteLine("Test LZ4 32 bit compression");
            TestEmpty(new LZ4Compressor32(), new LZ4Decompressor32());
            Test(args[0], new LZ4Compressor32(), new LZ4Decompressor32());

            Console.WriteLine("Test LZ4 64 bit compression");
            TestEmpty(new LZ4Compressor64(), new LZ4Decompressor64());
            Test(args[0], new LZ4Compressor64(), new LZ4Decompressor64());

            Console.WriteLine("Done. Press a key.");
            Console.ReadLine();
        }       

        private static unsafe void TestEmpty(ILZ4Compressor compressor, ILZ4Decompressor decompressor)
        {
            var bytes = new byte[50];
            byte[] dst = compressor.Compress(bytes);
            var result = decompressor.Decompress(dst);
        }       

        private static void Test(string directory, ILZ4Compressor compressor, ILZ4Decompressor decompressor)
        {
            var w = new Stopwatch();
            var dw = new Stopwatch();
            long compressedTotal = 0;
            long uncompressedTotal = 0;

            for(int j=0;j<10;j++)
            foreach (var bytes in Read(directory))
            {
                uncompressedTotal += bytes.Length;
                byte[] compressed = new byte[compressor.CalculateMaxCompressedLength(bytes.Length)];
                w.Start();
                int compressedLength = compressor.Compress(bytes, compressed);
                compressedTotal += compressedLength;
                w.Stop();

                byte[] uncompressed = new byte[bytes.Length];

                dw.Start();
                decompressor.DecompressKnownSize(compressed, uncompressed, uncompressed.Length);
                dw.Stop();

                for (int i = 0; i < uncompressed.Length; i++)
                {
                    if (uncompressed[i] != bytes[i])
                        throw new Exception("Original bytes and decompressed bytes differ starting at byte " + i);
                }
            }

            Console.WriteLine("Ratio = " + compressedTotal * 1.0 / uncompressedTotal);
            Console.WriteLine("Compression Time (MB / sec) = " + uncompressedTotal / 1024.0 / 1024.0 / w.Elapsed.TotalSeconds);
            Console.WriteLine("Uncompression Time (MB / sec) = " + uncompressedTotal / 1024.0 / 1024.0 / dw.Elapsed.TotalSeconds);
        }

        static IEnumerable<byte[]> Read(string directory)
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                using (var reader = new BinaryReader(File.OpenRead(file)))
                {
                    while (reader.BaseStream.Length != reader.BaseStream.Position)
                        yield return reader.ReadBytes(8 * 1024 * 1024);
                }
            }
        }

   }
}
