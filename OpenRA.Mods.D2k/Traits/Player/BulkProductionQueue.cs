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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Attach this to the player actor (not a building!) to define a new shared build queue.",
		"Will only work together with the Production: trait on the actor that actually does the production.",
		"You will also want to add PrimaryBuildings: to let the user choose where new units should exit.")]
	public class BulkProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("If you build more actors of the same type,", "the same queue will get its build time lowered for every actor produced there.")]
		public readonly bool SpeedUp = false;

		[Desc("Every time another production building of the same queue is",
			"constructed, the build times of all actors in the queue",
			"decreased by a percentage of the original time.")]
		public readonly int[] BuildTimeSpeedReduction = { 100, 85, 75, 65, 60, 55, 50 };

		[Desc("Minimum amount of units that can be produced at one time")]
		public readonly int MinimumOrders = 3;

		[Desc("Time it takes to process the order.")]
		public readonly int BuildTime = 1000;

		public override object Create(ActorInitializer init) { return new BulkProductionQueue(init, this); }
	}

	public class BulkProductionQueue : ProductionQueue
	{
		static readonly ActorInfo[] NoItems = { };

		readonly Actor self;
		readonly BulkProductionQueueInfo info;

		protected readonly List<ActorInfo> ReadyForDelivery = new List<ActorInfo>();

		public BulkProductionQueue(ActorInitializer init, BulkProductionQueueInfo info)
			: base(init, init.Self, info)
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

			if (Queue.Count < info.MinimumOrders)
				return;

			foreach (var item in Queue.Where(i => !i.Paused))
				item.Tick(playerResources);
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
			return Queue.Contains(item) && Queue.Count >= info.MinimumOrders;
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
			ReadyForDelivery.Add(unit);

			if (ReadyForDelivery.Count >= info.MinimumOrders)
				return BuildUnits(ReadyForDelivery);

			return true;
		}

		protected bool BuildUnits(IEnumerable<ActorInfo> units)
		{
			var items = units.ToList();
			var unit = items.First();
			var buildableInfo = unit.TraitInfo<BuildableInfo>();

			// Some units may request a specific production type, which is ignored if the AllTech cheat is enabled
			var type = developerMode.AllTech ? Info.Type : (buildableInfo.BuildAtProductionType ?? Info.Type);

			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled
					&& x.Trait.Info.Produces.Contains(type))
					.OrderByDescending(x => x.Actor.IsPrimaryBuilding())
					.ThenByDescending(x => x.Actor.ActorID);

			if (!producers.Any())
			{
				foreach (var item in items)
					CancelProduction(item.Name, 1);

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

				if (p.Trait.Produce(p.Actor, items, type, inits))
				{
					foreach (var item in items.ToList())
					{
						var productionItem = Queue.FirstOrDefault(i => i.Done && i.Item == item.Name);
						if (productionItem != null)
							EndProduction(productionItem);

						ReadyForDelivery.Remove(item);
					}

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

			var time = info.BuildTime;

			if (info.SpeedUp)
			{
				var type = bi.BuildAtProductionType ?? info.Type;

				var selfsameProductionsCount = self.World.ActorsWithTrait<Production>()
					.Count(p => !p.Trait.IsTraitDisabled && !p.Trait.IsTraitPaused && p.Actor.Owner == self.Owner && p.Trait.Info.Produces.Contains(type));

				var speedModifier = selfsameProductionsCount.Clamp(1, info.BuildTimeSpeedReduction.Length) - 1;
				time = (time * info.BuildTimeSpeedReduction[speedModifier]) / 100;
			}

			return time;
		}

		public override int RemainingTimeActual(ProductionItem item)
		{
			return Queue.Max(q => q.RemainingTimeActual);
		}
	}
}
