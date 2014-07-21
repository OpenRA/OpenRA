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
	public class ResourcerWarheadInfo : BaseWarhead, IWarheadInfo
	{
		[Desc("Size of the area. The resources are seeded within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Can this damage resource patches?")]
		public readonly bool DestroyResources = false;

		[Desc("Will this splatter resources and which?")]
		public readonly string AddsResourceType = null;

		//TODO: Allow maximum resource splatter to be defined. (Per tile, and in total).

		public ResourcerWarheadInfo() : base() { }

		public new void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);

			var resLayer = DestroyResources || !string.IsNullOrEmpty(AddsResourceType) ? world.WorldActor.Trait<ResourceLayer>() : null;

			if (Size[0] > 0)
			{
				var allCells = world.Map.FindTilesInCircle(targetTile, Size[0]).ToList();

				// Destroy all resources in range, not just the outer shell:
				if (DestroyResources)
					foreach (var cell in allCells)
						resLayer.Destroy(cell);

				// Splatter resources:
				if (!string.IsNullOrEmpty(AddsResourceType))
				{
					var resourceType = world.WorldActor.TraitsImplementing<ResourceType>()
						.FirstOrDefault(t => t.Info.Name == AddsResourceType);

					if (resourceType == null)
						Log.Write("debug", "Warhead defines an invalid resource type '{0}'".F(AddsResourceType));
					else
					{
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

			if (DestroyResources)
				world.WorldActor.Trait<ResourceLayer>().Destroy(targetTile);
		}
	}
}
