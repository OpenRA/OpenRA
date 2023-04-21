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
	public class LAT
	{
		[Flags]
		public enum Adjacency : byte
		{
			None = 0x0,

			/// <summary> Depending on the map grid type: Rectangular - Bottom; Isometric - BottomLeft. </summary>
			PlusY = 0x1,

			/// <summary> Depending on the map grid type: Rectangular - Left; Isometric - TopLeft. </summary>
			MinusX = 0x2,

			/// <summary> Depending on the map grid type: Rectangular - Top; Isometric - TopRight. </summary>
			MinusY = 0x4,

			/// <summary> Depending on the map grid type: Rectangular - Right; Isometric - BottomRight. </summary>
			PlusX = 0x8,
		}
    }
}
