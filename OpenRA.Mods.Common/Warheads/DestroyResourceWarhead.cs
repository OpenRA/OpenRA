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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Destroys resources in a circle.")]
	public class DestroyResourceWarhead : Warhead
	{
		[Desc("Size of the area. The resources are removed within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Amount of resources to be removed. If negative or zero, all resources within the area will be removed.")]
		public readonly int ResourceAmount = 0;

		[Desc("Resource types to remove with this warhead.", "If empty, all resource types will be removed.")]
		public readonly HashSet<string> ResourceTypes = new HashSet<string>();

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var firedBy = args.SourceActor;
			var pos = target.CenterPosition;
			var world = firedBy.World;
			var dat = world.Map.DistanceAboveTerrain(pos);
			if (dat > AirThreshold)
				return;

			var targetTile = world.Map.CellContaining(pos);
			var resourceLayer = world.WorldActor.Trait<IResourceLayer>();

			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			var removeAllTypes = ResourceTypes.Count == 0;

			foreach (var cell in allCells)
			{
				var cellContents = resourceLayer.GetResource(cell);

				if (removeAllTypes || ResourceTypes.Contains(cellContents.Type))
				{
					if (ResourceAmount <= 0)
						resourceLayer.ClearResources(cell);
					else
						resourceLayer.RemoveResource(cellContents.Type, cell, ResourceAmount);
				}
			}
		}
	}
}
