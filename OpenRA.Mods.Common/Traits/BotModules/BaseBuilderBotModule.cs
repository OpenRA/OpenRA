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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI base construction.")]
	public class BaseBuilderBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Tells the AI what building types are considered construction yards.")]
		public readonly HashSet<string> ConstructionYardTypes = new();

		[Desc("Tells the AI what building types are considered vehicle production facilities.")]
		public readonly HashSet<string> VehiclesFactoryTypes = new();

		[Desc("Tells the AI what building types are considered refineries.")]
		public readonly HashSet<string> RefineryTypes = new();

		[Desc("Tells the AI what building types are considered power plants.")]
		public readonly HashSet<string> PowerTypes = new();

		[Desc("Tells the AI what building types are considered infantry production facilities.")]
		public readonly HashSet<string> BarracksTypes = new();

		[Desc("Tells the AI what building types are considered production facilities.")]
		public readonly HashSet<string> ProductionTypes = new();

		[Desc("Tells the AI what building types are considered naval production facilities.")]
		public readonly HashSet<string> NavalProductionTypes = new();

		[Desc("Tells the AI what building types are considered silos (resource storage).")]
		public readonly HashSet<string> SiloTypes = new();

		[Desc("Tells the AI what building types are considered defenses.")]
		public readonly HashSet<string> DefenseTypes = new();

		[Desc("Production queues AI uses for buildings.")]
		public readonly HashSet<string> BuildingQueues = new() { "Building" };

		[Desc("Production queues AI uses for defenses.")]
		public readonly HashSet<string> DefenseQueues = new() { "Defense" };

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

		[Desc("Number of refineries to build before building a barracks.")]
		public readonly int InititalMinimumRefineryCount = 1;

		[Desc("Number of refineries to build additionally after building a barracks.")]
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

		[Desc("Radius in cells around a factory scanned for rally points by the AI.")]
		public readonly int RallyPointScanRadius = 8;

		[Desc("Radius in cells around each building with ProvideBuildableArea",
			"to check for a 3x3 area of water where naval structures can be built.",
			"Should match maximum adjacency of naval structures.")]
		public readonly int CheckForWaterRadius = 8;

		[Desc("Terrain types which are considered water for base building purposes.")]
		public readonly HashSet<string> WaterTerrainTypes = new() { "Water" };

		[Desc("What buildings to the AI should build.", "What integer percentage of the total base must be this type of building.")]
		public readonly Dictionary<string, int> BuildingFractions = null;

		[Desc("What buildings should the AI have a maximum limit to build.")]
		public readonly Dictionary<string, int> BuildingLimits = null;

		[Desc("When should the AI start building specific buildings.")]
		public readonly Dictionary<string, int> BuildingDelays = null;

		[Desc("Only queue construction of a new structure when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 500;

		[Desc("Delay (in ticks) between reassigning rally points.")]
		public readonly int AssignRallyPointsInterval = 100;

		public override object Create(ActorInitializer init) { return new BaseBuilderBotModule(init.Self, this); }
	}

	public class BaseBuilderBotModule : ConditionalTrait<BaseBuilderBotModuleInfo>, IGameSaveTraitData,
		IBotTick, IBotPositionsUpdated, IBotRespondToAttack, IBotRequestPauseUnitProduction, INotifyActorDisposing
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = constructionYardBuildings.Actors
				.RandomOrDefault(world.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public CPos DefenseCenter { get; private set; }

		// Actor, ActorCount.
		public Dictionary<string, int> BuildingsBeingProduced = new();

		readonly World world;
		readonly Player player;
		PowerManager playerPower;
		PlayerResources playerResources;
		IResourceLayer resourceLayer;
		IPathFinder pathFinder;
		IBotPositionsUpdated[] positionsUpdatedModules;
		CPos initialBaseCenter;

		readonly Stack<TraitPair<RallyPoint>> rallyPoints = new();
		int assignRallyPointsTicks;

		readonly BaseBuilderQueueManager[] builders;
		int currentBuilderIndex = 0;

		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> refineryBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> powerBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> constructionYardBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> barracksBuildings;

		public BaseBuilderBotModule(Actor self, BaseBuilderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			builders = new BaseBuilderQueueManager[info.BuildingQueues.Count + info.DefenseQueues.Count];
			refineryBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.RefineryTypes, player);
			powerBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.PowerTypes, player);
			constructionYardBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			barracksBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.BarracksTypes, player);
		}

		protected override void Created(Actor self)
		{
			playerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			resourceLayer = self.World.WorldActor.TraitOrDefault<IResourceLayer>();
			pathFinder = self.World.WorldActor.TraitOrDefault<IPathFinder>();
			positionsUpdatedModules = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();

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
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation)
		{
			DefenseCenter = newLocation;
		}

		bool IBotRequestPauseUnitProduction.PauseUnitProduction => !IsTraitDisabled && !HasAdequateRefineryCount();

		void IBotTick.BotTick(IBot bot)
		{
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
			AIUtils.CountActorByCommonName(refineryBuildings) >= MinimumRefineryCount() ||
			AIUtils.CountActorByCommonName(powerBuildings) == 0 ||
			AIUtils.CountActorByCommonName(constructionYardBuildings) == 0;

		int MinimumRefineryCount() =>
			AIUtils.CountActorByCommonName(barracksBuildings) > 0
			? Info.InititalMinimumRefineryCount + Info.AdditionalMinimumRefineryCount
			: Info.InititalMinimumRefineryCount;

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new("DefenseCenter", FieldSaver.FormatValue(DefenseCenter))
			};
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
			refineryBuildings.Dispose();
			powerBuildings.Dispose();
			constructionYardBuildings.Dispose();
			barracksBuildings.Dispose();
		}
	}
}
