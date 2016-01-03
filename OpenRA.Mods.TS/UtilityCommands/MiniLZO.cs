
#region Copyright notice
/* C# port of the crude minilzo source version 2.06 by Frank Razenberg
 
  Beware, you should never want to see C# code like this. You were warned.
  I simply ran the MSVC preprocessor on the original source, changed the datatypes 
  to their C# counterpart and fixed changed some control flow stuff to amend for
  the different goto semantics between C and C#.

  Original copyright notice is included below.
*/

/* minilzo.c -- mini subset of the LZO real-time data compression library

   This file is part of the LZO real-time data compression library.

   Copyright (C) 2011 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2010 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2009 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2008 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2007 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2006 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2005 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2004 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2003 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2002 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2001 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2000 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1999 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1998 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1997 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1996 Markus Franz Xaver Johannes Oberhumer
   All Rights Reserved.

   The LZO library is free software; you can redistribute it and/or
   modify it under the terms of the GNU General Public License as
   published by the Free Software Foundation; either version 2 of
   the License, or (at your option) any later version.

   The LZO library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with the LZO library; see the file COPYING.
   If not, write to the Free Software Foundation, Inc.,
   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

   Markus F.X.J. Oberhumer
   <markus@oberhumer.com>
   http://www.oberhumer.com/opensource/lzo/
 */

/*
 * NOTE:
 *   the full LZO package can be found at
 *   http://www.oberhumer.com/opensource/lzo/
 */

#endregion

using System;

namespace OpenRA.Mods.TS.UtilityCommands
{
	public static class MiniLZO {

		unsafe static uint lzo1x_1_compress_core(byte* @in, uint in_len, byte* @out, ref uint out_len, uint ti, void* wrkmem) {
			byte* ip;
			byte* op;
			byte* in_end = @in + in_len;
			byte* ip_end = @in + in_len - 20;
			byte* ii;
			ushort* dict = (ushort*)wrkmem;
			op = @out;
			ip = @in;
			ii = ip;
			ip += ti < 4 ? 4 - ti : 0;

			byte* m_pos;
			uint m_off;
			uint m_len;

			for (; ; ) {

				uint dv;
				uint dindex;
			literal:
				ip += 1 + ((ip - ii) >> 5);
			next:
				if (ip >= ip_end)
					break;
				dv = (*(uint*)(void*)(ip));
				dindex = ((uint)(((((((uint)((0x1824429d) * (dv)))) >> (32 - 14))) & (((1u << (14)) - 1) >> (0))) << (0)));
				m_pos = @in + dict[dindex];
				dict[dindex] = ((ushort)((uint)((ip) - (@in))));
				if (dv != (*(uint*)(void*)(m_pos)))
					goto literal;

				ii -= ti; ti = 0;
				{
					uint t = ((uint)((ip) - (ii)));
					if (t != 0) {
						if (t <= 3) {
							op[-2] |= ((byte)(t));
							*(uint*)(op) = *(uint*)(ii);
							op += t;
						}
						else if (t <= 16) {
							*op++ = ((byte)(t - 3));
							*(uint*)(op) = *(uint*)(ii);
							*(uint*)(op + 4) = *(uint*)(ii + 4);
							*(uint*)(op + 8) = *(uint*)(ii + 8);
							*(uint*)(op + 12) = *(uint*)(ii + 12);
							op += t;
						}
						else {
							if (t <= 18)
								*op++ = ((byte)(t - 3));
							else {
								uint tt = t - 18;
								*op++ = 0;
								while (tt > 255) {
									tt -= 255;
									*(byte*)op++ = 0;
								}

								*op++ = ((byte)(tt));
							}
							do {
								*(uint*)(op) = *(uint*)(ii);
								*(uint*)(op + 4) = *(uint*)(ii + 4);
								*(uint*)(op + 8) = *(uint*)(ii + 8);
								*(uint*)(op + 12) = *(uint*)(ii + 12);
								op += 16; ii += 16; t -= 16;
							} while (t >= 16); if (t > 0) { do *op++ = *ii++; while (--t > 0); }
						}
					}
				}
				m_len = 4;
				{
					uint v;
					v = (*(uint*)(void*)(ip + m_len)) ^ (*(uint*)(void*)(m_pos + m_len));
					if (v == 0) {
						do {
							m_len += 4;
							v = (*(uint*)(void*)(ip + m_len)) ^ (*(uint*)(void*)(m_pos + m_len));
							if (ip + m_len >= ip_end)
								goto m_len_done;
						} while (v == 0);
					}
					m_len += (uint)lzo_bitops_ctz32(v) / 8;
				}
			m_len_done:
				m_off = ((uint)((ip) - (m_pos)));
				ip += m_len;
				ii = ip;
				if (m_len <= 8 && m_off <= 0x0800) {
					m_off -= 1;
					*op++ = ((byte)(((m_len - 1) << 5) | ((m_off & 7) << 2)));
					*op++ = ((byte)(m_off >> 3));
				}
				else if (m_off <= 0x4000) {
					m_off -= 1;
					if (m_len <= 33)
						*op++ = ((byte)(32 | (m_len - 2)));
					else {
						m_len -= 33;
						*op++ = 32 | 0;
						while (m_len > 255) {
							m_len -= 255;
							*(byte*)op++ = 0;
						}
						*op++ = ((byte)(m_len));
					}
					*op++ = ((byte)(m_off << 2));
					*op++ = ((byte)(m_off >> 6));
				}
				else {
					m_off -= 0x4000;
					if (m_len <= 9)
						*op++ = ((byte)(16 | ((m_off >> 11) & 8) | (m_len - 2)));
					else {
						m_len -= 9;
						*op++ = ((byte)(16 | ((m_off >> 11) & 8)));
						while (m_len > 255) {
							m_len -= 255;
							*(byte*)op++ = 0;
						}
						*op++ = ((byte)(m_len));
					}
					*op++ = ((byte)(m_off << 2));
					*op++ = ((byte)(m_off >> 6));
				}
				goto next;
			}
			out_len = ((uint)((op) - (@out)));
			return ((uint)((in_end) - (ii - ti)));
		}

