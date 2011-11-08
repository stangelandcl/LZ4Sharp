Port of LZ4 algorithm to C# by Clayton Stangeland.

Original LZ4 algorithm can be found at (http://code.google.com/p/lz4) it was created by Yann Collet.
See license.txt for project license.

Timings
P4 = Pentium 4 2GHz 32 bit Windows XP   
i5 = Intel i5 2.67 GHZ 64 bit Windows 7 running LZ4CSharp in 64 bit

Lz4c# = Timings for 'webster' from silesia corpus (http://sun.aei.polsl.pl/~sdeor/index.php?page=silesia)
memcpy and LZ4 = Timing for file 'webster' in silesia corpus in m2mark.exe 
from http://sd-1.archive-host.com/membres/up/182754578/m2mark.zip 
(or from link to benchmark program at http://code.google.com/p/lz4)

P4 memcpy 217 MB/s
P4 Lz4 Compression 82 MB/s Decompression 304 MB/s
P4 LZ4C# Compression 43 MB/s Decompression 150 MB/s
P4 LZ4C# whole corpus Compression 49 MB/s Decompression 181 MB/s


i5 memcpy 1658 MB/s
i5 Lz4 Compression 270 MB/s Decompression 1184 MB/s
i5 Lz4C# Compression 207 MB/s Decompression 657 MB/s
i5 LZ4C# whole corpus Compression 267 MB/s Decompression 701 MB/s

Conclusion:
LZ4 C# is about twice as slow as the C version. (Also, LZ4C# is slightly faster on the whole silesia corpus than on just the 'webster' file)
LZ4 C# Compressed silesia corpus in 47% of uncompressed
LZ4 C# Compressed 'webster' file in 49% of uncompressed

Steps to recreate.
1) Download lz4.c and lz4.h from http://code.google.com/p/lz4/source/browse/
2) Replace #define with const
3) Expand macros
4) Delete extra lines at top of file
5) Search and replace types with .NET types
6) Create memcpy
7) Rename functions.
9) Remove context (ctx)
9) Remove const on pointers
10) Replace 'ref' (C# keyword with 'r')
11) change array definitions
12) add casts