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
		public static int ClassicQuantizeFacing(int facing, int numFrames, bool useClassicFacingFudge)
		{
			if (!useClassicFacingFudge || numFrames != 32)
				return OpenRA.Mods.Common.Util.QuantizeFacing(facing, numFrames);

			// TD and RA divided the facing artwork into 3 frames from (north|south) to (north|south)-(east|west)
			// and then 5 frames from (north|south)-(east|west) to (east|west)
			var quadrant = ((facing + 31) & 0xFF) / 64;
			if (quadrant == 0 || quadrant == 2)
			{
				var frame = OpenRA.Mods.Common.Util.QuantizeFacing(facing, 24);
				if (frame > 18)
					return frame + 6;
				if (frame > 4)
					return frame + 3;
				return frame;
			}
			else
			{
				var frame = OpenRA.Mods.Common.Util.QuantizeFacing(facing, 40);
				return frame < 20 ? frame - 3 : frame - 8;
			}
		}
	}
}
