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

using System;
using System.Linq;

namespace OpenRA.Mods.Cnc.FileFormats
{
	/* TODO: Convert this direct C port into readable code. */

	class BlowfishKeyProvider
	{
		const string PublicKeyString = "AihRvNoIbTn85FZRYNZRcT+i6KpU+maCsEqr3Q5q+LDB5tH7Tz2qQ38V";

		class PublicKey
		{
			public readonly uint[] KeyOne = new uint[64];
			public readonly uint[] KeyTwo = new uint[64];
			public uint Len;
		}

		readonly PublicKey pubkey = new PublicKey();

		readonly uint[] globOne = new uint[64];
		uint globOneBitLen, globOneLenXTwo;
		readonly uint[] globTwo = new uint[130];
		readonly uint[] globOneHigh = new uint[4];
		readonly uint[] globOneHighInv = new uint[4];
		uint globOneHighBitLen;
		uint globOneHighInvLow, globOneHighInvHigh;

		static void InitBigNum(uint[] n, uint val, uint len)
		{
			for (var i = 0; i < len; i++) n[i] = 0;
			n[0] = val;
		}

		static void MoveKeyToBig(uint[] n, byte[] key, uint klen, uint blen)
		{
			byte sign;

			if ((key[0] & 0x80) != 0) sign = 0xff;
			else sign = 0;

			unsafe
			{
				fixed (uint* tempPn = &n[0])
				{
					var pn = (byte*)tempPn;
					var i = blen * 4;
					for (; i > klen; i--) pn[i - 1] = sign;
					for (; i > 0; i--) pn[i - 1] = key[klen - i];
				}
			}
		}

		static void KeyToBigNum(uint[] n, byte[] key, uint len)
		{
			uint keylen;
			int i;

			var j = 0;

			if (key[j] != 2) return;
			j++;

			if ((key[j] & 0x80) != 0)
			{
				keylen = 0;
				for (i = 0; i < (key[j] & 0x7f); i++) keylen = (keylen << 8) | key[j + i + 1];
				j += (key[j] & 0x7f) + 1;
			}
			else
			{
				keylen = key[j];
				j++;
			}

			if (keylen <= len * 4)
				MoveKeyToBig(n, key.Skip(j).ToArray(), keylen, len);
		}

		static uint LenBigNum(uint[] n, uint len)
		{
			var i = len - 1;
			while (n[i] == 0) i--;
			return i + 1;
		}

		static uint BitLenBigNum(uint[] n, uint len)
		{
			uint ddlen, bitlen, mask;
			ddlen = LenBigNum(n, len);
			if (ddlen == 0) return 0;
			bitlen = ddlen * 32;
			mask = 0x80000000;
			while ((mask & n[ddlen - 1]) == 0)
			{
				mask >>= 1;
				bitlen--;
			}

			return bitlen;
		}

		void InitPublicKey()
		{
			InitBigNum(pubkey.KeyTwo, 0x10001, 64);

			KeyToBigNum(pubkey.KeyOne, Convert.FromBase64String(PublicKeyString), 64);
			pubkey.Len = BitLenBigNum(pubkey.KeyOne, 64) - 1;
		}

		static int CompareBigNum(uint[] n1, uint[] n2, uint len)
		{
			while (len > 0)
			{
				--len;
				if (n1[len] < n2[len]) return -1;
				if (n1[len] > n2[len]) return 1;
			}

			return 0;
		}

		static void MoveBigNum(uint[] dest, uint[] src, uint len)
		{
			Array.Copy(src, dest, len);
		}

		static void ShrBigNum(uint[] n, int bits, int len)
		{
			int i; var i2 = bits / 32;

			if (i2 > 0)
			{
				for (i = 0; i < len - i2; i++) n[i] = n[i + i2];
				for (; i < len; i++) n[i] = 0;
				bits %= 32;
			}

			if (bits == 0) return;
			for (i = 0; i < len - 1; i++) n[i] = (n[i] >> bits) | (n[i + 1] << (32 - bits));
			n[i] = n[i] >> bits;
		}

		static void ShlBigNum(uint[] n, int bits, int len)
		{
			int i, i2;

			i2 = bits / 32;
			if (i2 > 0)
			{
				for (i = len - 1; i > i2; i--) n[i] = n[i - i2];
				for (; i > 0; i--) n[i] = 0;
				bits %= 32;
			}

			if (bits == 0) return;
			for (i = len - 1; i > 0; i--) n[i] = (n[i] << bits) | (n[i - 1] >> (32 - bits));
			n[0] <<= bits;
		}

		static uint SubBigNum(uint[] dest, uint[] src1, uint[] src2, uint carry, int len)
		{
			uint i1, i2;

			len += len;
			unsafe
			{
				fixed (uint* tempPs1 = &src1[0])
				fixed (uint* tempPs2 = &src2[0])
				fixed (uint* tempPd = &dest[0])
				{
					var ps1 = (ushort*)tempPs1;
					var ps2 = (ushort*)tempPs2;
					var pd = (ushort*)tempPd;

					while (--len != -1)
					{
						i1 = *ps1++;
						i2 = *ps2++;
						*pd++ = (ushort)(i1 - i2 - carry);
						if (((i1 - i2 - carry) & 0x10000) != 0) carry = 1; else carry = 0;
					}
				}
			}

			return carry;
		}

