
		#define BYTE byte
		#define U64 ulong

#if LZ4_ARCH64
        const int STEPSIZE = 8;
		#define size_t long
        #define UARCH ulong
        #define AARCH *(long*)
        #define LZ4_COPYSTEP(s,d)		A64(d) = A64(s); d+=8; s+=8;
        #define LZ4_COPYPACKET(s,d)		LZ4_COPYSTEP(s,d)
        #define LZ4_SECURECOPY(s,d,e)	if (d<e) LZ4_WILDCOPY(s,d,e)
        #define LZ4_NbCommonBytes(val) DeBruijnBytePos[((ulong)((val & -val) * 0x0218A392CDABBD3F)) >> 58];
        static byte[] DeBruijnBytePos = new byte[64]{ 0, 0, 0, 0, 0, 1, 1, 2, 0, 3, 1, 3, 1, 4, 2, 7, 0, 2, 3, 6, 1, 5, 3, 5, 1, 3, 4, 4, 2, 5, 6, 7, 7, 0, 1, 2, 3, 3, 4, 6, 2, 6, 5, 5, 3, 4, 5, 6, 7, 1, 2, 4, 6, 4, 4, 5, 7, 2, 6, 5, 7, 6, 7, 7 };
        #define INITBASE(basePtr) long basePtr = (long)ip
#else
        const int STEPSIZE = 4;
		#define size_t int
        #define UARCH uint
        #define AARCH *(int*)
        #define LZ4_COPYSTEP(s,d)		A32(d) = A32(s); d+=4; s+=4;
        #define LZ4_COPYPACKET(s,d)		LZ4_COPYSTEP(s,d); LZ4_COPYSTEP(s,d);
        #define LZ4_SECURECOPY			LZ4_WILDCOPY
        #define LZ4_NbCommonBytes(val) DeBruijnBytePos[((uint)((val & -val) * 0x077CB531U)) >> 27];
        static byte[] DeBruijnBytePos = new byte[32] { 0, 0, 3, 0, 3, 1, 3, 0, 3, 2, 2, 1, 3, 2, 0, 1, 3, 3, 1, 2, 2, 2, 2, 0, 3, 1, 2, 0, 1, 0, 1, 1 };
        #define INITBASE(basePtr) int basePtr = 0;
#endif

		#define LZ4_READ_LITTLEENDIAN_16(d,s,p) { d = (s) - A16(p); }

		#define ref r
		#define A64(x) *(ulong*)x
		#define A16(x) *(ushort*)x
        #define A32(x) *(uint*)x
        #define U32(x) *(uint*)x


        //**************************************
        // Macros
        //**************************************
        #define LZ4_HASH_FUNCTION(i)	(((i) * 2654435761U) >> ((MINMATCH*8)-HASH_LOG))
        #define LZ4_HASH_VALUE(p)		LZ4_HASH_FUNCTION(A32(p))
        #define LZ4_WILDCOPY(s,d,e)		do { LZ4_COPYPACKET(s,d) } while (d<e);
        #define LZ4_BLINDCOPY(s,d,l)	{ BYTE* e=(d)+l; LZ4_WILDCOPY(s,d,e); d=e; }
