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

using OpenRA.Primitives;

namespace OpenRA
{
	public class WorldViewportSizes : IGlobalModData
	{
		public readonly int2 CloseWindowHeights = new int2(480, 600);
		public readonly int2 MediumWindowHeights = new int2(600, 900);
		public readonly int2 FarWindowHeights = new int2(900, 1300);

		public readonly float DefaultScale = 1.0f;
		public readonly float MaxZoomScale = 2.0f;
		public readonly int MaxZoomWindowHeight = 240;
		public readonly bool AllowNativeZoom = true;

		public readonly Size MinEffectiveResolution = new Size(1024, 720);

		public int2 GetSizeRange(WorldViewport distance)
		{
			return distance == WorldViewport.Close ? CloseWindowHeights
				: distance == WorldViewport.Medium ? MediumWindowHeights
				: FarWindowHeights;
		}
	}
}
