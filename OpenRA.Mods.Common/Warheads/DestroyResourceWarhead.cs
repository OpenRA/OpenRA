#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class DestroyResourceWarhead : Warhead
	{
		[Desc("Size of the area. The resources are seeded within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		// TODO: Allow maximum resource removal to be defined. (Per tile, and in total).
		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(target.CenterPosition);
			var resLayer = world.WorldActor.Trait<ResourceLayer>();

			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			// Destroy all resources in the selected tiles
			foreach (var cell in allCells)
				resLayer.Destroy(cell);
		}
	}
}
