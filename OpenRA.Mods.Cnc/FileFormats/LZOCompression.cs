#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
#region Additional Copyright & License Information
/*
 * C# port of the crude minilzo source version 2.06 by Frank Razenberg
 * The full LZO package can be found at http://www.oberhumer.com/opensource/lzo/
 *
 * Beware, you should never want to see C# code like this. You were warned.
 * I simply ran the MSVC preprocessor on the original source, changed the datatypes
 * to their C# counterpart and fixed changed some control flow stuff to amend for
 * the different goto semantics between C and C#.
 *
 * Original copyright notice is included below.
*/

/*
 * minilzo.c -- mini subset of the LZO real-time data compression library
 *
 * This file is part of the LZO real-time data compression library.
 *
 * Copyright (C) 2011 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2010 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2009 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2008 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2007 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2006 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2005 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2004 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2003 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2002 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2001 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 2000 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 1999 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 1998 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 1997 Markus Franz Xaver Johannes Oberhumer
 * Copyright (C) 1996 Markus Franz Xaver Johannes Oberhumer
 * All Rights Reserved.
 *
 * The LZO library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of
 * the License, or (at your option) any later version.
 *
 * The LZO library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with the LZO library; see the file COPYING.
 * If not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 * Markus F.X.J. Oberhumer
 * <markus@oberhumer.com>
 * http://www.oberhumer.com/opensource/lzo/
 */
#endregion

using System;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public static class LZOCompression
	{
		static unsafe int LZO1xDecompress(byte* @in, uint inLen, byte* @out, ref uint outLen, void* wrkmem)
		{
			byte* op;
			byte* ip;
			uint t;
			byte* mPos;
			var ipEnd = @in + inLen;
			outLen = 0;
			op = @out;
			ip = @in;
			var gtFirstLiteralRun = false;
			var gtMatchDone = false;
			if (*ip > 17)
			{
				t = (uint)(*ip++ - 17);
				if (t < 4)
					MatchNext(ref op, ref ip, ref t);
				else
				{
					do { *op++ = *ip++; } while (--t > 0);
					gtFirstLiteralRun = true;
				}
			}

			while (true)
			{
				if (gtFirstLiteralRun)
				{
					gtFirstLiteralRun = false;
					goto first_literal_run;
				}

				t = *ip++;
				if (t >= 16)
					goto match;

				if (t == 0)
				{
					while (*ip == 0)
					{
						t += 255;
						ip++;
					}

					t += (uint)(15 + *ip++);
				}

				*(uint*)op = *(uint*)ip;
				op += 4; ip += 4;
				if (--t > 0)
				{
					if (t >= 4)
					{
						do
						{
							*(uint*)op = *(uint*)ip;
							op += 4; ip += 4; t -= 4;
						}
						while (t >= 4);

						if (t > 0)
							do { *op++ = *ip++; } while (--t > 0);
					}
					else
						do { *op++ = *ip++; } while (--t > 0);
				}

			first_literal_run:
				t = *ip++;
				if (t >= 16)
					goto match;

				mPos = op - (1 + 0x0800);
				mPos -= t >> 2;
				mPos -= *ip++ << 2;

				*op++ = *mPos++; *op++ = *mPos++; *op++ = *mPos;
				gtMatchDone = true;

			match:
				do
				{
					if (gtMatchDone)
					{
						gtMatchDone = false;
						goto match_done;
					}

					if (t >= 64)
					{
						mPos = op - 1;
						mPos -= (t >> 2) & 7;
						mPos -= *ip++ << 3;
						t = (t >> 5) - 1;

						CopyMatch(ref op, ref mPos, ref t);
						goto match_done;
					}
					else if (t >= 32)
					{
						t &= 31;
						if (t == 0)
						{
							while (*ip == 0)
							{
								t += 255;
								ip++;
							}

							t += (uint)(31 + *ip++);
						}

						mPos = op - 1;
						mPos -= (*(ushort*)(void*)ip) >> 2;
						ip += 2;
					}
					else if (t >= 16)
					{
						mPos = op;
						mPos -= (t & 8) << 11;
						t &= 7;
						if (t == 0)
						{
							while (*ip == 0)
							{
								t += 255;
								ip++;
							}

							t += (uint)(7 + *ip++);
						}

						mPos -= (*(ushort*)ip) >> 2;
						ip += 2;
						if (mPos == op)
							goto eof_found;
						mPos -= 0x4000;
					}
					else
					{
						mPos = op - 1;
						mPos -= t >> 2;
						mPos -= *ip++ << 2;
						*op++ = *mPos++; *op++ = *mPos;
						goto match_done;
					}

					if (t >= 2 * 4 - (3 - 1) && (op - mPos) >= 4)
					{
						*(uint*)op = *(uint*)mPos;
						op += 4; mPos += 4; t -= 4 - (3 - 1);
						do
						{
							*(uint*)op = *(uint*)mPos;
							op += 4; mPos += 4; t -= 4;
						}
						while (t >= 4);

						if (t > 0)
							do { *op++ = *mPos++; } while (--t > 0);
					}
					else
					{
						// copy_match:
						*op++ = *mPos++; *op++ = *mPos++;
						do { *op++ = *mPos++; } while (--t > 0);
					}

				match_done:
					t = (uint)(ip[-2] & 3);
					if (t == 0)
						break;

					// match_next:
					*op++ = *ip++;
					if (t > 1)
					{
						*op++ = *ip++;
						if (t > 2)
							(*op++) = *ip++;
					}

					t = *ip++;
				}
				while (true);
			}

		eof_found:
			outLen = (uint)(op - @out);
			return ip == ipEnd ? 0 : (ip < ipEnd ? (-8) : (-4));
		}

		static unsafe void MatchNext(ref byte* op, ref byte* ip, ref uint t)
		{
			do { *op++ = *ip++; } while (--t > 0);
			t = *ip++;
		}

		static unsafe void CopyMatch(ref byte* op, ref byte* mPos, ref uint t)
		{
			*op++ = *mPos++; *op++ = *mPos++;
			do { *op++ = *mPos++; } while (--t > 0);
		}

		public static void DecodeInto(byte[] src, uint srcOffset, uint srcLength, byte[] dest, uint destOffset, ref uint destLength)
		{
			unsafe
			{
				fixed (byte* r = src, w = dest, wrkmem = new byte[IntPtr.Size * 16384])
				{
					LZO1xDecompress(r + srcOffset, srcLength, w + destOffset, ref destLength, wrkmem);
				}
			}
		}
	}
}
