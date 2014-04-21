#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace OpenRA.Graphics
{
	public static class Util
	{
		static float[] channelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };

		public static void FastCreateQuad(Vertex[] vertices, float2 o, Sprite r, int palette, int nv, float2 size)
		{
			var b = new float2(o.X + size.X, o.Y);
			var c = new float2(o.X + size.X, o.Y + size.Y);
			var d = new float2(o.X, o.Y + size.Y);
			FastCreateQuad(vertices, o, b, c, d, r, palette, nv);
		}

		public static void FastCreateQuad(Vertex[] vertices, float2 a, float2 b, float2 c, float2 d, Sprite r, int palette, int nv)
		{
			var attrib = new float2(palette / (float)HardwarePalette.MaxPalettes, channelSelect[(int)r.channel]);

			vertices[nv] = new Vertex(a, r.FastMapTextureCoords(0), attrib);
			vertices[nv + 1] = new Vertex(b, r.FastMapTextureCoords(1), attrib);
			vertices[nv + 2] = new Vertex(c, r.FastMapTextureCoords(3), attrib);
			vertices[nv + 3] = new Vertex(d, r.FastMapTextureCoords(2), attrib);
		}

		static readonly int[] channelMasks = { 2, 1, 0, 3 };	// yes, our channel order is nuts.

		public static void FastCopyIntoChannel(Sprite dest, byte[] src) { FastCopyIntoChannel(dest, 0, src); }
		public static void FastCopyIntoChannel(Sprite dest, int channelOffset, byte[] src)
		{
			var data = dest.sheet.Data;
			var srcStride = dest.bounds.Width;
			var destStride = dest.sheet.Size.Width * 4;
			var destOffset = destStride * dest.bounds.Top + dest.bounds.Left * 4 + channelMasks[(int)dest.channel + channelOffset];
			var destSkip = destStride - 4 * srcStride;
			var height = dest.bounds.Height;

			var srcOffset = 0;
			for (var j = 0; j < height; j++)
			{
				for (int i = 0; i < srcStride; i++, srcOffset++)
				{
					data[destOffset] = src[srcOffset];
					destOffset += 4;
				}
				destOffset += destSkip;
			}
		}

		public static void FastCopyIntoSprite(Sprite dest, Bitmap src)
		{
			var destStride = dest.sheet.Size.Width;
			var width = dest.bounds.Width;
			var height = dest.bounds.Height;

			var srcData = src.LockBits(src.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				var c = (int*)srcData.Scan0;

				// Cast the data to an int array so we can copy the src data directly
				fixed (byte* bd = &dest.sheet.Data[0])
				{
					var data = (int*)bd;
					var x = dest.bounds.Left;
					var y = dest.bounds.Top;

					for (var j = 0; j < height; j++)
						for (var i = 0; i < width; i++)
							data[(y + j) * destStride + x + i] = *(c + (j * srcData.Stride >> 2) + i);
				}
			}

			src.UnlockBits(srcData);

		}

		public static float[] IdentityMatrix()
		{
			return Exts.MakeArray(16, j => (j % 5 == 0) ? 1.0f : 0);
		}

		public static float[] ScaleMatrix(float sx, float sy, float sz)
		{
			var mtx = IdentityMatrix();
			mtx[0] = sx;
			mtx[5] = sy;
			mtx[10] = sz;
			return mtx;
		}

		public static float[] TranslationMatrix(float x, float y, float z)
		{
			var mtx = IdentityMatrix();
			mtx[12] = x;
			mtx[13] = y;
			mtx[14] = z;
			return mtx;
		}

		public static float[] MatrixMultiply(float[] lhs, float[] rhs)
		{
			var mtx = new float[16];
			for (var i = 0; i < 4; i++)
			for (var j = 0; j < 4; j++)
			{
				mtx[4*i + j] = 0;
				for (var k = 0; k < 4; k++)
					mtx[4*i + j] += lhs[4*k + j]*rhs[4*i + k];
			}

			return mtx;
		}

		public static float[] MatrixVectorMultiply(float[] mtx, float[] vec)
		{
			var ret = new float[4];
			for (var j = 0; j < 4; j++)
			{
				ret[j] = 0;
				for (var k = 0; k < 4; k++)
					ret[j] += mtx[4*k + j]*vec[k];
			}

			return ret;
		}

		public static float[] MatrixInverse(float[] m)
		{
			var mtx = new float[16];

			mtx[0] = m[5]*m[10]*m[15] -
				m[5]*m[11]*m[14] -
				m[9]*m[6]*m[15] +
				m[9]*m[7]*m[14] +
				m[13]*m[6]*m[11] -
				m[13]*m[7]*m[10];

			mtx[4] = -m[4]*m[10]*m[15] +
				m[4]*m[11]*m[14] +
				m[8]*m[6]*m[15] -
				m[8]*m[7]*m[14] -
				m[12]*m[6]*m[11] +
				m[12]*m[7]*m[10];

			mtx[8] = m[4]*m[9]*m[15] -
				m[4]*m[11]*m[13] -
				m[8]*m[5]*m[15] +
				m[8]*m[7]*m[13] +
				m[12]*m[5]*m[11] -
				m[12]*m[7]*m[9];

			mtx[12] = -m[4]*m[9]*m[14] +
				m[4]*m[10]*m[13] +
				m[8]*m[5]*m[14] -
				m[8]*m[6]*m[13] -
				m[12]*m[5]*m[10] +
				m[12]*m[6]*m[9];

			mtx[1] = -m[1]*m[10]*m[15] +
				m[1]*m[11]*m[14] +
				m[9]*m[2]*m[15] -
				m[9]*m[3]*m[14] -
				m[13]*m[2]*m[11] +
				m[13]*m[3]*m[10];

			mtx[5] = m[0]*m[10]*m[15] -
				m[0]*m[11]*m[14] -
				m[8]*m[2]*m[15] +
				m[8]*m[3]*m[14] +
				m[12]*m[2]*m[11] -
				m[12]*m[3]*m[10];

			mtx[9] = -m[0]*m[9]*m[15] +
				m[0]*m[11]*m[13] +
				m[8]*m[1]*m[15] -
				m[8]*m[3]*m[13] -
				m[12]*m[1]*m[11] +
				m[12]*m[3]*m[9];

			mtx[13] = m[0]*m[9]*m[14] -
				m[0]*m[10]*m[13] -
				m[8]*m[1]*m[14] +
				m[8]*m[2]*m[13] +
				m[12]*m[1]*m[10] -
				m[12]*m[2]*m[9];

			mtx[2] = m[1]*m[6]*m[15] -
				m[1]*m[7]*m[14] -
				m[5]*m[2]*m[15] +
				m[5]*m[3]*m[14] +
				m[13]*m[2]*m[7] -
				m[13]*m[3]*m[6];

			mtx[6] = -m[0]*m[6]*m[15] +
				m[0]*m[7]*m[14] +
				m[4]*m[2]*m[15] -
				m[4]*m[3]*m[14] -
				m[12]*m[2]*m[7] +
				m[12]*m[3]*m[6];

			mtx[10] = m[0]*m[5]*m[15] -
				m[0]*m[7]*m[13] -
				m[4]*m[1]*m[15] +
				m[4]*m[3]*m[13] +
				m[12]*m[1]*m[7] -
				m[12]*m[3]*m[5];

			mtx[14] = -m[0]*m[5]*m[14] +
				m[0]*m[6]*m[13] +
				m[4]*m[1]*m[14] -
				m[4]*m[2]*m[13] -
				m[12]*m[1]*m[6] +
				m[12]*m[2]*m[5];

			mtx[3] = -m[1]*m[6]*m[11] +
				m[1]*m[7]*m[10] +
				m[5]*m[2]*m[11] -
				m[5]*m[3]*m[10] -
				m[9]*m[2]*m[7] +
				m[9]*m[3]*m[6];

			mtx[7] = m[0]*m[6]*m[11] -
				m[0]*m[7]*m[10] -
				m[4]*m[2]*m[11] +
				m[4]*m[3]*m[10] +
				m[8]*m[2]*m[7] -
				m[8]*m[3]*m[6];

			mtx[11] = -m[0]*m[5]*m[11] +
				m[0]*m[7]*m[9] +
				m[4]*m[1]*m[11] -
				m[4]*m[3]*m[9] -
				m[8]*m[1]*m[7] +
				m[8]*m[3]*m[5];

			mtx[15] = m[0]*m[5]*m[10] -
				m[0]*m[6]*m[9] -
				m[4]*m[1]*m[10] +
				m[4]*m[2]*m[9] +
				m[8]*m[1]*m[6] -
				m[8]*m[2]*m[5];

			var det = m[0]*mtx[0] + m[1]*mtx[4] + m[2]*mtx[8] + m[3]*mtx[12];
			if (det == 0)
				return null;

			for (var i = 0; i < 16; i++)
				mtx[i] *= 1/det;

			return mtx;
		}

		public static float[] MakeFloatMatrix(int[] imtx)
		{
			var fmtx = new float[16];
			for (var i = 0; i < 16; i++)
				fmtx[i] = imtx[i]*1f / imtx[15];
			return fmtx;
		}

		public static float[] MatrixAABBMultiply(float[] mtx, float[] bounds)
		{
			// Corner offsets
			var ix = new uint[] {0,0,0,0,3,3,3,3};
			var iy = new uint[] {1,1,4,4,1,1,4,4};
			var iz = new uint[] {2,5,2,5,2,5,2,5};

			// Vectors to opposing corner
			var ret = new float[] {float.MaxValue, float.MaxValue, float.MaxValue,
				float.MinValue, float.MinValue, float.MinValue};

			// Transform vectors and find new bounding box
			for (var i = 0; i < 8; i++)
			{
				var vec = new float[] {bounds[ix[i]], bounds[iy[i]], bounds[iz[i]], 1};
				var tvec = MatrixVectorMultiply(mtx, vec);

				ret[0] = Math.Min(ret[0], tvec[0]/tvec[3]);
				ret[1] = Math.Min(ret[1], tvec[1]/tvec[3]);
				ret[2] = Math.Min(ret[2], tvec[2]/tvec[3]);
				ret[3] = Math.Max(ret[3], tvec[0]/tvec[3]);
				ret[4] = Math.Max(ret[4], tvec[1]/tvec[3]);
				ret[5] = Math.Max(ret[5], tvec[2]/tvec[3]);
			}

			return ret;
		}
	}
}