		static int[] MultiplyDeBruijnBitPosition = {
			  0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 
			  31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
			};
		private static int lzo_bitops_ctz32(uint v) {
			return MultiplyDeBruijnBitPosition[((uint)((v & -v) * 0x077CB531U)) >> 27];
		}

		unsafe static int lzo1x_1_compress(byte* @in, uint in_len, byte* @out, ref uint out_len, byte* wrkmem) {
			byte* ip = @in;
			byte* op = @out;
			uint l = in_len;
			uint t = 0;
			while (l > 20) {
				uint ll = l;
				ulong ll_end;
				ll = ((ll) <= (49152) ? (ll) : (49152));
				ll_end = (ulong)ip + ll;
				if ((ll_end + ((t + ll) >> 5)) <= ll_end || (byte*)(ll_end + ((t + ll) >> 5)) <= ip + ll)
					break;

				for (int i = 0; i < (1 << 14) * sizeof(ushort); i++)
					wrkmem[i] = 0;
				t = lzo1x_1_compress_core(ip, ll, op, ref out_len, t, wrkmem);
				ip += ll;
				op += out_len;
				l -= ll;
			}
			t += l;
			if (t > 0) {
				byte* ii = @in + in_len - t;
				if (op == @out && t <= 238)
					*op++ = ((byte)(17 + t));
				else if (t <= 3)
					op[-2] |= ((byte)(t));
				else if (t <= 18)
					*op++ = ((byte)(t - 3));
				else {
					uint tt = t - 18;
					*op++ = 0;
					while (tt > 255) {
						tt -= 255;
						*(byte*)op++ = 0;
					}

					*op++ = ((byte)(tt));
				}
				do *op++ = *ii++; while (--t > 0);
			}
			*op++ = 16 | 1;
			*op++ = 0;
			*op++ = 0;
			out_len = ((uint)((op) - (@out)));
			return 0;
		}

