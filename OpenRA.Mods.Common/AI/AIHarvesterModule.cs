#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	[Desc("Manages AI harvesters to ensure they always continue harvesting as long as there's resources on the map.")]
	public class AIHarvesterModuleInfo : IAIModuleInfo
	{
		[Desc("Name for identification purposes.")]
		public readonly string Name = "default-harvester-module";

		[Desc("Avoid enemy actors nearby when searching for a new resource patch. Should be somewhere near the max weapon range.")]
		public readonly WDist HarvesterEnemyAvoidanceRadius = WDist.FromCells(8);

		public object Create(ActorInitializer init) { return new AIHarvesterModule(init.Self, this); }
	}

	public class AIHarvesterModule : IAIModule
	{
		public readonly AIHarvesterModuleInfo Info;
		readonly World world;
		HackyAI ai;
		IPathFinder pathfinder;
		DomainIndex domainIndex;
		ResourceLayer resLayer;
		ResourceClaimLayer claimLayer;

		public AIHarvesterModule(Actor self, AIHarvesterModuleInfo info)
		{
			Info = info;
			world = self.World;
		}

		string IAIModule.Name { get { return Info.Name; } }

		void IAIModule.Activate(HackyAI ai)
		{
			this.ai = ai;
			pathfinder = world.WorldActor.Trait<IPathFinder>();
			domainIndex = world.WorldActor.Trait<DomainIndex>();
			resLayer = world.WorldActor.TraitOrDefault<ResourceLayer>();
			claimLayer = world.WorldActor.TraitOrDefault<ResourceClaimLayer>();
		}

		void IAIModule.Tick()
		{
			if (resLayer == null || resLayer.IsResourceLayerEmpty)
				return;

			// Find idle harvesters and give them orders:
			foreach (var harvester in ai.Harvesters)
			{
				var harv = harvester.Trait<Harvester>();
				if (!harv.IsEmpty)
					continue;

				if (!harvester.IsIdle)
				{
					var act = harvester.CurrentActivity;
					if (!harv.LastSearchFailed || act.NextActivity == null || act.NextActivity.GetType() != typeof(FindResources))
						continue;
				}

				var para = harvester.TraitOrDefault<Parachutable>();
				if (para != null && para.IsInAir)
					continue;

				// Tell the idle harvester to quit slacking:
				var newSafeResourcePatch = FindNextResource(harvester, harv);
				HackyAI.BotDebug("AI: Harvester {0} is idle. Ordering to {1} in search for new resources.".F(harvester, newSafeResourcePatch));
				ai.QueueOrder(new Order("Harvest", harvester, Target.FromCell(world, newSafeResourcePatch), false));
			}
		}

		CPos FindNextResource(Actor actor, Harvester harv)
		{
			var locomotorInfo = actor.Info.TraitInfo<MobileInfo>().LocomotorInfo;

			Func<CPos, bool> isValidResource = cell =>
				domainIndex.IsPassable(actor.Location, cell, locomotorInfo) &&
				harv.CanHarvestCell(actor, cell) &&
				claimLayer.CanClaimCell(actor, cell);

			var path = pathfinder.FindPath(
				PathSearch.Search(world, locomotorInfo, actor, true, isValidResource)
					.WithCustomCost(loc => world.FindActorsInCircle(world.Map.CenterOfCell(loc), Info.HarvesterEnemyAvoidanceRadius)
						.Where(u => !u.IsDead && actor.Owner.Stances[u.Owner] == Stance.Enemy)
						.Sum(u => Math.Max(WDist.Zero.Length, Info.HarvesterEnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(loc) - u.CenterPosition).Length)))
					.FromPoint(actor.Location));

			if (path.Count == 0)
				return CPos.Zero;

			return path[0];
		}
	}
}
