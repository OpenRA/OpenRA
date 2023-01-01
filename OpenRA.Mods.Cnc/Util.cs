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
	}
}
