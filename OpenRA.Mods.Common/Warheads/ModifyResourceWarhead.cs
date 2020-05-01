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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class ModifyResourceWarhead : Warhead
	{
		[Desc("Size of the area. The resources are seeded within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Removes resources in effect area. If AddsResourceType is not null, the removal of old resources is triggered first.")]
		public readonly bool RemoveResources = false;

		[Desc("Will this splatter resources and which?")]
		public readonly string AddsResourceType = null;

		public override void DoImpact(Target target, WarheadArgs args)
		{
			if (!RemoveResources && string.IsNullOrEmpty(AddsResourceType))
				return;

			var firedBy = args.SourceActor;
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(target.CenterPosition);
			var resLayer = world.WorldActor.Trait<ResourceLayer>();

			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			// Destroy all resources in the selected tiles
			// TODO: Allow maximum resource removal to be defined (per tile, and in total)
			if (RemoveResources)
				foreach (var cell in allCells)
					resLayer.Destroy(cell);

			if (string.IsNullOrEmpty(AddsResourceType))
				return;

			var resourceType = world.WorldActor.TraitsImplementing<ResourceType>()
				.FirstOrDefault(t => t.Info.Type == AddsResourceType);

			if (resourceType == null)
				Log.Write("debug", "Warhead defines an invalid resource type '{0}'".F(AddsResourceType));
			else
			{
				// TODO: Allow maximum resource splatter to be defined. (Per tile, and in total).
				foreach (var cell in allCells)
				{
					if (!resLayer.CanSpawnResourceAt(resourceType, cell))
						continue;

					var splash = world.SharedRandom.Next(1, resourceType.Info.MaxDensity - resLayer.GetResourceDensity(cell));
					resLayer.AddResource(resourceType, cell, splash);
				}
			}
		}
	}
}
