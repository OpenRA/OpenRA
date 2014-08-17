#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class DestroyResourceWarhead : Warhead
	{
		[Desc("Size of the area. The resources are seeded within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		// TODO: Allow maximum resource removal to be defined. (Per tile, and in total).

		public override void DoImpact(Target target, Actor firedBy, float firepowerModifier)
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
