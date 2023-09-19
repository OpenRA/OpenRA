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

namespace OpenRA.Mods.Cnc
{
	public static class Util
	{
		// TD and RA used a nonlinear mapping between artwork frames and unit facings for units with 32 facings.
		// This table defines the exclusive maximum facing for the i'th sprite frame.
		// i.e. sprite frame 1 is used for facings 20-55, sprite frame 2 for 56-87, and so on.
		// Sprite frame 0 is used for facings smaller than 20 or larger than 999.
		static readonly int[] SpriteRanges =
		{
			20, 56, 88, 132, 156, 184, 212, 240,
			268, 296, 324, 352, 384, 416, 452, 488,
			532, 568, 604, 644, 668, 696, 724, 752,
			780, 808, 836, 864, 896, 928, 964, 1000
		};

		// The actual facing associated with each sprite frame.
		static readonly WAngle[] SpriteFacings =
		{
			WAngle.Zero, new WAngle(40), new WAngle(74), new WAngle(112), new WAngle(146), new WAngle(172), new WAngle(200), new WAngle(228),
			new WAngle(256), new WAngle(284), new WAngle(312), new WAngle(340), new WAngle(370), new WAngle(402), new WAngle(436), new WAngle(472),
			new WAngle(512), new WAngle(552), new WAngle(588), new WAngle(626), new WAngle(658), new WAngle(684), new WAngle(712), new WAngle(740),
			new WAngle(768), new WAngle(796), new WAngle(824), new WAngle(852), new WAngle(882), new WAngle(914), new WAngle(948), new WAngle(984)
		};

		/// <summary>
		/// Calculate the frame index (between 0..numFrames) that
		/// should be used for the given facing value, accounting
		/// for the non-linear facing mapping for sprites with 32 directions.
		/// </summary>
		public static int ClassicIndexFacing(WAngle facing, int numFrames)
		{
			if (numFrames == 32)
			{
				var angle = facing.Angle;
				for (var i = 0; i < SpriteRanges.Length; i++)
					if (angle < SpriteRanges[i])
						return i;

				return 0;
			}

			return Common.Util.IndexFacing(facing, numFrames);
		}

		/// <summary>
		/// Rounds the given facing value to the nearest quantized step,
		/// accounting for the non-linear facing mapping for sprites with 32 directions.
		/// </summary>
		public static WAngle ClassicQuantizeFacing(WAngle facing, int steps)
		{
			if (steps == 32)
				return SpriteFacings[ClassicIndexFacing(facing, steps)];

			return Common.Util.QuantizeFacing(facing, steps);
		}

		public static float[] IdentityMatrix()
		{
			return new float[]
			{
				1, 0, 0, 0,
				0, 1, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1,
			};
		}

		public static float[] ScaleMatrix(float sx, float sy, float sz)
		{
			return new float[]
			{
				sx, 0, 0, 0,
				0, sy, 0, 0,
				0, 0, sz, 0,
				0, 0, 0, 1,
			};
		}

		public static float[] TranslationMatrix(float x, float y, float z)
		{
			return new float[]
			{
				1, 0, 0, 0,
				0, 1, 0, 0,
				0, 0, 1, 0,
				x, y, z, 1,
			};
		}

		public static float[] MatrixMultiply(float[] lhs, float[] rhs)
		{
			var mtx = new float[16];
			for (var i = 0; i < 4; i++)
				for (var j = 0; j < 4; j++)
				{
					mtx[4 * i + j] = 0;
					for (var k = 0; k < 4; k++)
						mtx[4 * i + j] += lhs[4 * k + j] * rhs[4 * i + k];
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
					ret[j] += mtx[4 * k + j] * vec[k];
			}

			return ret;
		}

