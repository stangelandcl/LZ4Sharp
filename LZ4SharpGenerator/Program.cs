using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LZ4SharpGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            //foreach (var strargs in new[] {
            // " -P -CC -x c -E \"$(ProjectDir)LZ4Compressor.c\" >> \"$(ProjectDir)LZ4Compressor32.cs\"",
            // " -P -CC -x c -E \"$(ProjectDir)LZ4Decompressor.c\" >> \"$(ProjectDir)LZ4Decompressor32.cs\"",
            // " -DLZ4_ARCH64=1 -P -CC -x c -E \"$(ProjectDir)LZ4Compressor.c\" >> \"$(ProjectDir)LZ4Compressor64.cs\"",
            // " -DLZ4_ARCH64=1 -P -CC -x c -E \"$(ProjectDir)LZ4Decompressor.c\" >> \"$(ProjectDir)LZ4Decompressor64.cs\""})
            //{
            //    const string gcc = @"C:\Portable\Programs\Programming\CodeBlocks\MinGW\bin\gcc.exe";
            //    // @"C:\Program Files (x86)\CodeBlocks\MinGW\bin\gcc.exe";


            //    var gccArgs = strargs.Replace("$(ProjectDir)", Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), @"..\..\..\LZ4Sharp\"));
            //    var s = new ProcessStartInfo(gcc + gccArgs);
            //    s.RedirectStandardOutput = true;
            //    s.UseShellExecute = false;                
            //    var p = Process.Start(s);
            //    p.WaitForExit();
            //}

            //Console.ReadLine();
        }
    }
}
