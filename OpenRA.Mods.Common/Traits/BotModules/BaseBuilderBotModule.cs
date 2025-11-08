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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI base construction.")]
	public class BaseBuilderBotModuleInfo : ConditionalTraitInfo, NotBefore<ResourceMapBotModuleInfo>, NotBefore<IResourceLayerInfo>
	{
		[Desc("Tells the AI what building types are considered construction yards.")]
		public readonly FrozenSet<string> ConstructionYardTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered refineries.")]
		public readonly FrozenSet<string> RefineryTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered power plants.")]
		public readonly FrozenSet<string> PowerTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered production facilities.")]
		public readonly FrozenSet<string> ProductionTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered tech buildings.")]
		public readonly FrozenSet<string> TechTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered naval production facilities.")]
		public readonly FrozenSet<string> NavalProductionTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered silos (resource storage).")]
		public readonly FrozenSet<string> SiloTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered defenses.")]
		public readonly FrozenSet<string> DefenseTypes = FrozenSet<string>.Empty;

		[Desc("Production queues AI uses for buildings.")]
		public readonly FrozenSet<string> BuildingQueues = new HashSet<string> { "Building" }.ToFrozenSet();

		[Desc("Production queues AI uses for defenses.")]
		public readonly FrozenSet<string> DefenseQueues = new HashSet<string> { "Defense" }.ToFrozenSet();

		[Desc("Minimum distance in cells from center of the base when checking for building placement.")]
		public readonly int MinBaseRadius = 2;

		[Desc("Radius in cells around the center of the base to expand.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Minimum excess power the AI should try to maintain.")]
		public readonly int MinimumExcessPower = 0;

		[Desc("The targeted excess power the AI tries to maintain cannot rise above this.")]
		public readonly int MaximumExcessPower = 0;

		[Desc("Increase maintained excess power by this amount for every ExcessPowerIncreaseThreshold of base buildings.")]
		public readonly int ExcessPowerIncrement = 0;

		[Desc("Increase maintained excess power by ExcessPowerIncrement for every N base buildings.")]
		public readonly int ExcessPowerIncreaseThreshold = 1;

		[Desc("Number of refineries to build before building any production building.")]
		public readonly int InititalMinimumRefineryCount = 1;

		[Desc("Number of refineries to build additionally after building any production building.")]
		public readonly int AdditionalMinimumRefineryCount = 1;

		[Desc("Additional delay (in ticks) between structure production checks when there is no active production.",
			"StructureProductionRandomBonusDelay is added to this.")]
		public readonly int StructureProductionInactiveDelay = 125;

		[Desc("Additional delay (in ticks) added between structure production checks when actively building things.",
			"Note: this should be at least as large as the typical order latency to avoid duplicated build choices.")]
		public readonly int StructureProductionActiveDelay = 25;

		[Desc("A random delay (in ticks) of up to this is added to active/inactive production delays.")]
		public readonly int StructureProductionRandomBonusDelay = 10;

		[Desc("Delay (in ticks) until retrying to build structure after the last 3 consecutive attempts failed.")]
		public readonly int StructureProductionResumeDelay = 1500;

		[Desc("After how many failed attempts to place a structure should AI give up and wait",
			"for StructureProductionResumeDelay before retrying.")]
		public readonly int MaximumFailedPlacementAttempts = 3;

		[Desc("How many randomly chosen cells with resources to check when deciding refinery placement.")]
		public readonly int MaxResourceCellsToCheck = 3;

		[Desc("Delay (in ticks) until rechecking for new BaseProviders.")]
		public readonly int CheckForNewBasesDelay = 1500;

		[Desc("Chance that the AI will place the defenses in the direction of the closest enemy building.")]
		public readonly int PlaceDefenseTowardsEnemyChance = 100;

		[Desc("Minimum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MinimumDefenseRadius = 5;

		[Desc("Maximum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MaximumDefenseRadius = 20;

		[Desc("Try to build another production building if there is too much cash.")]
		public readonly int NewProductionCashThreshold = 5000;

		[Desc("Chance to build another production building if there is too much cash.")]
		public readonly int NewProductionChance = 50;

		[Desc("Radius in cells around a factory scanned for rally points by the AI.")]
		public readonly int RallyPointScanRadius = 8;

		[Desc("Radius in cells around each building with ProvideBuildableArea",
			"to check for a 3x3 area of water where naval structures can be built.",
			"Should match maximum adjacency of naval structures.")]
		public readonly int CheckForWaterRadius = 8;

		[Desc("Terrain types which are considered water for base building purposes.")]
		public readonly FrozenSet<string> WaterTerrainTypes = new HashSet<string> { "Water" }.ToFrozenSet();

		[Desc("What buildings to the AI should build.", "What integer percentage of the total base must be this type of building.")]
		public readonly FrozenDictionary<string, int> BuildingFractions = null;

		[Desc("What buildings should the AI have a maximum limit to build.")]
		public readonly FrozenDictionary<string, int> BuildingLimits = null;

		[Desc("When should the AI start building specific buildings.")]
		public readonly FrozenDictionary<string, int> BuildingDelays = null;

		[Desc("Only queue construction of a new structure when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 500;

		[Desc("Delay (in ticks) between reassigning rally points.")]
		public readonly int AssignRallyPointsInterval = 100;

		[Desc("Delay (in ticks) for finding a good resource to place a refinery next to.")]
		public readonly int CheckBestResourceLocationInterval = 151;

		[Desc("Interval (in ticks) between checking whether to sell a redundant refinery. Set to -1 to disable.")]
		public readonly int SellRefineryInterval = 5000;

		[Desc("Distance (in cells) for refineries finding redundant refineries.")]
		public readonly int SellRefineryTooCloseCellDistance = 6;

		[Desc("Maximum distance (in cells) from resources before refineries are eligible to be sold.")]
		public readonly int SellRefineryNoResourceDistance = 12;

		[Desc("Maximum refinery count per area. Area size is defined in " + nameof(ResourceMapBotModule) + ".")]
		public readonly int MaxRefineryPerIndice = 2;

		[Desc($"AI will move mcv when those numbers of refinery <= productions + tech - {nameof(ExpansionTolerate)}.")]
		public readonly ImmutableArray<int> ExpansionTolerate = [0, 1];

		[Desc($"AI will move the only mcv when those numbers of refinery <= productions + tech - {nameof(ForceExpansionTolerate)}.")]
		public readonly ImmutableArray<int> ForceExpansionTolerate = [2, 3];

		public override object Create(ActorInitializer init) { return new BaseBuilderBotModule(init.Self, this); }
	}

	public class BaseBuilderBotModule : ConditionalTrait<BaseBuilderBotModuleInfo>, IGameSaveTraitData,
		IBotTick, IBotPositionsUpdated, IBotRespondToAttack, IBotRequestPauseUnitProduction, IBotSuggestRefineryProduction, INotifyActorDisposing
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = ConstructionYardBuildings.Actors
				.RandomOrDefault(world.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public CPos DefenseCenter { get; private set; }

		// Actor, ActorCount.
		public Dictionary<string, int> BuildingsBeingProduced = [];
		public IBotBaseExpansion[] BaseExpansionModules;
		public ResourceMapBotModule ResourceMapModule;

		readonly World world;
		readonly Player player;
		PowerManager playerPower;
		PlayerResources playerResources;
		IResourceLayer resourceLayer;
		IPathFinder pathFinder;
		IBotPositionsUpdated[] positionsUpdatedModules;
		CPos initialBaseCenter;
		public CPos? ResourceConyardCenter;
		public Dictionary<Actor, (CPos ConyardLoc, CPos ResourceLoc)> RequestedRefineries = [];

		readonly Stack<TraitPair<RallyPoint>> rallyPoints = [];
		int assignRallyPointsTicks;
		int checkBestResourceLocationTicks;
		int sellRefineryTick;
		bool firstTick = true;

		readonly BaseBuilderQueueManager[] builders;
		int currentBuilderIndex = 0;

		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> RefineryBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> powerBuildings;
		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> ConstructionYardBuildings;
		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> ProductionBuildings;

		public BaseBuilderBotModule(Actor self, BaseBuilderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			builders = new BaseBuilderQueueManager[info.BuildingQueues.Count + info.DefenseQueues.Count];
			RefineryBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.RefineryTypes, player);
			powerBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.PowerTypes, player);
			ConstructionYardBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			ProductionBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ProductionTypes, player);
		}

		protected override void Created(Actor self)
		{
			playerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			resourceLayer = self.World.WorldActor.TraitOrDefault<IResourceLayer>();
			pathFinder = self.World.WorldActor.TraitOrDefault<IPathFinder>();
			positionsUpdatedModules = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			BaseExpansionModules = self.Owner.PlayerActor.TraitsImplementing<IBotBaseExpansion>().ToArray();

			var i = 0;

			foreach (var building in Info.BuildingQueues)
				builders[i++] = new BaseBuilderQueueManager(this, building, player, playerPower, playerResources, resourceLayer);

			foreach (var defense in Info.DefenseQueues)
				builders[i++] = new BaseBuilderQueueManager(this, defense, player, playerPower, playerResources, resourceLayer);
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRallyPointsTicks = world.LocalRandom.Next(0, Info.AssignRallyPointsInterval);
			checkBestResourceLocationTicks = world.LocalRandom.Next(0, Info.CheckBestResourceLocationInterval);
			sellRefineryTick = Info.SellRefineryInterval < 0 ? 0 : world.LocalRandom.Next(0, Info.SellRefineryInterval);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation)
		{
			DefenseCenter = newLocation;
		}

		bool IBotRequestPauseUnitProduction.PauseUnitProduction => !IsTraitDisabled && !HasMinimalRefineryCount();

		void IBotTick.BotTick(IBot bot)
		{
			if (firstTick)
			{
				ResourceMapModule = bot.Player.PlayerActor.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());
				firstTick = false;
			}

			if (--assignRallyPointsTicks <= 0)
			{
				assignRallyPointsTicks = Math.Max(2, Info.AssignRallyPointsInterval);
				foreach (var rp in world.ActorsWithTrait<RallyPoint>().Where(rp => rp.Actor.Owner == player))
					rallyPoints.Push(rp);
			}
			else
			{
				// PERF: Spread out rally point assignments updates across multiple ticks.
				var updateCount = Exts.IntegerDivisionRoundingAwayFromZero(rallyPoints.Count, assignRallyPointsTicks);
				for (var i = 0; i < updateCount; i++)
				{
					var rp = rallyPoints.Pop();
					if (rp.Actor.Owner == player && !rp.Actor.Disposed)
						SetRallyPoint(bot, rp);
				}
			}

			if (--checkBestResourceLocationTicks <= 0 && resourceLayer != null)
			{
				checkBestResourceLocationTicks = Info.CheckBestResourceLocationInterval;

				// Clear outdated refinery requests that add too many refinery to a map indice
				if (ResourceMapModule != null)
				{
					foreach (var mcv in RequestedRefineries.Keys.ToList())
					{
						if (ResourceMapModule.FindClosestIndiceFromCPos(
							RequestedRefineries[mcv].ResourceLoc).PlayerRefineryCount >= Info.MaxRefineryPerIndice)
							RequestedRefineries.Remove(mcv);
					}
				}

				Actor bestconyard = null;
				var best = int.MinValue;

				foreach (var conyard in ConstructionYardBuildings.Actors)
				{
					if (conyard.IsDead)
						continue;

					if (!world.Map.FindTilesInAnnulus(conyard.Location, Info.MinBaseRadius, Info.MaxBaseRadius)
						.Any(c => ResourceMapModule != null
						? ResourceMapModule.Info.ValuableResourceTypes.Contains(resourceLayer.GetResource(c).Type)
						: resourceLayer.GetResource(c).Type != null))
						continue;

					var refs = world.FindActorsInCircle(conyard.CenterPosition, WDist.FromCells(Info.MaxBaseRadius))
							.Count(a => a.Owner == player && Info.RefineryTypes.Contains(a.Info.Name));

					var suitable = -world.FindActorsInCircle(conyard.CenterPosition, WDist.FromCells(Info.MaxBaseRadius))
							.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy) - refs;

					if (suitable > best)
					{
						best = suitable;
						bestconyard = conyard;
					}
				}

				ResourceConyardCenter = bestconyard?.Location;
			}

			BuildingsBeingProduced.Clear();

			// PERF: We tick only one type of valid queue at a time
			// if AI gets enough cash, it can fill all of its queues with enough ticks
			var findQueue = false;
			var queuesByCategory = AIUtils.FindQueuesByCategory(player);
			for (int i = 0, builderIndex = currentBuilderIndex; i < builders.Length; i++)
			{
				if (++builderIndex >= builders.Length)
					builderIndex = 0;

				--builders[builderIndex].WaitTicks;

				var queues = queuesByCategory[builders[builderIndex].Category].ToArray();
				if (queues.Length != 0)
				{
					if (!findQueue)
					{
						currentBuilderIndex = builderIndex;
						findQueue = true;
					}

					// Refresh "BuildingsBeingProduced" only when AI can produce
					if (playerResources.GetCashAndResources() >= Info.ProductionMinCashRequirement)
					{
						foreach (var queue in queues)
						{
							var producing = queue.AllQueued().FirstOrDefault();
							if (producing == null)
								continue;

							if (BuildingsBeingProduced.TryGetValue(producing.Item, out var number))
								BuildingsBeingProduced[producing.Item] = number + 1;
							else
								BuildingsBeingProduced.Add(producing.Item, 1);
						}
					}
				}
			}

			builders[currentBuilderIndex].Tick(bot, queuesByCategory);

			if (Info.SellRefineryInterval >= 0 && --sellRefineryTick <= 0)
			{
				SellUselessRefinery(bot);
				sellRefineryTick = Info.SellRefineryInterval;
			}
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (e.Attacker.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Enemy)
				return;

			if (!e.Attacker.Info.HasTraitInfo<ITargetableInfo>())
				return;

			// Protect buildings
			if (self.Info.HasTraitInfo<BuildingInfo>())
				foreach (var n in positionsUpdatedModules)
					n.UpdatedDefenseCenter(e.Attacker.Location);
		}

		void SetRallyPoint(IBot bot, TraitPair<RallyPoint> rp)
		{
			var needsRallyPoint = rp.Trait.Path.Count == 0;

			if (!needsRallyPoint)
			{
				var locomotors = LocomotorsForProducibles(rp.Actor);
				needsRallyPoint = !IsRallyPointValid(rp.Actor.Location, rp.Trait.Path[0], locomotors, rp.Actor.Info.TraitInfoOrDefault<BuildingInfo>());
			}

			if (needsRallyPoint)
			{
				bot.QueueOrder(new Order("SetRallyPoint", rp.Actor, Target.FromCell(world, ChooseRallyLocationNear(rp.Actor)), false)
				{
					SuppressVisualFeedback = true
				});
			}
		}

		// Won't work for shipyards...
		CPos ChooseRallyLocationNear(Actor producer)
		{
			var locomotors = LocomotorsForProducibles(producer);
			var possibleRallyPoints = world.Map.FindTilesInCircle(producer.Location, Info.RallyPointScanRadius)
				.Where(c => IsRallyPointValid(producer.Location, c, locomotors, producer.Info.TraitInfoOrDefault<BuildingInfo>()))
				.ToList();

			if (possibleRallyPoints.Count == 0)
			{
				AIUtils.BotDebug("{0} has no possible rallypoint near {1}", producer.Owner, producer.Location);
				return producer.Location;
			}

			return possibleRallyPoints.Random(world.LocalRandom);
		}

		Locomotor[] LocomotorsForProducibles(Actor producer)
		{
			// Per-actor production
			var productions = producer.TraitsImplementing<Production>();

			// Player-wide production
			if (!productions.Any())
				productions = producer.World.ActorsWithTrait<Production>().Where(x => x.Actor.Owner != producer.Owner).Select(x => x.Trait);

			var produces = productions.SelectMany(p => p.Info.Produces).ToHashSet();
			var locomotors = Array.Empty<Locomotor>();
			if (produces.Count > 0)
			{
				// Per-actor production
				var productionQueues = producer.TraitsImplementing<ProductionQueue>();

				// Player-wide production
				if (!productionQueues.Any())
					productionQueues = producer.Owner.PlayerActor.TraitsImplementing<ProductionQueue>();

				productionQueues = productionQueues.Where(pq => produces.Contains(pq.Info.Type));

				var producibles = productionQueues.SelectMany(pq => pq.BuildableItems());
				var locomotorNames = producibles
					.Select(p => p.TraitInfoOrDefault<MobileInfo>())
					.Where(mi => mi != null)
					.Select(mi => mi.Locomotor)
					.ToHashSet();

				if (locomotorNames.Count != 0)
					locomotors = world.WorldActor.TraitsImplementing<Locomotor>()
						.Where(l => locomotorNames.Contains(l.Info.Name))
						.ToArray();
			}

			return locomotors;
		}

		bool IsRallyPointValid(CPos producerLocation, CPos rallyPointLocation, Locomotor[] locomotors, BuildingInfo buildingInfo)
		{
			return
				(pathFinder == null ||
					locomotors.All(l => pathFinder.PathMightExistForLocomotorBlockedByImmovable(l, producerLocation, rallyPointLocation)))
				&&
				(buildingInfo == null ||
					world.IsCellBuildable(rallyPointLocation, null, buildingInfo));
		}

		// Require at least one refinery, unless we can't build it.
		public bool HasAdequateRefineryCount() =>
			Info.RefineryTypes.Count == 0 ||
			AIUtils.CountActorByCommonName(RefineryBuildings) >= OptimalRefineryCount() ||
			AIUtils.CountActorByCommonName(powerBuildings) == 0 ||
			AIUtils.CountActorByCommonName(ConstructionYardBuildings) == 0;

		int OptimalRefineryCount() =>
			AIUtils.CountActorByCommonName(ProductionBuildings) > 0
			? Info.InititalMinimumRefineryCount + Info.AdditionalMinimumRefineryCount
			: Info.InititalMinimumRefineryCount;
		bool HasMinimalRefineryCount() =>
			AIUtils.CountActorByCommonName(RefineryBuildings) >= Info.InititalMinimumRefineryCount;

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return
			[
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new("DefenseCenter", FieldSaver.FormatValue(DefenseCenter))
			];
		}

		void SellUselessRefinery(IBot bot)
		{
			// Sell one refinery each time. Perserve at least one refinery
			var refineries = world.ActorsHavingTrait<Refinery>().Where(a => a.Owner == player).ToArray();

			if (refineries.Length <= Info.InititalMinimumRefineryCount + Info.AdditionalMinimumRefineryCount)
				return;

			for (var i = 0; i < refineries.Length; i++)
			{
				for (var j = i + 1; j < refineries.Length; j++)
				{
					if ((refineries[i].Location - refineries[j].Location).LengthSquared <= Info.SellRefineryTooCloseCellDistance * Info.SellRefineryTooCloseCellDistance)
					{
						bot.QueueOrder(new Order("Sell", refineries[i], Target.FromActor(refineries[i]), false));
						return;
					}
				}

				if (ResourceMapModule != null &&
					!world.Map.FindTilesInAnnulus(refineries[i].Location, 0, Info.SellRefineryNoResourceDistance)
					.Any(c => ResourceMapModule.Info.ValuableResourceTypes.Contains(resourceLayer.GetResource(c).Type))
					&& !world.FindActorsInCircle(refineries[i].CenterPosition, WDist.FromCells(Info.SellRefineryNoResourceDistance))
					.Any(a => ResourceMapModule.Info.ResourceCreatorTypes.Contains(a.Info.Name)))
				{
					bot.QueueOrder(new Order("Sell", refineries[i], Target.FromActor(refineries[i]), false));
					return;
				}
			}
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var initialBaseCenterNode = data.NodeWithKeyOrDefault("InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);

			var defenseCenterNode = data.NodeWithKeyOrDefault("DefenseCenter");
			if (defenseCenterNode != null)
				DefenseCenter = FieldLoader.GetValue<CPos>("DefenseCenter", defenseCenterNode.Value.Value);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			RefineryBuildings.Dispose();
			powerBuildings.Dispose();
			ConstructionYardBuildings.Dispose();
			ProductionBuildings.Dispose();
		}

		void IBotSuggestRefineryProduction.RequestLocation(CPos refineryLocation, CPos conyardLocation, Actor expandActor)
		{
			if (ResourceMapModule == null || ResourceMapModule.FindClosestIndiceFromCPos(refineryLocation).PlayerRefineryCount < Info.MaxRefineryPerIndice)
				RequestedRefineries[expandActor] = (conyardLocation, refineryLocation);
		}
	}
}
