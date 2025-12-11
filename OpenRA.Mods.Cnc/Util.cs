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
using System.Numerics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc
{
	public static class Util
	{
		// TD and RA used a nonlinear mapping between artwork frames and unit facings for units with 32 facings.
		// This table defines the exclusive maximum facing for the i'th sprite frame.
		// i.e. sprite frame 1 is used for facings 20-55, sprite frame 2 for 56-87, and so on.
		// Sprite frame 0 is used for facings smaller than 20 or larger than 999.
		static readonly int[] SpriteRanges =
		[
			20, 56, 88, 132, 156, 184, 212, 240,
			268, 296, 324, 352, 384, 416, 452, 488,
			532, 568, 604, 644, 668, 696, 724, 752,
			780, 808, 836, 864, 896, 928, 964, 1000
		];

		// The actual facing associated with each sprite frame.
		static readonly WAngle[] SpriteFacings =
		[
			WAngle.Zero, new(40), new(74), new(112), new(146), new(172), new(200), new(228),
			new(256), new(284), new(312), new(340), new(370), new(402), new(436), new(472),
			new(512), new(552), new(588), new(626), new(658), new(684), new(712), new(740),
			new(768), new(796), new(824), new(852), new(882), new(914), new(948), new(984)
		];

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

		public static Matrix4x4 IdentityMatrix()
		{
			return Matrix4x4.Identity;
		}

		public static Matrix4x4 ScaleMatrix(float sx, float sy, float sz)
		{
			return Matrix4x4.CreateScale(sx, sy, sz);
		}

		public static Matrix4x4 TranslationMatrix(float x, float y, float z)
		{
			return Matrix4x4.CreateTranslation(x, y, z);
		}

		/// <summary>
		/// Warning: Arguments are backwards.
		/// </summary>
		public static Matrix4x4 MatrixMultiply(Matrix4x4 rhs, Matrix4x4 lhs)
		{
			return lhs * rhs;
		}

		public static Vector4 MatrixVectorMultiply(Matrix4x4 mtx, Vector4 vec)
		{
			return Vector4.Transform(vec, mtx);
		}

		public static Matrix4x4? MatrixInverse(Matrix4x4 m)
		{
			if (Matrix4x4.Invert(m, out var r))
				return r;

			return null;
		}

		public static Matrix4x4 MakeFloatMatrix(Int32Matrix4x4 imtx)
		{
			var multipler = 1f / imtx.M44;
			return new Matrix4x4(
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
				1f);
		}

		public static AABB MatrixAABBMultiply(Matrix4x4 mtx, AABB bounds)
		{
			// Vectors to opposing corner.
			var minX = float.MaxValue;
			var minY = float.MaxValue;
			var minZ = float.MaxValue;
			var maxX = float.MinValue;
			var maxY = float.MinValue;
			var maxZ = float.MinValue;

			// Transform vectors and find new bounding box.
			for (var i = 0; i < 8; i++)
			{
				var vec = bounds.Corner(i);
				var tvec = MatrixVectorMultiply(mtx, vec);

				minX = Math.Min(minX, tvec[0] / tvec[3]);
				minY = Math.Min(minY, tvec[1] / tvec[3]);
				minZ = Math.Min(minZ, tvec[2] / tvec[3]);
				maxX = Math.Max(maxX, tvec[0] / tvec[3]);
				maxY = Math.Max(maxY, tvec[1] / tvec[3]);
				maxZ = Math.Max(maxZ, tvec[2] / tvec[3]);
			}

			return new AABB(minX, minY, minZ, maxX, maxY, maxZ);
		}
	}
}
