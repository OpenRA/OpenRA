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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Put this on the Player actor. Manages bot harvesters to ensure they always continue harvesting as long as there are resources on the map.")]
	public class HarvesterBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are considered harvesters. If harvester count drops below RefineryTypes count, a new harvester is built.",
			"Leave empty to disable harvester replacement. Currently only needed by harvester replacement system.")]
		public readonly HashSet<string> HarvesterTypes = new HashSet<string>();

		[Desc("Actor types that are counted as refineries. Currently only needed by harvester replacement system.")]
		public readonly HashSet<string> RefineryTypes = new HashSet<string>();

		[Desc("Interval (in ticks) between giving out orders to idle harvesters.")]
		public readonly int ScanForIdleHarvestersInterval = 50;

		[Desc("Avoid enemy actors nearby when searching for a new resource patch. Should be somewhere near the max weapon range.")]
		public readonly WDist HarvesterEnemyAvoidanceRadius = WDist.FromCells(8);

		public override object Create(ActorInitializer init) { return new HarvesterBotModule(init.Self, this); }
	}

	public class HarvesterBotModule : ConditionalTrait<HarvesterBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		IBotRequestUnitProduction[] requestUnitProduction;
		IPathFinder pathfinder;
		DomainIndex domainIndex;
		ResourceLayer resLayer;
		ResourceClaimLayer claimLayer;
		List<Actor> harvesters = new List<Actor>();
		int scanForIdleHarvestersTicks;

		public HarvesterBotModule(Actor self, HarvesterBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a.Owner != self.Owner || a.IsDead || !a.IsInWorld;
		}

		protected override void TraitEnabled(Actor self)
		{
			requestUnitProduction = player.PlayerActor.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			pathfinder = world.WorldActor.Trait<IPathFinder>();
			domainIndex = world.WorldActor.Trait<DomainIndex>();
			resLayer = world.WorldActor.TraitOrDefault<ResourceLayer>();
			claimLayer = world.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			scanForIdleHarvestersTicks = Info.ScanForIdleHarvestersInterval;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (resLayer == null || resLayer.IsResourceLayerEmpty)
				return;

			if (--scanForIdleHarvestersTicks > 0)
				return;

			harvesters.RemoveAll(unitCannotBeOrdered);
			scanForIdleHarvestersTicks = Info.ScanForIdleHarvestersInterval;

			// Find new harvesters
			// TODO: Look for a more performance-friendly way to update this list
			var newHarvesters = world.ActorsHavingTrait<Harvester>().Where(a => a.Owner == player && !harvesters.Contains(a));
			foreach (var a in newHarvesters)
				harvesters.Add(a);

			// Find idle harvesters and give them orders:
			foreach (var harvester in harvesters)
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
				AIUtils.BotDebug("AI: Harvester {0} is idle. Ordering to {1} in search for new resources.".F(harvester, newSafeResourcePatch));
				bot.QueueOrder(new Order("Harvest", harvester, Target.FromCell(world, newSafeResourcePatch), false));
			}

			// Less harvesters than refineries - build a new harvester
			var unitBuilder = requestUnitProduction.FirstOrDefault(Exts.IsTraitEnabled);
			if (unitBuilder != null && Info.HarvesterTypes.Any())
			{
				var harvInfo = AIUtils.GetInfoByCommonName(Info.HarvesterTypes, player);
				var harvCountTooLow = AIUtils.CountActorByCommonName(Info.HarvesterTypes, player) < AIUtils.CountBuildingByCommonName(Info.RefineryTypes, player);
				if (harvCountTooLow && unitBuilder.RequestedProductionCount(bot, harvInfo.Name) == 0)
					unitBuilder.RequestUnitProduction(bot, harvInfo.Name);
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
