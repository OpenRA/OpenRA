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
		static readonly int[] SpriteFacings =
		{
			5, 14, 22, 33, 39, 46, 53, 60,
			67, 74, 81, 88, 96, 104, 113, 122,
			133, 142, 151, 161, 167, 174, 181, 188,
			195, 202, 209, 216, 224, 232, 241, 250
		};

		public static int ClassicQuantizeFacing(int facing, int numFrames, bool useClassicFacingFudge)
		{
			if (useClassicFacingFudge && numFrames == 32)
			{
				for (var i = 0; i < SpriteFacings.Length; i++)
					if (facing < SpriteFacings[i])
						return i;

				return 0;
			}

			return Common.Util.QuantizeFacing(facing, numFrames);
		}
	}
}