		static unsafe uint SubBigNum(uint* dest, uint* src1, uint* src2, uint carry, int len)
		{
			uint i1, i2;

			len += len;

			var ps1 = (ushort*)src1;
			var ps2 = (ushort*)src2;
			var pd = (ushort*)dest;

			while (--len != -1)
			{
				i1 = *ps1++;
				i2 = *ps2++;
				*pd++ = (ushort)(i1 - i2 - carry);
				if (((i1 - i2 - carry) & 0x10000) != 0) carry = 1; else carry = 0;
			}

			return carry;
		}

		static void InvertBigNum(uint[] n1, uint[] n2, uint len)
		{
			var nTmp = new uint[64];
			uint nTwoByteLen, bit;
			int nTwoBitLen;

			InitBigNum(nTmp, 0, len);
			InitBigNum(n1, 0, len);
			nTwoBitLen = (int)BitLenBigNum(n2, len);
			bit = 1U << (nTwoBitLen % 32);
			var j = ((nTwoBitLen + 32) / 32) - 1;
			nTwoByteLen = (uint)((nTwoBitLen - 1) / 32) * 4;
			nTmp[nTwoByteLen / 4] |= 1U << ((nTwoBitLen - 1) & 0x1f);

			while (nTwoBitLen > 0)
			{
				nTwoBitLen--;
				ShlBigNum(nTmp, 1, (int)len);
				if (CompareBigNum(nTmp, n2, len) != -1)
				{
					SubBigNum(nTmp, nTmp, n2, 0, (int)len);
					n1[j] |= bit;
				}

				bit >>= 1;
				if (bit == 0)
				{
					j--;
					bit = 0x80000000;
				}
			}

			InitBigNum(nTmp, 0, len);
		}

		static void IncrementBigNum(uint[] n, uint len)
		{
			var i = 0;
			while ((++n[i] == 0) && (--len > 0)) i++;
		}

		void InitTwoDw(uint[] n, uint len)
		{
			MoveBigNum(globOne, n, len);
			globOneBitLen = BitLenBigNum(globOne, len);
			globOneLenXTwo = (globOneBitLen + 15) / 16;
			MoveBigNum(globOneHigh, globOne.Skip((int)LenBigNum(globOne, len) - 2).ToArray(), 2);
			globOneHighBitLen = BitLenBigNum(globOneHigh, 2) - 32;
			ShrBigNum(globOneHigh, (int)globOneHighBitLen, 2);
			InvertBigNum(globOneHighInv, globOneHigh, 2);
			ShrBigNum(globOneHighInv, 1, 2);
			globOneHighBitLen = (globOneHighBitLen + 15) % 16 + 1;
			IncrementBigNum(globOneHighInv, 2);
			if (BitLenBigNum(globOneHighInv, 2) > 32)
			{
				ShrBigNum(globOneHighInv, 1, 2);
				globOneHighBitLen--;
			}

			globOneHighInvLow = (ushort)globOneHighInv[0];
			globOneHighInvHigh = (ushort)(globOneHighInv[0] >> 16);
		}

		static unsafe void MulBignumWord(ushort* pn1, uint[] n2, uint mul, uint len)
		{
			uint i, tmp;
			unsafe
			{
				fixed (uint* tempPn2 = &n2[0])
				{
					var pn2 = (ushort*)tempPn2;

					tmp = 0;
					for (i = 0; i < len; i++)
					{
						tmp = mul * (*pn2) + (*pn1) + tmp;
						*pn1 = (ushort)tmp;
						pn1++;
						pn2++;
						tmp >>= 16;
					}

					*pn1 += (ushort)tmp;
				}
			}
		}

		static void MulBigNum(uint[] dest, uint[] src1, uint[] src2, uint len)
		{
			uint i;

			unsafe
			{
				fixed (uint* tempSrc2 = &src2[0])
				fixed (uint* tempPdest = &dest[0])
				{
					var psrc2 = (ushort*)tempSrc2;
					var pdest = (ushort*)tempPdest;

					InitBigNum(dest, 0, len * 2);
					for (i = 0; i < len * 2; i++)
						MulBignumWord(pdest++, src1, *psrc2++, len * 2);
				}
			}
		}

		static void NotBigNum(uint[] n, uint len)
		{
			uint i;
			for (i = 0; i < len; i++) n[i] = ~n[i];
		}

		static void NegBigNum(uint[] n, uint len)
		{
			NotBigNum(n, len);
			IncrementBigNum(n, len);
		}