		public unsafe static int lzo1x_decompress(byte* @in, uint in_len, byte* @out, ref uint out_len, void* wrkmem) {
			byte* op;
			byte* ip;
			uint t;
			byte* m_pos;
			byte* ip_end = @in + in_len;
			out_len = 0;
			op = @out;
			ip = @in;
			bool gt_first_literal_run = false;
			bool gt_match_done = false;
			if (*ip > 17) {
				t = (uint)(*ip++ - 17);
				if (t < 4) {
					match_next(ref op, ref ip, ref t);
				}
				else {
					do *op++ = *ip++; while (--t > 0);
					gt_first_literal_run = true;
				}
			}
			while (true) {
				if (gt_first_literal_run) {
					gt_first_literal_run = false;
					goto first_literal_run;
				}

				t = *ip++;
				if (t >= 16)
					goto match;
				if (t == 0) {
					while (*ip == 0) {
						t += 255;
						ip++;
					}
					t += (uint)(15 + *ip++);
				}
				*(uint*)op = *(uint*)ip;
				op += 4; ip += 4;
				if (--t > 0) {
					if (t >= 4) {
						do {
							*(uint*)op = *(uint*)ip;
							op += 4; ip += 4; t -= 4;
						} while (t >= 4);
						if (t > 0) do *op++ = *ip++; while (--t > 0);
					}
					else
						do *op++ = *ip++; while (--t > 0);
				}
			first_literal_run:
				t = *ip++;
				if (t >= 16)
					goto match;
				m_pos = op - (1 + 0x0800);
				m_pos -= t >> 2;
				m_pos -= *ip++ << 2;

				*op++ = *m_pos++; *op++ = *m_pos++; *op++ = *m_pos;
				gt_match_done = true;

			match:
				do {
					if (gt_match_done) {
						gt_match_done = false;
						goto match_done;
						;
					}
					if (t >= 64) {
						m_pos = op - 1;
						m_pos -= (t >> 2) & 7;
						m_pos -= *ip++ << 3;
						t = (t >> 5) - 1;

						copy_match(ref op, ref m_pos, ref t);
						goto match_done;
					}
					else if (t >= 32) {
						t &= 31;
						if (t == 0) {
							while (*ip == 0) {
								t += 255;
								ip++;
							}
							t += (uint)(31 + *ip++);
						}
						m_pos = op - 1;
						m_pos -= (*(ushort*)(void*)(ip)) >> 2;
						ip += 2;
					}
					else if (t >= 16) {
						m_pos = op;
						m_pos -= (t & 8) << 11;
						t &= 7;
						if (t == 0) {
							while (*ip == 0) {
								t += 255;
								ip++;
							}
							t += (uint)(7 + *ip++);
						}
						m_pos -= (*(ushort*)ip) >> 2;
						ip += 2;
						if (m_pos == op)
							goto eof_found;
						m_pos -= 0x4000;
					}
					else {
						m_pos = op - 1;
						m_pos -= t >> 2;
						m_pos -= *ip++ << 2;
						*op++ = *m_pos++; *op++ = *m_pos;
						goto match_done;
					}

					if (t >= 2 * 4 - (3 - 1) && (op - m_pos) >= 4) {
						*(uint*)op = *(uint*)m_pos;
						op += 4; m_pos += 4; t -= 4 - (3 - 1);
						do {
							*(uint*)op = *(uint*)m_pos;
							op += 4; m_pos += 4; t -= 4;
						} while (t >= 4);
						if (t > 0) do *op++ = *m_pos++; while (--t > 0);
					}
					else {
					// copy_match:
						*op++ = *m_pos++; *op++ = *m_pos++;
						do *op++ = *m_pos++; while (--t > 0);
					}
				match_done:
					t = (uint)(ip[-2] & 3);
					if (t == 0)
						break;
				// match_next:
					*op++ = *ip++;
					if (t > 1) { *op++ = *ip++; if (t > 2) { *op++ = *ip++; } }
					t = *ip++;
				} while (true);
			}
		eof_found:

			out_len = ((uint)((op) - (@out)));
			return (ip == ip_end ? 0 :
				   (ip < ip_end ? (-8) : (-4)));
		}

		private static unsafe void match_next(ref byte* op, ref byte* ip, ref uint t) {
			do *op++ = *ip++; while (--t > 0);
			t = *ip++;
		}

		private static unsafe void copy_match(ref byte* op, ref byte* m_pos, ref uint t) {
			*op++ = *m_pos++; *op++ = *m_pos++;
			do *op++ = *m_pos++; while (--t > 0);
		}



		public static unsafe byte[] Decompress(byte[] @in, byte[] @out) {
			uint out_len = 0;
			fixed (byte* @pIn = @in, wrkmem = new byte[IntPtr.Size * 16384], pOut = @out) {
				lzo1x_decompress(pIn, (uint)@in.Length, @pOut, ref @out_len, wrkmem);
			}
			return @out;
		}

		public static unsafe void Decompress(byte* r, uint size_in, byte* w, ref uint size_out) {
			fixed (byte* wrkmem = new byte[IntPtr.Size * 16384]) {
				lzo1x_decompress(r, size_in, w, ref size_out, wrkmem);
			}
		}

		public static unsafe byte[] Compress(byte[] input) {
			byte[] @out = new byte[input.Length + (input.Length / 16) + 64 + 3];
			uint out_len = 0;
			fixed (byte* @pIn = input, wrkmem = new byte[IntPtr.Size * 16384], pOut = @out) {
				lzo1x_1_compress(pIn, (uint)input.Length, @pOut, ref @out_len, wrkmem);
			}
			Array.Resize(ref @out, (int)out_len);
			return @out;
		}

		public static unsafe void Compress(byte* r, uint size_in, byte* w, ref uint size_out) {
			fixed (byte* wrkmem = new byte[IntPtr.Size * 16384]) {
				lzo1x_1_compress(r, size_in, w, ref size_out, wrkmem);
			}
		}
	}
}
