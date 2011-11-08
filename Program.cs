/*
 * Created by SharpDevelop.
 * User: stangecl
 * Date: 11/6/2011
 * Time: 1:59 AM
 *  
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LZ4CSharp
{
	class Program
	{
		public static void Main(string[] args)
		{
			if(args.Length != 1)
			{
				Console.WriteLine("LZ4CSharp performance test");
				Console.WriteLine("usage: LZ4CSharp directory");
				Console.WriteLine("This will time compressing and decompressing all the files in 'directory' ignoring file read time");				
				return;	
			}
			
			var w = new Stopwatch();
			var dw = new Stopwatch();
			long compressedTotal = 0;
			long uncompressedTotal = 0;
			           
			for(int j=0;j<10;j++)
            foreach (var bytes in Read(args[0]))
            {                         	
                uncompressedTotal += bytes.Length;
                byte[] compressed = new byte[(int)(bytes.Length * 1.25 + 255)];
                w.Start();
                int compressedLength = LZ4.Compress(bytes, compressed);
                compressedTotal += compressedLength;
                w.Stop();

                byte[] uncompressed = new byte[bytes.Length];

                dw.Start();
                LZ4.DecompressKnownSize(compressed, uncompressed, uncompressed.Length);
                dw.Stop();

                for (int i = 0; i < uncompressed.Length; i++)
                {
                    if (uncompressed[i] != bytes[i])
                        throw new Exception("Original bytes and decompressed bytes differ starting at byte " + i);
                }
            }
			
			Console.WriteLine("Ratio = " + compressedTotal * 1.0 / uncompressedTotal);
			Console.WriteLine("Compression Time (MB / sec) = " + uncompressedTotal /1024.0/1024.0 / w.Elapsed.TotalSeconds);							
			Console.WriteLine("Uncompression Time (MB / sec) = " + uncompressedTotal /1024.0/1024.0 / dw.Elapsed.TotalSeconds);
			
			Console.WriteLine("Done. Press a key.");
			Console.ReadLine();
		}
		
		static IEnumerable<byte[]> Read(string directory)
		{
			foreach(var file in Directory.GetFiles(directory))
			{				
//				if(!file.EndsWith("webster"))
//					continue;
				using(var reader = new BinaryReader(File.OpenRead(file)))
				{
					while(reader.BaseStream.Length != reader.BaseStream.Position)
						yield return reader.ReadBytes(8 * 1024 * 1024);
				}			
			}
		}
	}
}