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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum ElevatedBridgePlaceholderOrientation { X, Y }

	[Desc("Placeholder to make static elevated bridges work.",
		"Define individual trait instances for each elevated bridge footprint in the map.")]
	[TraitLocation(SystemActors.World)]
	public class ElevatedBridgePlaceholderInfo : TraitInfo<ElevatedBridgePlaceholder>, Requires<ElevatedBridgeLayerInfo>, ILobbyCustomRulesIgnore
	{
		[FieldLoader.Require]
		[Desc("Location of the bridge")]
		public readonly CPos Location = CPos.Zero;

		[FieldLoader.Require]
		[Desc("Orientation of the bridge.")]
		public readonly ElevatedBridgePlaceholderOrientation Orientation;

		[FieldLoader.Require]
		[Desc("Length of the bridge")]
		public readonly int Length = 0;

		[FieldLoader.Require]
		[Desc("Height of the bridge in map height steps.")]
		public readonly byte Height = 0;

		[Desc("Terrain type of the bridge.")]
		public readonly string TerrainType = "Road";

		public IEnumerable<CPos> BridgeCells()
		{
			var dimensions = Orientation == ElevatedBridgePlaceholderOrientation.X ?
				new CVec(Length + 1, 3) : new CVec(3, Length + 1);

			for (var y = 0; y < dimensions.Y; y++)
				for (var x = 0; x < dimensions.X; x++)
					yield return Location + new CVec(x, y);
		}

		public IEnumerable<CPos> EndCells()
		{
			if (Orientation == ElevatedBridgePlaceholderOrientation.X)
			{
				for (var y = 0; y < 3; y++)
				{
					yield return Location + new CVec(0, y);
					yield return Location + new CVec(Length, y);
				}
			}
			else
			{
				for (var x = 0; x < 3; x++)
				{
					yield return Location + new CVec(x, 0);
					yield return Location + new CVec(x, Length);
				}
			}
		}
	}

	public class ElevatedBridgePlaceholder { }
}
