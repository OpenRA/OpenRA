#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Cnc
{
	public static class Util
	{
		// TD and RA used a nonlinear mapping between artwork frames and unit facings for units with 32 facings.
		// This table defines the exclusive maximum facing for the i'th sprite frame.
		// i.e. sprite frame 1 is used for facings 5-13, sprite frame 2 for 14-21, and so on.
		// Sprite frame 0 is used for facings smaller than 5 or larger than 249.
		static readonly int[] SpriteRanges =
		{
			5, 14, 22, 33, 39, 46, 53, 60,
			67, 74, 81, 88, 96, 104, 113, 122,
			133, 142, 151, 161, 167, 174, 181, 188,
			195, 202, 209, 216, 224, 232, 241, 250
		};

		// The actual facing associated with each sprite frame.
		static readonly int[] SpriteFacings =
		{
			0, 10, 18, 28, 36, 43, 50, 57,
			64, 71, 78, 85, 92, 100, 109, 118,
			128, 138, 147, 156, 164, 171, 178, 185,
			192, 199, 206, 213, 220, 228, 237, 246
		};

		/// <summary>
		/// Calculate the frame index (between 0..numFrames) that
		/// should be used for the given facing value, accounting
		/// for the non-linear facing mapping for sprites with 32 directions.
		/// </summary>
		public static int ClassicIndexFacing(int facing, int numFrames)
		{
			if (numFrames == 32)
			{
				for (var i = 0; i < SpriteRanges.Length; i++)
					if (facing < SpriteRanges[i])
						return i;

				return 0;
			}

			return Common.Util.IndexFacing(facing, numFrames);
		}

		/// <summary>
		/// Rounds the given facing value to the nearest quantized step,
		/// accounting for the non-linear facing mapping for sprites with 32 directions.
		/// </summary>
		public static int ClassicQuantizeFacing(int facing, int steps)
		{
			if (steps == 32)
				return SpriteFacings[ClassicIndexFacing(facing, steps)];

			return Common.Util.QuantizeFacing(facing, steps);
		}
	}
}
