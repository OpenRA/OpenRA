using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace OpenRa.FileFormats
{
    /* possibly the fugliest C# i've ever seen. */

    class BlowfishKeyProvider
    {
        const string pubkeyStr = "AihRvNoIbTn85FZRYNZRcT+i6KpU+maCsEqr3Q5q+LDB5tH7Tz2qQ38V";

        static sbyte[] char2num = {
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,
    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,
    -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
    15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,
    -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
    41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};

        class PublicKey
        {
            public uint[] key1 = new uint[64];
            public uint[] key2 = new uint[64];
            public uint len;
        }
        PublicKey pubkey = new PublicKey();

        uint[] glob1 = new uint[64];
        uint glob1_bitlen, glob1_len_x2;
        uint[] glob2 = new uint[130];
        uint[] glob1_hi = new uint[4];
        uint[] glob1_hi_inv = new uint[4];
        uint glob1_hi_bitlen;
        uint glob1_hi_inv_lo, glob1_hi_inv_hi;

        void init_bignum(uint[] n, uint val, uint len)
        {
            for (int i = 0; i < len; i++) n[i] = 0;
            n[0] = val;
        }

        void move_key_to_big(uint[] n, byte[] key, uint klen, uint blen)
        {
            byte sign;

            if ((key[0] & 0x80) != 0) sign = 0xff;
            else sign = 0;

            unsafe
            {
                fixed (uint* _pn = &n[0])
                {
                    byte* pn = (byte*)_pn;
                    uint i = blen * 4;
                    for (; i > klen; i--) pn[i - 1] = (byte)sign;
                    for (; i > 0; i--) pn[i - 1] = key[klen - i];
                }
            }
        }

        void key_to_bignum(uint[] n, byte[] key, uint len)
        {
            uint keylen;
            int i;

            int j = 0;

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
                move_key_to_big(n, key.Skip(j).ToArray(), keylen, len);
        }

        uint len_bignum(uint[] n, uint len)
        {
            uint i;
            i = len - 1;
            while ((i >= 0) && (n[i] == 0)) i--;
            return i + 1;
        }

        uint bitlen_bignum(uint[] n, uint len)
        {
            uint ddlen, bitlen, mask;
            ddlen = len_bignum(n, len);
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

        void init_pubkey()
        {
            int i = 0;
            uint i2, tmp;
            byte[] keytmp = new byte[256];

            init_bignum(pubkey.key2, 0x10001, 64);

            i2 = 0;
            while (i < pubkeyStr.Length)
            {
                tmp = (uint)char2num[pubkeyStr[i++]];
                tmp <<= 6; tmp |= (uint)(byte)char2num[pubkeyStr[i++]];
                tmp <<= 6; tmp |= (uint)(byte)char2num[pubkeyStr[i++]];
                tmp <<= 6; tmp |= (uint)(byte)char2num[pubkeyStr[i++]];
                keytmp[i2++] = (byte)((tmp >> 16) & 0xff);
                keytmp[i2++] = (byte)((tmp >> 8) & 0xff);
                keytmp[i2++] = (byte)(tmp & 0xff);
            }

            key_to_bignum(pubkey.key1, keytmp, 64);
            pubkey.len = bitlen_bignum(pubkey.key1, 64) - 1;
        }

        uint len_predata()
        {
            uint a = (pubkey.len - 1) / 8;
            return (55 / a + 1) * (a + 1);
        }

        int cmp_bignum(uint[] n1, uint[] n2, uint len)
        {

            while (len > 0)
            {
                --len;
                if (n1[len] < n2[len]) return -1;
                if (n1[len] > n2[len]) return 1;
            }
            return 0;
        }

        void mov_bignum(uint[] dest, uint[] src, uint len)
        {
            Array.Copy(src, dest, len);
        }

        void shr_bignum(uint[] n, int bits, int len)
        {
            int i; int i2 = bits / 32;

            if (i2 > 0)
            {
                for (i = 0; i < len - i2; i++) n[i] = n[i + i2];
                for (; i < len; i++) n[i] = 0;
                bits = bits % 32;
            }
            if (bits == 0) return;
            for (i = 0; i < len - 1; i++) n[i] = (n[i] >> bits) | (n[i + 1] << (32 -
          bits));
            n[i] = n[i] >> bits;
        }

        void shl_bignum(uint[] n, int bits, int len)
        {
            int i, i2;

            i2 = bits / 32;
            if (i2 > 0)
            {
                for (i = len - 1; i > i2; i--) n[i] = n[i - i2];
                for (; i > 0; i--) n[i] = 0;
                bits = bits % 32;
            }
            if (bits == 0) return;
            for (i = len - 1; i > 0; i--) n[i] = (n[i] << bits) | (n[i - 1] >> (32 -
          bits));
            n[0] <<= bits;
        }

        uint sub_bignum(uint[] dest, uint[] src1, uint[] src2, uint carry, int len)
        {
            uint i1, i2;

            len += len;
            unsafe
            {
                fixed (uint* _ps1 = &src1[0])
                fixed (uint* _ps2 = &src2[0])
                fixed (uint* _pd = &dest[0])
                {
                    ushort* ps1 = (ushort*)_ps1;
                    ushort* ps2 = (ushort*)_ps2;
                    ushort* pd = (ushort*)_pd;

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

        unsafe uint sub_bignum(uint* dest, uint* src1, uint* src2, uint carry, int len)
        {
            uint i1, i2;

            len += len;

            ushort* ps1 = (ushort*)src1;
            ushort* ps2 = (ushort*)src2;
            ushort* pd = (ushort*)dest;

            while (--len != -1)
            {
                i1 = *ps1++;
                i2 = *ps2++;
                *pd++ = (ushort)(i1 - i2 - carry);
                if (((i1 - i2 - carry) & 0x10000) != 0) carry = 1; else carry = 0;

            }
            return carry;
        }

        void inv_bignum(uint[] n1, uint[] n2, uint len)
        {
            uint[] n_tmp = new uint[64];
            uint n2_bytelen, bit;
            int n2_bitlen;

            int j = 0;

            init_bignum(n_tmp, 0, len);
            init_bignum(n1, 0, len);
            n2_bitlen = (int)bitlen_bignum(n2, len);
            bit = ((uint)1) << (n2_bitlen % 32);
            j = ((n2_bitlen + 32) / 32) - 1;
            n2_bytelen = (uint)((n2_bitlen - 1) / 32) * 4;
            n_tmp[n2_bytelen / 4] |= ((uint)1) << ((n2_bitlen - 1) & 0x1f);

            while (n2_bitlen > 0)
            {
                n2_bitlen--;
                shl_bignum(n_tmp, 1, (int)len);
                if (cmp_bignum(n_tmp, n2, len) != -1)
                {
                    sub_bignum(n_tmp, n_tmp, n2, 0, (int)len);
                    n1[j] |= bit;
                }
                bit >>= 1;
                if (bit == 0)
                {
                    j--;
                    bit = 0x80000000;
                }
            }
            init_bignum(n_tmp, 0, len);
        }

        void inc_bignum(uint[] n, uint len)
        {
            int i = 0;
            while ((++n[i] == 0) && (--len > 0)) i++;
        }

        void init_two_dw(uint[] n, uint len)
        {
            mov_bignum(glob1, n, len);
            glob1_bitlen = bitlen_bignum(glob1, len);
            glob1_len_x2 = (glob1_bitlen + 15) / 16;
            mov_bignum(glob1_hi, glob1.Skip((int)len_bignum(glob1, len) - 2).ToArray(), 2);
            glob1_hi_bitlen = bitlen_bignum(glob1_hi, 2) - 32;
            shr_bignum(glob1_hi, (int)glob1_hi_bitlen, 2);
            inv_bignum(glob1_hi_inv, glob1_hi, 2);
            shr_bignum(glob1_hi_inv, 1, 2);
            glob1_hi_bitlen = (glob1_hi_bitlen + 15) % 16 + 1;
            inc_bignum(glob1_hi_inv, 2);
            if (bitlen_bignum(glob1_hi_inv, 2) > 32)
            {
                shr_bignum(glob1_hi_inv, 1, 2);
                glob1_hi_bitlen--;
            }
            glob1_hi_inv_lo = (ushort)glob1_hi_inv[0];
            glob1_hi_inv_hi = (ushort)(glob1_hi_inv[0] >> 16);
        }

        unsafe void mul_bignum_word(ushort *pn1, uint[] n2, uint mul, uint len)
        {
            uint i, tmp;
            unsafe
            {
                fixed (uint* _pn2 = &n2[0])
                {
                    ushort* pn2 = (ushort*)_pn2;

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

        void mul_bignum(uint[] dest, uint[] src1, uint[] src2, uint len)
        {
          uint i;

          unsafe
          {
              fixed( uint * _psrc2 = &src2[0] )
              fixed(uint* _pdest = &dest[0])
              {
                  ushort* psrc2 = (ushort*)_psrc2;
                  ushort* pdest = (ushort*)_pdest;

                  init_bignum(dest, 0, len * 2);
                  for (i = 0; i < len * 2; i++)
                      mul_bignum_word(pdest++, src1, *psrc2++, len * 2);
              }
          }
        }

        void not_bignum(uint[] n, uint len)
        {
          uint i;
          for (i = 0; i < len; i++) n[i] = ~n[i];
        }

        void neg_bignum(uint[] n, uint len)
        {
          not_bignum(n, len);
          inc_bignum(n, len);
        }

        unsafe uint get_mulword(uint* n)
        {
            ushort* wn = (ushort*)n;
            uint i = (uint)((((((((((*(wn - 1) ^ 0xffff) & 0xffff) * glob1_hi_inv_lo + 0x10000) >> 1)
                 + (((*(wn - 2) ^ 0xffff) * glob1_hi_inv_hi + glob1_hi_inv_hi) >> 1) + 1)
                 >> 16) + ((((*(wn - 1) ^ 0xffff) & 0xffff) * glob1_hi_inv_hi) >> 1) +
                 (((*wn ^ 0xffff) * glob1_hi_inv_lo) >> 1) + 1) >> 14) + glob1_hi_inv_hi
                 * (*wn ^ 0xffff) * 2) >> (int)glob1_hi_bitlen);
            if (i > 0xffff) i = 0xffff;
            return i & 0xffff;
        }

        void dec_bignum(uint[] n, uint len)
        {
            int i = 0;
            while ((--n[i] == 0xffffffff) && (--len > 0)) 
                i++;
        }

        void calc_a_bignum(uint[] n1, uint[] n2, uint[] n3, uint len)
        {
            uint g2_len_x2, len_diff;
            unsafe
            {
                fixed( uint* g1 = &glob1[0])
                fixed (uint* g2 = &glob2[0])
                {
                    mul_bignum(glob2, n2, n3, len);
                    glob2[len * 2] = 0;
                    g2_len_x2 = len_bignum(glob2, len * 2 + 1) * 2;
                    if (g2_len_x2 >= glob1_len_x2)
                    {
                        inc_bignum(glob2, len * 2 + 1);
                        neg_bignum(glob2, len * 2 + 1);
                        len_diff = g2_len_x2 + 1 - glob1_len_x2;
                        ushort* esi = ((ushort*)g2) + (1 + g2_len_x2 - glob1_len_x2);
                        ushort* edi = ((ushort*)g2) + (g2_len_x2 + 1);
                        for (; len_diff != 0; len_diff--)
                        {
                            edi--;
                            uint tmp = get_mulword((uint*)edi);
                            esi--;
                            if (tmp > 0)
                            {
                                mul_bignum_word(esi, glob1, tmp, 2 * len);
                                if ((*edi & 0x8000) == 0)
                                {
                                    if (0 != sub_bignum((uint*)esi, (uint*)esi, g1, 0, (int)len)) (*edi)--;
                                }
                            }
                        }
                        neg_bignum(glob2, len);
                        dec_bignum(glob2, len);
                    }
                    mov_bignum(n1, glob2, len);
                }
            }
        }

        void clear_tmp_vars(uint len)
        {
            init_bignum(glob1, 0, len);
            init_bignum(glob2, 0, len);
            init_bignum(glob1_hi_inv, 0, 4);
            init_bignum(glob1_hi, 0, 4);
            glob1_bitlen = 0;
            glob1_hi_bitlen = 0;
            glob1_len_x2 = 0;
            glob1_hi_inv_lo = 0;
            glob1_hi_inv_hi = 0;
        }

        void calc_a_key(uint[] n1, uint[] n2, uint[] n3, uint[] n4, uint len)
        {
            uint[] n_tmp = new uint[64];
            uint n3_len, n4_len;
            int n3_bitlen;
            uint bit_mask;

            unsafe
            {
                fixed (uint* _pn3 = &n3[0])
                {
                    uint* pn3 = _pn3;

                    init_bignum(n1, 1, len);
                    n4_len = len_bignum(n4, len);
                    init_two_dw(n4, n4_len);
                    n3_bitlen = (int)bitlen_bignum(n3, n4_len);
                    n3_len = (uint)((n3_bitlen + 31) / 32);
                    bit_mask = (((uint)1) << ((n3_bitlen - 1) % 32)) >> 1;
                    pn3 += n3_len - 1;
                    n3_bitlen--;
                    mov_bignum(n1, n2, n4_len);
                    while (--n3_bitlen != -1)
                    {
                        if (bit_mask == 0)
                        {
                            bit_mask = 0x80000000;
                            pn3--;
                        }
                        calc_a_bignum(n_tmp, n1, n1, n4_len);
                        if ((*pn3 & bit_mask) != 0)
                            calc_a_bignum(n1, n_tmp, n2, n4_len);
                        else
                            mov_bignum(n1, n_tmp, n4_len);
                        bit_mask >>= 1;
                    }
                    init_bignum(n_tmp, 0, n4_len);
                    clear_tmp_vars(len);
                }
            }
        }

        unsafe void memcpy(byte* dest, byte* src, int len)
        {
            while (len-- != 0) *dest++ = *src++;
        }

        unsafe void process_predata(byte* pre, uint pre_len, byte *buf)
        {
            uint[] n2 = new uint[64];
            uint[] n3 = new uint[64];

            uint a = (pubkey.len - 1) / 8;
            while (a + 1 <= pre_len) 
            {
                init_bignum(n2, 0, 64);
                fixed( uint * pn2 = &n2[0] )
                    memcpy((byte *)pn2, pre, (int)a + 1);
                calc_a_key(n3, n2, pubkey.key2, pubkey.key1, 64);

                fixed( uint * pn3 = &n3[0] )
                    memcpy(buf, (byte *)pn3, (int)a);

                pre_len -= a + 1;
                pre += a + 1;
                buf += a;
            }
        }

        public byte[] DecryptKey(byte[] src)
        {
            init_pubkey();
            byte[] dest = new byte[256];

            unsafe
            {
                fixed (byte* pdest = &dest[0])
                fixed (byte* psrc = &src[0])
                    process_predata(psrc, len_predata(), pdest);
            }
            return dest.Take(56).ToArray();
        }
    }
}
