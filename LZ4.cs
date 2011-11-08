using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LZ4CSharp
{
	public static unsafe class LZ4
	{		
		//**************************************
		// Performance parameter
		//**************************************
		// Increasing this value improves compression ratio
		// Lowering this value reduces memory usage
		// Lowering may also improve speed, typically on reaching cache size limits (L1 32KB for Intel, 64KB for AMD)
		// Memory usage formula for 32 bits systems : N->2^(N+2) Bytes (examples : 17 -> 512KB ; 12 -> 16KB)
		const int HASH_LOG = 12;
		//**************************************
		// Constants
		//**************************************
		const int MINMATCH = 4;
		const int SKIPSTRENGTH = 6;
		const int STACKLIMIT = 13;
		const bool HEAPMODE = (HASH_LOG>STACKLIMIT);  // Defines if memory is allocated into the stack (local variable), or into the heap (malloc()).
		const int COPYTOKEN = 4;
		const int COPYLENGTH = 8;
		const int LASTLITERALS = 5;
		const int MFLIMIT = (COPYLENGTH+MINMATCH);
		const int MINLENGTH = (MFLIMIT+1);

		const int MAXD_LOG = 16;
		const int MAX_DISTANCE = ((1 << MAXD_LOG) - 1);

		const int HASHTABLESIZE = (1 << HASH_LOG);
		const int HASH_MASK = (HASHTABLESIZE - 1);

		const int ML_BITS = 4;
		const uint ML_MASK = ((1U<<ML_BITS)-1);
		const int RUN_BITS = (8-ML_BITS);
		const uint RUN_MASK = ((1U<<RUN_BITS)-1);
		
		const uint LZ4_64KLIMIT =((1U<<16) + (MFLIMIT-1));
		const int HASHLOG64K = (HASH_LOG+1);

		public static byte[] Compress(byte[] source)
		{
			byte[] dst = new byte[(int)(source.Length * 1.25 + 255)]; // guess max length.
			int length = Compress(source, dst);
			byte[] dest = new byte[length];
			Buffer.BlockCopy(dst, 0, dest, 0, length);
			return dest;
		}
		
		public static int Compress(byte[] source, byte[] dest)
		{
			fixed(byte* s = source)
			fixed(byte* d = dest)
			{
        		if (source.Length < (int)LZ4_64KLIMIT) 
        			return Compress64K(s, d, source.Length);
        		return Compress(s, d, source.Length);
			}
		}

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
		public static void DecompressKnownSize(byte[] compressed, byte[] decompressed)
		{
			int len = DecompressKnownSize(compressed, decompressed, decompressed.Length);
			Debug.Assert(len == decompressed.Length);
		}

		public static int DecompressKnownSize(byte[] compressed, byte[] decompressedBuffer, int decompressedSize)
		{
			fixed(byte* src = compressed)
				fixed(byte* dst = decompressedBuffer)
				return DecompressKnownSize(src, dst, decompressedSize);
		}

		public static int DecompressKnownSize(byte* compressed, byte* decompressedBuffer, int decompressedSize)
		{
			// Local Variables
			byte* ip = (byte*) compressed;
			byte* r;

			byte* op = (byte*) decompressedBuffer;
			byte* oend = op + decompressedSize;
			byte* cpy;

			byte token;

			uint* dec = stackalloc uint[4];
			dec[0] = 0; dec[1] = 3; dec[2] = 2; dec[3] = 3;			
			int len, length;


			// Main Loop
			while (true)
			{
				// get runLength
				token = *ip++;
				if ((length=(token>>ML_BITS)) == RUN_MASK) { for (;(len=*ip++)==255;length+=255){} length += len; }


				r = op+length;
				if (r>oend-COPYLENGTH)
				{
					if (r > oend) goto _output_error;
					memcpy(op, ip, length);
					ip += length;
					break;
				}
				
				// LZ4_WILDCOPY
				do { *(uint *)op = *(uint *)ip; op+=4; ip+=4; *(uint *)op = *(uint *)ip; op+=4; ip+=4; } while (op<r);
				ip -= (op-r); op = r;


				// get offset
				r -= *(ushort *)ip; ip+=2;


				// get matchLength
				if ((length=(int)(token&ML_MASK)) == ML_MASK) { for (;*ip==255;length+=255) {ip++;} length += *ip++; }

				// copy repeated sequence
				if (op-r<COPYTOKEN)
				{
					*op++ = *r++;
					*op++ = *r++;
					*op++ = *r++;
					*op++ = *r++;
					r -= dec[op-r];
					*(uint *)op=*(uint *)r;
				} else { *(uint *)op=*(uint *)r; op+=4; r+=4; }
				cpy = op + length;
				if (cpy > oend-COPYLENGTH)
				{
					if (cpy > oend) goto _output_error;
					
					// LZ4_WILDCOPY
					byte * end = oend - COPYLENGTH;
					do { *(uint *)op = *(uint *)r; op+=4; r+=4; *(uint *)op = *(uint *)r; op+=4; r+=4; } while (op<end);
					while(op<cpy) *op++=*r++;
					op=cpy;
					if (op == oend) break;
					continue;
				}
				
				// LZ4_WILDCOPY
				do { *(uint *)op = *(uint *)r; op+=4; r+=4; *(uint *)op = *(uint *)r; op+=4; r+=4; } while (op<cpy);
				op=cpy;
			}

			// end of decoding
			return (int) (((byte*)ip)-compressed);

			// write overflow error detected
			_output_error:
				return (int) (-(((byte*)ip)-compressed));
		}

		public static byte[] Decompress(byte[] compressed)
		{
			byte[] dest = new byte[compressed.Length * 4];
			int len = Decompress(compressed, dest, dest.Length);
			byte[] d = new byte[len];
			Buffer.BlockCopy(dest,0,d,0,d.Length);
			return d;
		}

		public static int Decompress(byte[] compressed, byte[] decompressedBuffer)
		{
			return Decompress(compressed, decompressedBuffer, decompressedBuffer.Length);
		}

		public static int Decompress(byte[] compressedBuffer, byte[] decompressedBuffer, int compressedSize)
		{
			fixed(byte*src = compressedBuffer)
				fixed(byte*dst=decompressedBuffer)
				return Decompress(src,dst, compressedSize, decompressedBuffer.Length);
		}

		public static int Decompress(
			byte* compressedBuffer,
			byte* decompressedBuffer,
			int compressedSize,
			int maxDecompressedSize)
		{
			// Local Variables
			byte* ip = (byte*) compressedBuffer;
			byte* iend = ip + compressedSize;
			byte* r;

			byte* op = (byte*) decompressedBuffer;
			byte* oend = op + maxDecompressedSize;
			byte* cpy;

			byte token;

			uint* dec = stackalloc uint[4];
			dec[0] = 0; dec[1] = 3; dec[2] = 2; dec[3] = 3;			
			int len, length;


			// Main Loop
			while (ip<iend)
			{
				// get runLength
				token = *ip++;
				if ((length=(token>>ML_BITS)) == RUN_MASK) { for (;(len=*ip++)==255;length+=255){} length += len; }

				// copy literals
				r = op+length;
				if (r>oend-COPYLENGTH)
				{
					if (r > oend) goto _output_error;
					memcpy(op, ip, length);
					op += length;
					break; //Necessarily EOF
				}
				
				// LZ4_WILDCOPY
				do { *(uint *)op = *(uint *)ip; op+=4; ip+=4; *(uint *)op = *(uint *)ip; op+=4; ip+=4; } while (op<r); ip -= (op-r); op = r;
				if (ip>=iend) break; // check EOF


				// get offset
				r -= *(ushort *)ip; ip+=2;


				// get matchlength
				if ((length=(int)(token&ML_MASK)) == ML_MASK) { for (;(len=*ip++)==255;length+=255){} length += len; }

				// copy repeated sequence
				if (op-r<COPYTOKEN)
				{
					*op++ = *r++;
					*op++ = *r++;
					*op++ = *r++;
					*op++ = *r++;
					r -= dec[op-r];
					*(uint *)op=*(uint *)r;
				} else { *(uint *)op=*(uint *)r; op+=4; r+=4; }
				cpy = op + length;
				if (cpy>oend-COPYLENGTH)
				{
					if (cpy > oend) goto _output_error;
					
					//LZ4_WILDCOPY
					byte* end = (oend - COPYLENGTH);
					do { *(uint *)op = *(uint *)r; op+=4; r+=4; *(uint *)op = *(uint *)r; op+=4; r+=4; } while (op<end);
					while(op<cpy) *op++=*r++;
					op=cpy;
					if (op == oend) break; // Check EOF (should never happen, since last 5 bytes are supposed to be literals)
					continue;
				}
				do { *(uint *)op = *(uint *)r; op+=4; r+=4; *(uint *)op = *(uint *)r; op+=4; r+=4; } while (op<cpy);
				op=cpy;
			}


			return (int) (((byte*)op)-decompressedBuffer);


			_output_error:
				return (int) (-(((byte*)ip)-compressedBuffer));
		}

		static void memcpy(byte* dst, byte* src, long length)
		{
			while (length >= 16)
			{
				*(ulong*)dst = *(ulong*)src; dst += 8; src += 8;
				*(ulong*)dst = *(ulong*)src; dst += 8; src += 8;
				length -= 16;
			}
			
			while(length >= 8)
			{
				*(ulong*) dst = *(ulong*) src; dst += 8; src += 8;
				length -= 8;
			}
			
			if(length >= 4)
			{
				*(uint*) dst = *(uint*) src; dst += 4; src += 4;
				length -= 4;
			}
			
			if(length >= 2)
			{
				*(ushort*) dst = *(ushort*) src; dst += 2; src += 2;
				length -= 2;
			}
			
			if(length != 0)
				*dst = *src;
		}
		
		static int Compress(byte* source, byte* dest, int isize)
		{
			var table = new byte*[HASHTABLESIZE];			
			fixed(byte** hashTable = table)
			{
				byte* ip = (byte*) source;
				byte* anchor = ip;
				byte* iend = ip + isize;
				byte* mflimit = iend - MFLIMIT;
				byte* matchlimit = (iend - LASTLITERALS);


				byte* op = (byte*) dest;

				int len, length;
				const int skipStrength = SKIPSTRENGTH;
				uint forwardH;


				// Init
				if (isize<MINLENGTH) goto _last_literals;

				// First Byte
				hashTable[((*(uint *)ip * 2654435761U) >> ((MINMATCH*8)-HASH_LOG))] = ip;
				ip++; forwardH = ((*(uint *)ip * 2654435761U) >> ((MINMATCH*8)-HASH_LOG));

				// Main Loop
				for ( ; ; )
				{
					int findMatchAttempts = (int)(1U << skipStrength) + 3;
					byte* forwardIp = ip;
					byte* r;
					byte* token;

					// Find a match
					do
					{
						uint h = forwardH;
						int step = findMatchAttempts++ >> skipStrength;
						ip = forwardIp;
						forwardIp = ip + step;

						if (forwardIp > mflimit) { goto _last_literals; }

						forwardH = ((*(uint *)forwardIp * 2654435761U) >> ((MINMATCH*8)-HASH_LOG));
						r = hashTable[h];
						hashTable[h] = ip;

					} while ((r < ip - MAX_DISTANCE) || (*(uint *)r != *(uint *)ip));

					// Catch up
					while ((ip>anchor) && (r>(byte*)source) && (ip[-1]==r[-1])) { ip--; r--; }

					// Encode Literal Length
					length = (int)(ip - anchor);
					token = op++;
					if (length>=(int)RUN_MASK) { *token=(byte)(RUN_MASK<<ML_BITS); len = (int)(length-RUN_MASK); for(; len > 254 ; len-=255) *op++ = 255; *op++ = (byte)len; }
					else *token = (byte)(length<<ML_BITS);

					//Copy Literals
					byte* e=op+length; do { *(uint *)op = *(uint *)anchor; op+=4; anchor+=4; *(uint *)op = *(uint *)anchor; op+=4; anchor+=4; } while (op<e); op=e;


					_next_match:
						// Encode Offset
						*(ushort *)op = (ushort)(ip-r); op+=2;

					// Start Counting
					ip+=MINMATCH; r+= MINMATCH; // MinMatch verified
					anchor = ip;
					while (*(uint *)r == *(uint *)ip)
					{
						ip+=4; r+=4;
						if (ip>matchlimit-4) { r -= ip - (matchlimit-3); ip = matchlimit-3; break; }
					}
					if (*(ushort *)r == *(ushort *)ip) { ip+=2; r+=2; }
					if (*r == *ip) ip++;
					len = (int)(ip - anchor);

					// Encode MatchLength
					if (len>=(int)ML_MASK) { *token+=(byte)ML_MASK; len-=(byte)ML_MASK; for(; len > 509 ; len-=510) { *op++ = 255; *op++ = 255; } if (len > 254) { len-=255; *op++ = 255; } *op++ = (byte)len; }
					else *token += (byte)len;

					// Test end of chunk
					if (ip > mflimit) { anchor = ip; break; }

					// Fill table
					hashTable[((*(uint *)(ip-2) * 2654435761U) >> ((MINMATCH*8)-HASH_LOG))] = ip-2;

					// Test next position
					r = hashTable[(*(uint *)ip * 2654435761U) >> ((MINMATCH*8)-HASH_LOG)];
					hashTable[((*(uint *)ip * 2654435761U) >> (MINMATCH*8)-HASH_LOG)] = ip;
					if ((r > ip - (MAX_DISTANCE + 1)) && (*(uint *)r == *(uint *)ip)) { token = op++; *token=0; goto _next_match; }

					// Prepare next loop
					anchor = ip++;
					forwardH = (*(uint *)ip * 2654435761U) >> ((MINMATCH*8)-HASH_LOG);
				}

				_last_literals:
				// Encode Last Literals
				{
					int lastRun = (int)(iend - anchor);
					if (lastRun>=(int)RUN_MASK) { *op++=(byte)(RUN_MASK<<ML_BITS); lastRun-=(byte)RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *op++ = 255; *op++ = (byte) lastRun; }
					else *op++ = (byte)(lastRun<<ML_BITS);
					memcpy(op, anchor, iend - anchor);
					op += iend-anchor;
				}

				// End
				return (int) (((byte*)op)-dest);
			}
		}
			
		// Note : this function is valid only if isize < LZ4_64KLIMIT
		static int Compress64K(byte* source, byte* dest, int isize)
		{			
			var table = new ushort[HASHTABLESIZE<<1];
			fixed(ushort *hashTable = table){

				byte* ip = (byte*) source;
				byte* anchor = ip;
				byte* basep = ip;
				byte* iend = ip + isize;
				byte* mflimit = iend - MFLIMIT;
				byte* matchlimit = (iend - LASTLITERALS);
				byte* op = (byte*) dest;

				int len, length;
				const int skipStrength = SKIPSTRENGTH;
				uint forwardH;

				// Init
				if (isize<MINLENGTH) goto _last_literals;

				// First Byte
				ip++; forwardH = ((*(uint *)ip * 2654435761U) >> (int)((MINMATCH*8)-HASHLOG64K));

				// Main Loop
				for ( ; ; )
				{
					int findMatchAttempts = (int)(1U << skipStrength) + 3;
					byte* forwardIp = ip;
					byte* r;
					byte* token;

					// Find a match
					do
					{
						uint h = forwardH;
						int step = findMatchAttempts++ >> skipStrength;
						ip = forwardIp;
						forwardIp = ip + step;

						if (forwardIp > mflimit) { goto _last_literals; }

						forwardH = ((*(uint *)forwardIp * 2654435761U) >> ((MINMATCH*8)-HASHLOG64K));
						r = basep + hashTable[h];
						hashTable[h] = (ushort)(ip - basep);

					} while (*(uint *)r != *(uint *)ip);

					// Catch up
					while ((ip>anchor) && (r>(byte*)source) && (ip[-1]==r[-1])) { ip--; r--; }

					// Encode Literal Length
					length = (int)(ip - anchor);
					token = op++;
					if (length>=(int)RUN_MASK) { *token=(byte)(RUN_MASK<<ML_BITS); len = (int)(length-RUN_MASK); for(; len > 254 ; len-=255) *op++ = 255; *op++ = (byte)len; }
					else *token = (byte)(length<<ML_BITS);

					// Copy Literals
					byte* e=op+length; do { *(uint *)op = *(uint *)anchor; op+=4; anchor+=4; *(uint *)op = *(uint *)anchor; op+=4; anchor+=4; } while (op<e); op=e;


					_next_match:
						// Encode Offset
						*(ushort *)op =(ushort) (ip-r); op+=2;

					// Start Counting
					ip+=MINMATCH; r+=MINMATCH; // MinMatch verified
					anchor = ip;
					while (ip<matchlimit-3)
					{
						if (*(uint *)r == *(uint *)ip) { ip+=4; r+=4; continue; }
						if (*(ushort *)r == *(ushort *)ip) { ip+=2; r+=2; }
						if (*r == *ip) ip++;
						goto _endCount;
					}
					if ((ip<(matchlimit-1)) && (*(ushort *)r == *(ushort *)ip)) { ip+=2; r+=2; }
					if ((ip<matchlimit) && (*r == *ip)) ip++;
					_endCount:
						len = (int)(ip - anchor);

					//Encode MatchLength
					if (len>=(int)ML_MASK) { *token=(byte)(*token + ML_MASK); len=(int)(len - ML_MASK); for(; len > 509 ; len-=510) { *op++ = 255; *op++ = 255; } if (len > 254) { len-=255; *op++ = 255; } *op++ = (byte)len; }
					else *token = (byte)(*token + len);

					// Test end of chunk
					if (ip > mflimit) { anchor = ip; break; }

					// Test next position
					r = basep + hashTable[((*(uint *)ip * 2654435761U) >> ((MINMATCH*8)-HASHLOG64K))];
					hashTable[(*(uint *)ip * 2654435761U) >> (int)((MINMATCH*8)-HASHLOG64K)] = (ushort)(ip - basep);
					if ((r > ip - (MAX_DISTANCE + 1)) && (*(uint *)r == *(uint *)ip)) { token = op++; *token=0; goto _next_match; }

					// Prepare next loop
					anchor = ip++;
					forwardH = (*(uint *)ip * 2654435761U) >> ((MINMATCH*8)-HASHLOG64K);
				}

				_last_literals:

				{
					int lastRun = (int)(iend - anchor);
					if (lastRun>=(int)RUN_MASK) { *op++=(byte)(RUN_MASK<<ML_BITS); lastRun-=(byte)RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *op++ = 255; *op++ = (byte) lastRun; }
					else *op++ = (byte)(lastRun<<ML_BITS);
					memcpy(op, anchor, iend - anchor);
					op += iend-anchor;
				}


				return (int) (((byte*)op)-dest);
			}
		}

	}
}