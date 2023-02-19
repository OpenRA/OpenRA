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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor (not a building!) to define a new shared build queue.",
		"Will only work together with the Production: trait on the actor that actually does the production.",
		"You will also want to add PrimaryBuildings: to let the user choose where new units should exit.",
		"The production speed depends on the number of production buildings and units queued at the same time.")]
	[TraitLocation(SystemActors.Player)]
	public class ClassicParallelProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("If you build more actors of the same type,", "the same queue will get its build time lowered for every actor produced there.")]
		public readonly bool SpeedUp = false;

		[Desc("Every time another production building of the same queue is",
			"constructed, the build times of all actors in the queue",
			"modified by a percentage of the original time.")]
		public readonly int[] BuildingCountBuildTimeMultipliers = { 100, 86, 75, 67, 60, 55, 50 };

		[Desc("Build time modifier multiplied by the number of parallel production for producing different actors at the same time.")]
		public readonly int[] ParallelPenaltyBuildTimeMultipliers = { 100, 116, 133, 150, 166, 183, 200, 216, 233, 250 };

		public override object Create(ActorInitializer init) { return new ClassicParallelProductionQueue(init, this); }
	}

	public class ClassicParallelProductionQueue : ProductionQueue
	{
		static readonly ActorInfo[] NoItems = Array.Empty<ActorInfo>();

		readonly Actor self;
		readonly ClassicParallelProductionQueueInfo info;

		int penalty;

		public ClassicParallelProductionQueue(ActorInitializer init, ClassicParallelProductionQueueInfo info)
			: base(init, info)
		{
			self = init.Self;
			this.info = info;
		}

		protected override void Tick(Actor self)
		{
			// PERF: Avoid LINQ.
			Enabled = false;
			var isActive = false;
			foreach (var x in self.World.ActorsWithTrait<Production>())
			{
				if (x.Trait.IsTraitDisabled)
					continue;

				if (x.Actor.Owner != self.Owner || !x.Trait.Info.Produces.Contains(Info.Type))
					continue;

				Enabled |= IsValidFaction;
				isActive |= !x.Trait.IsTraitPaused;
			}

			if (!Enabled)
				ClearQueue();

			TickInner(self, !isActive);
		}

		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			CancelUnbuildableItems();

			if (allProductionPaused)
				return;

			var item = Queue.FirstOrDefault(i => !i.Paused);
			if (item == null)
				return;

			var parallelBuilds = Queue.FindAll(i => !i.Paused && !i.Done)
				.GroupBy(i => i.Item)
				.ToList()
				.Count - 1;

			if (parallelBuilds > 0 && !developerMode.FastBuild)
			{
				penalty -= 100;
				if (penalty < 0)
					penalty = info.ParallelPenaltyBuildTimeMultipliers[Math.Min(parallelBuilds, info.ParallelPenaltyBuildTimeMultipliers.Length - 1)];
				else
					return;
			}

			var before = item.RemainingTime;
			item.Tick(playerResources);

			if (item.RemainingTime == before)
				return;

			// As we have progressed this actor type, we will move all queued items of this actor to the end.
			foreach (var other in Queue.FindAll(a => a.Item == item.Item))
			{
				Queue.Remove(other);
				Queue.Add(other);
			}
		}

		public override IEnumerable<ActorInfo> AllItems()
		{
			return Enabled ? base.AllItems() : NoItems;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return Enabled ? base.BuildableItems() : NoItems;
		}

		public override bool IsProducing(ProductionItem item)
		{
			return Queue.Contains(item);
		}

		public override TraitPair<Production> MostLikelyProducer()
		{
			var productionActors = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled && x.Trait.Info.Produces.Contains(Info.Type))
				.OrderByDescending(x => x.Actor.IsPrimaryBuilding())
				.ThenByDescending(x => x.Actor.ActorID)
				.ToList();

			var unpaused = productionActors.FirstOrDefault(a => !a.Trait.IsTraitPaused);
			return unpaused.Trait != null ? unpaused : productionActors.FirstOrDefault();
		}

		protected override bool BuildUnit(ActorInfo unit)
		{
			// Find a production structure to build this actor
			var bi = unit.TraitInfo<BuildableInfo>();

			// Some units may request a specific production type, which is ignored if the AllTech cheat is enabled
			var type = developerMode.AllTech ? Info.Type : (bi.BuildAtProductionType ?? Info.Type);

			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled
					&& x.Trait.Info.Produces.Contains(type))
				.OrderByDescending(x => x.Actor.IsPrimaryBuilding())
				.ThenByDescending(x => x.Actor.ActorID);

			if (!producers.Any())
			{
				CancelProduction(unit.Name, 1);
				return false;
			}

			foreach (var p in producers)
			{
				if (p.Trait.IsTraitPaused)
					continue;

				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new FactionInit(BuildableInfo.GetInitialFaction(unit, p.Trait.Faction))
				};

				var item = Queue.First(i => i.Done && i.Item == unit.Name);
				if (p.Trait.Produce(p.Actor, unit, type, inits, item.TotalCost))
				{
					EndProduction(item);
					return true;
				}
			}

			return false;
		}

		protected override void BeginProduction(ProductionItem item, bool hasPriority)
		{
			// Ignore `hasPriority` as it's not relevant in parallel production context.
			base.BeginProduction(item, false);
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			if (developerMode.FastBuild)
				return 0;

			var time = base.GetBuildTime(unit, bi);

			if (info.SpeedUp)
			{
				var type = bi.BuildAtProductionType ?? info.Type;

				var selfsameProductionsCount = self.World.ActorsWithTrait<Production>()
					.Count(p => !p.Trait.IsTraitDisabled && !p.Trait.IsTraitPaused && p.Actor.Owner == self.Owner && p.Trait.Info.Produces.Contains(type));

				var speedModifier = selfsameProductionsCount.Clamp(1, info.BuildingCountBuildTimeMultipliers.Length) - 1;
				time = time * info.BuildingCountBuildTimeMultipliers[speedModifier] / 100;
			}

			return time;
		}

		public override int RemainingTimeActual(ProductionItem item)
		{
			var parallelBuilds = Queue.FindAll(i => !i.Paused && !i.Done)
				.GroupBy(i => i.Item)
				.ToList()
				.Count;
			return item.RemainingTimeActual * parallelBuilds * info.ParallelPenaltyBuildTimeMultipliers[Math.Min(parallelBuilds - 1, info.ParallelPenaltyBuildTimeMultipliers.Length - 1)] / 100;
		}
	}
}