		public static float[] MatrixInverse(float[] m)
		{
			var mtx = new float[16];

			mtx[0] = m[5] * m[10] * m[15] -
				m[5] * m[11] * m[14] -
				m[9] * m[6] * m[15] +
				m[9] * m[7] * m[14] +
				m[13] * m[6] * m[11] -
				m[13] * m[7] * m[10];

			mtx[4] = -m[4] * m[10] * m[15] +
				m[4] * m[11] * m[14] +
				m[8] * m[6] * m[15] -
				m[8] * m[7] * m[14] -
				m[12] * m[6] * m[11] +
				m[12] * m[7] * m[10];

			mtx[8] = m[4] * m[9] * m[15] -
				m[4] * m[11] * m[13] -
				m[8] * m[5] * m[15] +
				m[8] * m[7] * m[13] +
				m[12] * m[5] * m[11] -
				m[12] * m[7] * m[9];

			mtx[12] = -m[4] * m[9] * m[14] +
				m[4] * m[10] * m[13] +
				m[8] * m[5] * m[14] -
				m[8] * m[6] * m[13] -
				m[12] * m[5] * m[10] +
				m[12] * m[6] * m[9];

			mtx[1] = -m[1] * m[10] * m[15] +
				m[1] * m[11] * m[14] +
				m[9] * m[2] * m[15] -
				m[9] * m[3] * m[14] -
				m[13] * m[2] * m[11] +
				m[13] * m[3] * m[10];

			mtx[5] = m[0] * m[10] * m[15] -
				m[0] * m[11] * m[14] -
				m[8] * m[2] * m[15] +
				m[8] * m[3] * m[14] +
				m[12] * m[2] * m[11] -
				m[12] * m[3] * m[10];

			mtx[9] = -m[0] * m[9] * m[15] +
				m[0] * m[11] * m[13] +
				m[8] * m[1] * m[15] -
				m[8] * m[3] * m[13] -
				m[12] * m[1] * m[11] +
				m[12] * m[3] * m[9];

			mtx[13] = m[0] * m[9] * m[14] -
				m[0] * m[10] * m[13] -
				m[8] * m[1] * m[14] +
				m[8] * m[2] * m[13] +
				m[12] * m[1] * m[10] -
				m[12] * m[2] * m[9];

			mtx[2] = m[1] * m[6] * m[15] -
				m[1] * m[7] * m[14] -
				m[5] * m[2] * m[15] +
				m[5] * m[3] * m[14] +
				m[13] * m[2] * m[7] -
				m[13] * m[3] * m[6];

			mtx[6] = -m[0] * m[6] * m[15] +
				m[0] * m[7] * m[14] +
				m[4] * m[2] * m[15] -
				m[4] * m[3] * m[14] -
				m[12] * m[2] * m[7] +
				m[12] * m[3] * m[6];

			mtx[10] = m[0] * m[5] * m[15] -
				m[0] * m[7] * m[13] -
				m[4] * m[1] * m[15] +
				m[4] * m[3] * m[13] +
				m[12] * m[1] * m[7] -
				m[12] * m[3] * m[5];

			mtx[14] = -m[0] * m[5] * m[14] +
				m[0] * m[6] * m[13] +
				m[4] * m[1] * m[14] -
				m[4] * m[2] * m[13] -
				m[12] * m[1] * m[6] +
				m[12] * m[2] * m[5];

			mtx[3] = -m[1] * m[6] * m[11] +
				m[1] * m[7] * m[10] +
				m[5] * m[2] * m[11] -
				m[5] * m[3] * m[10] -
				m[9] * m[2] * m[7] +
				m[9] * m[3] * m[6];

			mtx[7] = m[0] * m[6] * m[11] -
				m[0] * m[7] * m[10] -
				m[4] * m[2] * m[11] +
				m[4] * m[3] * m[10] +
				m[8] * m[2] * m[7] -
				m[8] * m[3] * m[6];

			mtx[11] = -m[0] * m[5] * m[11] +
				m[0] * m[7] * m[9] +
				m[4] * m[1] * m[11] -
				m[4] * m[3] * m[9] -
				m[8] * m[1] * m[7] +
				m[8] * m[3] * m[5];

			mtx[15] = m[0] * m[5] * m[10] -
				m[0] * m[6] * m[9] -
				m[4] * m[1] * m[10] +
				m[4] * m[2] * m[9] +
				m[8] * m[1] * m[6] -
				m[8] * m[2] * m[5];

			var det = m[0] * mtx[0] + m[1] * mtx[4] + m[2] * mtx[8] + m[3] * mtx[12];
			if (det == 0)
				return null;

			for (var i = 0; i < 16; i++)
				mtx[i] *= 1 / det;

			return mtx;
		}

		public static float[] MakeFloatMatrix(Int32Matrix4x4 imtx)
		{
			var multipler = 1f / imtx.M44;
			return new[]
			{
				imtx.M11 * multipler,
				imtx.M12 * multipler,
				imtx.M13 * multipler,
				imtx.M14 * multipler,

				imtx.M21 * multipler,
				imtx.M22 * multipler,
				imtx.M23 * multipler,
				imtx.M24 * multipler,

				imtx.M31 * multipler,
				imtx.M32 * multipler,
				imtx.M33 * multipler,
				imtx.M34 * multipler,

				imtx.M41 * multipler,
				imtx.M42 * multipler,
				imtx.M43 * multipler,
				imtx.M44 * multipler,
			};
		}

		public static float[] MatrixAABBMultiply(float[] mtx, float[] bounds)
		{
			// Corner offsets.
			var ix = new uint[] { 0, 0, 0, 0, 3, 3, 3, 3 };
			var iy = new uint[] { 1, 1, 4, 4, 1, 1, 4, 4 };
			var iz = new uint[] { 2, 5, 2, 5, 2, 5, 2, 5 };

			// Vectors to opposing corner.
			var ret = new[]
			{
				float.MaxValue, float.MaxValue, float.MaxValue,
				float.MinValue, float.MinValue, float.MinValue
			};

			// Transform vectors and find new bounding box.
			for (var i = 0; i < 8; i++)
			{
				var vec = new[] { bounds[ix[i]], bounds[iy[i]], bounds[iz[i]], 1 };
				var tvec = MatrixVectorMultiply(mtx, vec);

				ret[0] = Math.Min(ret[0], tvec[0] / tvec[3]);
				ret[1] = Math.Min(ret[1], tvec[1] / tvec[3]);
				ret[2] = Math.Min(ret[2], tvec[2] / tvec[3]);
				ret[3] = Math.Max(ret[3], tvec[0] / tvec[3]);
				ret[4] = Math.Max(ret[4], tvec[1] / tvec[3]);
				ret[5] = Math.Max(ret[5], tvec[2] / tvec[3]);
			}

			return ret;
		}
	}
}
