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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Put this on the Player actor. Manages bot harvesters to ensure they always continue harvesting as long as there are resources on the map.")]
	public class HarvesterBotModuleInfo : ConditionalTraitInfo, NotBefore<IResourceLayerInfo>
	{
		[Desc("Actor types that are considered harvesters. If harvester count drops below RefineryTypes count, a new harvester is built.",
			"Leave empty to disable harvester replacement. Currently only needed by harvester replacement system.")]
		public readonly HashSet<string> HarvesterTypes = [];

		[Desc("Actor types that are counted as refineries. Currently only needed by harvester replacement system.")]
		public readonly HashSet<string> RefineryTypes = [];

		[Desc("Interval (in ticks) between giving out orders to idle harvesters.")]
		public readonly int ScanForIdleHarvestersInterval = 50;

		[Desc("When an idle harvester cannot find resources, increase the wait to this many scan intervals.")]
		public readonly int ScanIntervalMultiplerWhenNoResources = 5;

		[Desc("Avoid enemy actors nearby when searching for a new resource patch. Should be somewhere near the max weapon range.")]
		public readonly WDist HarvesterEnemyAvoidanceRadius = WDist.FromCells(10);

		[Desc("For each enemy within the threat radius, apply the following cost multiplier for every cell that needs to be moved through.")]
		public readonly int HarvesterEnemyAvoidanceCostMultipler = 20;

		public override object Create(ActorInitializer init) { return new HarvesterBotModule(init.Self, this); }
	}

	public class HarvesterBotModule : ConditionalTrait<HarvesterBotModuleInfo>, IBotTick, INotifyActorDisposing, IWorldLoaded
	{
		sealed class HarvesterTraitWrapper
		{
			public readonly Actor Actor;
			public readonly Harvester Harvester;
			public readonly DockClientManager DockClientManager;
			public readonly Parachutable Parachutable;
			public readonly Mobile Mobile;
			public int NoResourcesCooldown { get; set; }

			public HarvesterTraitWrapper(Actor actor)
			{
				Actor = actor;
				Harvester = actor.Trait<Harvester>();
				DockClientManager = actor.Trait<DockClientManager>();
				Parachutable = actor.TraitOrDefault<Parachutable>();
				Mobile = actor.TraitOrDefault<Mobile>();
			}
		}

		readonly World world;
		readonly Player player;
		readonly Func<Actor, bool> unitCannotBeOrdered;
		readonly Dictionary<Actor, HarvesterTraitWrapper> harvesters = [];
		readonly Stack<HarvesterTraitWrapper> harvestersNeedingOrders = [];
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> refineries;
		readonly ActorIndex.OwnerAndNamesAndTrait<HarvesterInfo> harvestersIndex;
		readonly Dictionary<CPos, string> resourceTypesByCell = [];

		IResourceLayer resourceLayer;
		ResourceClaimLayer claimLayer;
		IBotRequestUnitProduction[] requestUnitProduction;
		int scanForIdleHarvestersTicks;

		public HarvesterBotModule(Actor self, HarvesterBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a.Owner != self.Owner || a.IsDead || !a.IsInWorld;
			refineries = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.RefineryTypes, player);
			harvestersIndex = new ActorIndex.OwnerAndNamesAndTrait<HarvesterInfo>(world, info.HarvesterTypes, player);
		}

		protected override void Created(Actor self)
		{
			requestUnitProduction = self.Owner.PlayerActor.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			claimLayer = world.WorldActor.TraitOrDefault<ResourceClaimLayer>();
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (resourceLayer != null)
			{
				foreach (var cell in w.Map.AllCells)
				{
					var resource = resourceLayer.GetResource(cell);
					if (resource.Type != null)
						resourceTypesByCell.Add(cell, resource.Type);
				}

				resourceLayer.CellChanged += ResourceCellChanged;
			}
		}

		void ResourceCellChanged(CPos cell, string resourceType)
		{
			if (resourceType == null)
				resourceTypesByCell.Remove(cell);
			else
				resourceTypesByCell[cell] = resourceType;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs scanning for idle harvesters on the same tick, randomize their initial scan delay.
			scanForIdleHarvestersTicks = world.LocalRandom.Next(Info.ScanForIdleHarvestersInterval);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (resourceLayer == null || resourceLayer.IsEmpty)
				return;

			// Find idle harvesters and give them orders:
			// PERF: FindNextResource is expensive, so only perform one search per tick.
			var searchedForResources = false;
			while (harvestersNeedingOrders.TryPop(out var hno) && !searchedForResources)
				searchedForResources = HarvestIfAble(bot, hno);

			if (--scanForIdleHarvestersTicks > 0)
				return;

			var toRemove = harvesters.Keys.Where(unitCannotBeOrdered).ToList();
			foreach (var a in toRemove)
				harvesters.Remove(a);

			scanForIdleHarvestersTicks = Info.ScanForIdleHarvestersInterval;

			// Find new harvesters
			var newHarvesters = world.ActorsHavingTrait<Harvester>().Where(a => !unitCannotBeOrdered(a) && !harvesters.ContainsKey(a));
			foreach (var a in newHarvesters)
				harvesters[a] = new HarvesterTraitWrapper(a);

			harvestersNeedingOrders.Clear();
			foreach (var h in harvesters)
				harvestersNeedingOrders.Push(h.Value);

			// Less harvesters than refineries - build a new harvester
			var unitBuilder = requestUnitProduction.FirstEnabledTraitOrDefault();
			if (unitBuilder != null && Info.HarvesterTypes.Count > 0)
			{
				var harvCountTooLow =
					AIUtils.CountActorByCommonName(harvestersIndex) <
					AIUtils.CountActorByCommonName(refineries);
				if (harvCountTooLow)
				{
					var harvesterType = Info.HarvesterTypes.Random(world.LocalRandom);
					if (unitBuilder.RequestedProductionCount(bot, harvesterType) == 0)
						unitBuilder.RequestUnitProduction(bot, harvesterType);
				}
			}
		}

		// Returns true if FindNextResource was called.
		bool HarvestIfAble(IBot bot, HarvesterTraitWrapper h)
		{
			if (h.Actor.IsDead || !h.Actor.IsInWorld || h.Mobile == null)
				return false;

			if (!h.Actor.IsIdle)
			{
				// Ignore this actor if FindAndDeliverResources is working fine or it is performing a different activity
				if (h.Actor.CurrentActivity is not FindAndDeliverResources act || !act.LastSearchFailed)
					return false;
			}

			if (h.NoResourcesCooldown > 1)
			{
				h.NoResourcesCooldown--;
				return false;
			}

			if (h.Parachutable != null && h.Parachutable.IsInAir)
				return false;

			// Tell the idle harvester to quit slacking:
			var newSafeResourcePatch = FindNextResource(h.Actor, h);
			AIUtils.BotDebug($"AI: Harvester {h.Actor} is idle. Ordering to {newSafeResourcePatch} in search for new resources.");
			if (newSafeResourcePatch.Type != TargetType.Invalid)
				bot.QueueOrder(new Order("Harvest", h.Actor, newSafeResourcePatch, false));
			else
				h.NoResourcesCooldown = Info.ScanIntervalMultiplerWhenNoResources;

			return true;
		}

		Target FindNextResource(Actor actor, HarvesterTraitWrapper harv)
		{
			// Prefer resource nearby to the nearest drop off point, otherwise scan from the current location.
			var scanFromActor = harv.DockClientManager.ClosestDock(null, ignoreOccupancy: true)?.Actor ?? actor;

			var targets = resourceTypesByCell
				.Where(kvp =>
					harv.Harvester.Info.Resources.Contains(kvp.Value) &&
					claimLayer.CanClaimCell(actor, kvp.Key))
				.Select(kvp => kvp.Key);

			var avoidanceCostForBin = new Dictionary<int2, int>();
			var cellRadius = Info.HarvesterEnemyAvoidanceRadius.Length / 1024;
			var minCellCost = harv.Mobile.Locomotor.Info.TerrainSpeeds.Values.Min(ti => ti.Cost);
			var cellCostMultiplier = Info.HarvesterEnemyAvoidanceCostMultipler;

			static int2 CellToBin(CPos cell, int cellRadius)
			{
				return new int2(
					cell.X / cellRadius,
					cell.Y / cellRadius);
			}

			static int CalculateAvoidanceCostForBin(World world, int2 bin, int cellRadius, Actor actor, int minCellCost, int cellCostMultipler)
			{
				// Bins are overlapping, this allows actors to apply threat in both directions when they're at the edge.
				// If the bins didn't overlap, actors along the edge of a bin only affect that bin, and not the bin next to it,
				// despite the fact the are an equal risk to both.
				var r = WDist.FromCells(cellRadius);
				var vec = new WVec(r, r, WDist.Zero);
				var originCell = new CPos(bin.X * cellRadius + cellRadius / 2, bin.Y * cellRadius + cellRadius / 2);
				var origin = world.Map.CenterOfCell(originCell);
				var threatActors = world.ActorMap.ActorsInBox(origin - vec, origin + vec)
					.Where(u => !u.IsDead && actor.Owner.RelationshipWith(u.Owner) == PlayerRelationship.Enemy);

				// For each actor in the threat radius, every cell we want to move is an extra cost than a threat-free area.
				return threatActors.Count() * minCellCost * cellCostMultipler;
			}

			var path = harv.Mobile.PathFinder.FindPathToTargetCells(
				actor, scanFromActor.Location, targets, BlockedByActor.Stationary,
				loc =>
				{
					// Avoid areas with enemies.
					var bin = CellToBin(loc, cellRadius);
					if (avoidanceCostForBin.TryGetValue(bin, out var avoidanceCost))
						return avoidanceCost;

					// PERF: Calculate a "bin" for a threat area.
					// This allows future custom cost checks to reuse the result for that area,
					// rather than calculating it fresh for every cell explored for the path.
					avoidanceCost = CalculateAvoidanceCostForBin(world, bin, cellRadius, actor, minCellCost, cellCostMultiplier);
					avoidanceCostForBin.Add(bin, avoidanceCost);
					return avoidanceCost;
				});

			if (path.Count == 0)
				return Target.Invalid;

			return Target.FromCell(world, path[0]);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			refineries.Dispose();
			harvestersIndex.Dispose();

			if (resourceLayer != null)
				resourceLayer.CellChanged -= ResourceCellChanged;
		}
	}
}
