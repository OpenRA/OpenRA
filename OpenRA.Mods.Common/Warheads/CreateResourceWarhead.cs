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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class CreateResourceWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Size of the area. The resources are seeded within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Will this splatter resources and which?")]
		[FieldLoader.Require]
		public readonly string AddsResourceType = null;

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			var world = rules.Actors["world"];
			var resourceType = world.TraitInfos<ResourceTypeInfo>()
				.FirstOrDefault(t => t.Type == AddsResourceType);

			if (resourceType == null)
				throw new YamlException("CreateResourceWarhead defines an invalid resource type '{0}'".F(AddsResourceType));
		}

		// TODO: Allow maximum resource splatter to be defined. (Per tile, and in total).
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

			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			var resourceType = world.WorldActor.TraitsImplementing<ResourceType>()
				.First(t => t.Info.Type == AddsResourceType);

			var resLayer = world.WorldActor.Trait<ResourceLayer>();
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