		unsafe uint GetMulWord(uint* n)
		{
			var wn = (ushort*)n;
			var i = (uint)((((((((((*(wn - 1) ^ 0xffff) & 0xffff) * globOneHighInvLow + 0x10000) >> 1)
				 + (((*(wn - 2) ^ 0xffff) * globOneHighInvHigh + globOneHighInvHigh) >> 1) + 1)
				 >> 16) + ((((*(wn - 1) ^ 0xffff) & 0xffff) * globOneHighInvHigh) >> 1) +
				 (((*wn ^ 0xffff) * globOneHighInvLow) >> 1) + 1) >> 14) + globOneHighInvHigh
				 * (*wn ^ 0xffff) * 2) >> (int)globOneHighBitLen);
			if (i > 0xffff) i = 0xffff;
			return i & 0xffff;
		}

		static void DecBigNum(uint[] n, uint len)
		{
			var i = 0;
			while ((--n[i] == 0xffffffff) && (--len > 0))
				i++;
		}

		void CalcBigNum(uint[] n1, uint[] n2, uint[] n3, uint len)
		{
			uint globTwoXtwo, lenDiff;
			unsafe
			{
				fixed (uint* g1 = &globOne[0])
				fixed (uint* g2 = &globTwo[0])
				{
					MulBigNum(globTwo, n2, n3, len);
					globTwo[len * 2] = 0;
					globTwoXtwo = LenBigNum(globTwo, len * 2 + 1) * 2;
					if (globTwoXtwo >= globOneLenXTwo)
					{
						IncrementBigNum(globTwo, len * 2 + 1);
						NegBigNum(globTwo, len * 2 + 1);
						lenDiff = globTwoXtwo + 1 - globOneLenXTwo;
						var esi = ((ushort*)g2) + (1 + globTwoXtwo - globOneLenXTwo);
						var edi = ((ushort*)g2) + (globTwoXtwo + 1);
						for (; lenDiff != 0; lenDiff--)
						{
							edi--;
							var tmp = GetMulWord((uint*)edi);
							esi--;
							if (tmp > 0)
							{
								MulBignumWord(esi, globOne, tmp, 2 * len);
								if ((*edi & 0x8000) == 0)
								{
									if (SubBigNum((uint*)esi, (uint*)esi, g1, 0, (int)len) != 0)
										(*edi)--;
								}
							}
						}

						NegBigNum(globTwo, len);
						DecBigNum(globTwo, len);
					}

					MoveBigNum(n1, globTwo, len);
				}
			}
		}

		void ClearTempVars(uint len)
		{
			InitBigNum(globOne, 0, len);
			InitBigNum(globTwo, 0, len);
			InitBigNum(globOneHighInv, 0, 4);
			InitBigNum(globOneHigh, 0, 4);
			globOneBitLen = 0;
			globOneHighBitLen = 0;
			globOneLenXTwo = 0;
			globOneHighInvLow = 0;
			globOneHighInvHigh = 0;
		}

		void CalcKey(uint[] n1, uint[] n2, uint[] n3, uint[] n4, uint len)
		{
			var n_tmp = new uint[64];
			uint n3_len, n4_len;
			int n3_bitlen;
			uint bit_mask;

			unsafe
			{
				fixed (uint* tempPn3 = &n3[0])
				{
					var pn3 = tempPn3;

					InitBigNum(n1, 1, len);
					n4_len = LenBigNum(n4, len);
					InitTwoDw(n4, n4_len);
					n3_bitlen = (int)BitLenBigNum(n3, n4_len);
					n3_len = (uint)((n3_bitlen + 31) / 32);
					bit_mask = (1U << ((n3_bitlen - 1) % 32)) >> 1;
					pn3 += n3_len - 1;
					n3_bitlen--;
					MoveBigNum(n1, n2, n4_len);
					while (--n3_bitlen != -1)
					{
						if (bit_mask == 0)
						{
							bit_mask = 0x80000000;
							pn3--;
						}

						CalcBigNum(n_tmp, n1, n1, n4_len);
						if ((*pn3 & bit_mask) != 0)
							CalcBigNum(n1, n_tmp, n2, n4_len);
						else
							MoveBigNum(n1, n_tmp, n4_len);
						bit_mask >>= 1;
					}

					InitBigNum(n_tmp, 0, n4_len);
					ClearTempVars(len);
				}
			}
		}

		byte[] ProcessPredata(byte[] src)
		{
			var dest = new byte[256];
			var n2 = new uint[64];
			var n3 = new uint[64];

			var a = (int)((pubkey.Len - 1) / 8);
			var pre_len = (55 / a + 1) * (a + 1);
			var srcOffset = 0;
			var destOffset = 0;

			while (a + 1 <= pre_len)
			{
				InitBigNum(n2, 0, 64);

				Buffer.BlockCopy(src, srcOffset, n2, 0, a + 1);
				CalcKey(n3, n2, pubkey.KeyTwo, pubkey.KeyOne, 64);
				Buffer.BlockCopy(n3, 0, dest, destOffset, a);

				pre_len -= a + 1;
				srcOffset += a + 1;
				destOffset += a;
			}

			return dest;
		}

		public byte[] DecryptKey(byte[] src)
		{
			InitPublicKey();
			return ProcessPredata(src).Take(56).ToArray();
		}
	}
}
