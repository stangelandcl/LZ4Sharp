using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LZ4Sharp
{
	#if LZ4_ARCH64
		#define CLASSNAME LZ4Decompressor64
	#else
		#define CLASSNAME LZ4Decompressor32
	#endif
    /// <summary>
    /// Class for decompressing an LZ4 compressed byte array.
    /// </summary>
     public unsafe class CLASSNAME : ILZ4Decompressor
    {
        #include "LZ4.h"

        readonly sbyte[] m_DecArray = new sbyte[8] { 0, 3, 2, 3,0,0,0,0 };
        #if LZ4_ARCH64
		readonly sbyte[] m_Dec2table= new sbyte[8]{0, 0, 0, -1, 0, 1, 2, 3};
        #endif

        // Note : The decoding functions LZ4_uncompress() and LZ4_uncompress_unknownOutputSize()
        //              are safe against "buffer overflow" attack type
        //              since they will *never* write outside of the provided output buffer :
        //              they both check this condition *before* writing anything.
        //              A corrupted packet however can make them *read* within the first 64K before the output buffer.

        /// <summary>
        /// Decompress.
        /// </summary>
        /// <param name="source">compressed array</param>
        /// <param name="dest">This must be the exact length of the decompressed item</param>
        public void DecompressKnownSize(byte[] compressed, byte[] decompressed)
        {
            int len = DecompressKnownSize(compressed, decompressed, decompressed.Length);
            Debug.Assert(len == decompressed.Length);
        }

        public int DecompressKnownSize(byte[] compressed, byte[] decompressedBuffer, int decompressedSize)
        {
            fixed (byte* src = compressed)
            fixed (byte* dst = decompressedBuffer)
                return DecompressKnownSize(src, dst, decompressedSize);
        }

        public int DecompressKnownSize(byte* compressed, byte* decompressedBuffer, int decompressedSize)
        {
            fixed (sbyte* dec = m_DecArray)
#if LZ4_ARCH64
            fixed(sbyte* dec2Ptr = m_Dec2table)
#endif
            {
                // Local Variables
                byte* ip = (byte*)compressed;
                byte* r;

                byte* op = (byte*)decompressedBuffer;
                byte* oend = op + decompressedSize;
                byte* cpy;

                byte token;
                int len, length;


                // Main Loop
                while (true)
                {
                    // get runLength
                    token = *ip++;
                    if ((length = (token >> LZ4Util.ML_BITS)) == LZ4Util.RUN_MASK) { for (; (len = *ip++) == 255; length += 255) { } length += len; }


                    cpy = op + length;
                    if (cpy > oend - LZ4Util.COPYLENGTH)
                    {
                        if (cpy > oend) goto _output_error;
                        LZ4Util.CopyMemory(op, ip, length);
                        ip += length;
                        break;
                    }

                    LZ4_WILDCOPY(ip, op, cpy); ip -= (op - cpy); op = cpy;


                    // get offset
                    LZ4_READ_LITTLEENDIAN_16(r,cpy,ip); ip+=2;
					if(r < decompressedBuffer) goto _output_error;

                    // get matchLength
                    if ((length = (int)(token & LZ4Util.ML_MASK)) == LZ4Util.ML_MASK) { for (; *ip == 255; length += 255) { ip++; } length += *ip++; }

                    // copy repeated sequence
                    if (op - r < STEPSIZE)
                    {
#if LZ4_ARCH64
                        var dec2 = dec2Ptr[(int)(op-r)];
#else
                        const int dec2 = 0;
#endif


                        *op++ = *r++;
                        *op++ = *r++;
                        *op++ = *r++;
                        *op++ = *r++;
                        r -= dec[op - r];
                        A32(op) = A32(r); op += STEPSIZE - 4;
                        r -= dec2;
                    }
                    else { LZ4_COPYSTEP(r, op);  }
                    cpy = op + length - (STEPSIZE - 4);
                    if (cpy > oend - LZ4Util.COPYLENGTH)
                    {
                        if (cpy > oend) goto _output_error;

                        LZ4_SECURECOPY(r, op, (oend - LZ4Util.COPYLENGTH));
                        while (op < cpy) *op++ = *r++;
                        op = cpy;
                        if (op == oend) break;
                        continue;
                    }
                    
                    LZ4_SECURECOPY(r, op, cpy);
                    op = cpy; // correction
                }

                // end of decoding
                return (int)(((byte*)ip) - compressed);

                // write overflow error detected
            _output_error:
                return (int)(-(((byte*)ip) - compressed));
            }
        }

        public byte[] Decompress(byte[] compressed)
        {
			int length = compressed.Length;
            int len;
            byte[] dest;
			const int Multiplier = 4; // Just a number. Determines how fast length should increase.
            do
            {
                length *= Multiplier;
                dest = new byte[length];
                len = Decompress(compressed, dest, compressed.Length);
            }
            while (len < 0 || dest.Length < len);

            byte[] d = new byte[len];
            Buffer.BlockCopy(dest, 0, d, 0, d.Length);
            return d;
        }

        public int Decompress(byte[] compressed, byte[] decompressedBuffer)
        {
            return Decompress(compressed, decompressedBuffer, compressed.Length);
        }

        public int Decompress(byte[] compressedBuffer, byte[] decompressedBuffer, int compressedSize)
        {
            fixed (byte* src = compressedBuffer)
            fixed (byte* dst = decompressedBuffer)
                return Decompress(src, dst, compressedSize, decompressedBuffer.Length);
        }

        public int Decompress(byte[] compressedBuffer, int compressedPosition, byte[] decompressedBuffer, int decompressedPosition, int compressedSize)
        {
            fixed (byte* src = &compressedBuffer[compressedPosition])
            fixed (byte* dst = &decompressedBuffer[decompressedPosition])
                return Decompress(src, dst, compressedSize, decompressedBuffer.Length);
        }

        public int Decompress(
            byte* compressedBuffer,
            byte* decompressedBuffer,
            int compressedSize,
            int maxDecompressedSize)
        {
            fixed (sbyte* dec = m_DecArray)
#if LZ4_ARCH64
            fixed(sbyte* dec2Ptr = m_Dec2table)
#endif
            {
                // Local Variables
                byte* ip = (byte*)compressedBuffer;
                byte* iend = ip + compressedSize;
                byte* r;

                byte* op = (byte*)decompressedBuffer;
                byte* oend = op + maxDecompressedSize;
                byte* cpy;

                byte token;
                int len, length;


                // Main Loop
                while (ip < iend)
                {
                    // get runLength
                    token = *ip++;
                    if ((length = (token >> LZ4Util.ML_BITS)) == LZ4Util.RUN_MASK) { for (; (len = *ip++) == 255; length += 255) { } length += len; }

                    // copy literals
                    cpy = op + length;
                    if (cpy > oend - LZ4Util.COPYLENGTH)
                    {
                        if (cpy > oend) goto _output_error;
                        LZ4Util.CopyMemory(op, ip, length);
                        op += length;
                        break; //Necessarily EOF
                    }

                    LZ4_WILDCOPY(ip, op, cpy); ip -= (op - cpy); op = cpy;
                    if (ip >= iend) break; // check EOF

                    // get offset
                    LZ4_READ_LITTLEENDIAN_16(r,cpy,ip); ip+=2;
                    if (r < decompressedBuffer) goto _output_error;

                    // get matchlength
                    if ((length = (int)(token & LZ4Util.ML_MASK)) == LZ4Util.ML_MASK) { for (; (len = *ip++) == 255; length += 255) { } length += len; }

                    // copy repeated sequence
                    if (op - r < STEPSIZE)
                    {
#if LZ4_ARCH64
                        var dec2 = dec2Ptr[op-ref];
#else
			            const int dec2 = 0;
#endif

                        *op++ = *r++;
                        *op++ = *r++;
                        *op++ = *r++;
                        *op++ = *r++;
                        r -= dec[op - r];
                        A32(op) = A32(r); op += STEPSIZE - 4;
                        r -= dec2;
                    }
                    else { LZ4_COPYSTEP(r, op); }
                    cpy = op + length - (STEPSIZE - 4);
                    if (cpy > oend - LZ4Util.COPYLENGTH)
                    {
                        if (cpy > oend) goto _output_error;

                        LZ4_SECURECOPY(r, op, (oend - LZ4Util.COPYLENGTH));
                        while (op < cpy) *op++ = *r++;
                        op = cpy;
                        if (op == oend) break; // Check EOF (should never happen, since last 5 bytes are supposed to be literals)
                        continue;
                    }
                    LZ4_SECURECOPY(r, op, cpy);
                    op = cpy; // correction
                }


                return (int)(((byte*)op) - decompressedBuffer);


            _output_error:
                return (int)(-(((byte*)ip) - compressedBuffer));
            }
        }
    }
}
