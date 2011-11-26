# Port of Yann Collet's LZ4 algorithm to C# by Clayton Stangeland.

Original LZ4 algorithm can be found at (http://code.google.com/p/lz4) it was created by Yann Collet.
See license.txt for project license.

## Timings
Compression times do not include disk I/O.

P4 = Pentium 4 2GHz 32 bit Windows XP   
i5 = Intel i5 2.67 GHZ 64 bit Windows 7 running LZ4Sharp in 64 bit

LZ4C# = Timings for 'webster' from silesia corpus (http://sun.aei.plsl.pl/~sdeor/index.php?page=silesia)
memcpy and LZ4 = Timing for file 'webster' in silesia corpus in m2mark.exe 
	from http://sd-1.archive-host.com/membres/up/182754578/m2mark.zip 
	(or from link to benchmark program at http://code.google.com/p/lz4)
gzip = GZip from the Intel compiler built as a dll and accessed from C# by PInvoke

Ratio = compressed size / decompressed size (lower is better)


P4 memcpy 217 MB/s
P4 LZ4 Compression 82 MB/s Decompression 304 MB/s
P4 LZ4C# Compression 43 MB/s Decompression 150 MB/s
P4 LZ4C# whole corpus Compression 49 MB/s Decompression 181 MB/s

i5 memcpy 1658 MB/s
i5 Lz4 Compression 270 MB/s Decompression 1184 MB/s  
i5 LZ4C# Compression 207 MB/s Decompression 758 MB/s Ratio 49%
i5 LZ4C# whole corpus Compression 267 MB/s Decompression 838 MB/s Ratio 47%
i5 gzip whole corpus Compression 48 MB/s Decompression 266 MB/s Ratio 33%

### Conclusion:
LZ4 C# is about 2/3 the speed of the c version. (Also, LZ4C# is slightly faster on the whole silesia corpus than on just the 'webster' file)
LZ4 C# Compressed silesia corpus is 47% of uncompressed
LZ4 C# Compressed 'webster' file is 49% of uncompressed

## Steps to recreate.
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