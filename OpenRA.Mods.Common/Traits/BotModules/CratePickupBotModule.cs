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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Put this on the Player actor. Manages crate collection.")]
	public class CratePickupBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that should not start hunting for crates.")]
		public readonly HashSet<string> ExcludedUnitTypes = new HashSet<string>();

		[Desc("Only these actor types should start hunting for crates.")]
		public readonly HashSet<string> IncludedUnitTypes = new HashSet<string>();

		[Desc("Interval (in ticks) between giving out orders to idle units.")]
		public readonly int ScanForCratesInterval = 50;

		[Desc("Only move this far away from base. Disabled if set to zero.")]
		public readonly int MaxProximityRadius = 0;

		[Desc("Avoid enemy actors nearby when searching for crates. Should be somewhere near the max weapon range.")]
		public readonly WDist EnemyAvoidanceRadius = WDist.FromCells(8);

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for crates?")]
		public readonly bool CheckTargetsForVisibility = true;

		public override object Create(ActorInitializer init) { return new CratePickupBotModule(init.Self, this); }
	}

	public class CratePickupBotModule : ConditionalTrait<CratePickupBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly int maxProximity;

		CrateSpawner crateSpawner;

		IPathFinder pathfinder;
		DomainIndex domainIndex;
		int scanForCratesTicks;

		List<Actor> alreadyPursuitCrates = new List<Actor>();

		public CratePickupBotModule(Actor self, CratePickupBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			maxProximity = Info.MaxProximityRadius > 0 ? info.MaxProximityRadius : world.Map.Grid.MaximumTileSearchRange;
		}

		protected override void Created(Actor self)
		{
			crateSpawner = self.Owner.World.WorldActor.TraitOrDefault<CrateSpawner>();
		}

		protected override void TraitEnabled(Actor self)
		{
			pathfinder = world.WorldActor.Trait<IPathFinder>();
			domainIndex = world.WorldActor.Trait<DomainIndex>();
			scanForCratesTicks = Info.ScanForCratesInterval;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (crateSpawner == null || !crateSpawner.IsTraitEnabled() || !crateSpawner.Enabled)
				return;

			if (--scanForCratesTicks > 0)
				return;

			scanForCratesTicks = Info.ScanForCratesInterval;

			var crates = world.ActorsHavingTrait<Crate>().ToList();
			if (!crates.Any())
				return;

			if (Info.CheckTargetsForVisibility)
				crates.RemoveAll(c => !c.CanBeViewedByPlayer(player));

			var idleUnits = world.ActorsHavingTrait<Mobile>().Where(a => a.Owner == player && a.IsIdle
				&& (Info.IncludedUnitTypes.Contains(a.Info.Name) || (!Info.IncludedUnitTypes.Any() && !Info.ExcludedUnitTypes.Contains(a.Info.Name)))).ToList();

			if (!idleUnits.Any())
				return;

			foreach (var crate in crates)
			{
				if (alreadyPursuitCrates.Contains(crate))
					continue;

				if (!crate.IsAtGroundLevel())
					continue;

				var crateCollector = idleUnits.ClosestTo(crate);
				if (crateCollector == null)
					continue;

				if ((crate.Location - crateCollector.Location).Length > maxProximity)
					continue;

				idleUnits.Remove(crateCollector);

				var target = PathToNextCrate(crateCollector, crate);
				if (target.Type == TargetType.Invalid)
					continue;

				AIUtils.BotDebug("AI: Ordering unit {0} to {1} for crate pick up.".F(crateCollector, target));
				bot.QueueOrder(new Order("Move", crateCollector, target, true));
				alreadyPursuitCrates.Add(crate);
			}
		}

		Target PathToNextCrate(Actor collector, Actor crate)
		{
			var locomotor = collector.Trait<Mobile>().Locomotor;

			if (!domainIndex.IsPassable(collector.Location, crate.Location, locomotor.Info))
				return Target.Invalid;

			var path = pathfinder.FindPath(
				PathSearch.FromPoint(world, locomotor, collector, collector.Location, crate.Location, BlockedByActor.Stationary)
					.WithCustomCost(loc => world.FindActorsInCircle(world.Map.CenterOfCell(loc), Info.EnemyAvoidanceRadius)
						.Where(u => !u.IsDead && collector.Owner.Stances[u.Owner] == Stance.Enemy)
						.Sum(u => Math.Max(WDist.Zero.Length, Info.EnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(loc) - u.CenterPosition).Length)))
					.FromPoint(collector.Location));

			if (path.Count == 0)
				return Target.Invalid;

			// Don't use the actor to avoid invalid targets when the crate disappears midway.
			return Target.FromCell(world, crate.Location);
		}
	}
}
