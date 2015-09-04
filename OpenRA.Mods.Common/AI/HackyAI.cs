#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	public sealed class HackyAIInfo : IBotInfo, ITraitInfo
	{
		[Desc("Ingame name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

		[Desc("Random number of up to this many units is added to squad size when creating an attack squad.")]
		public readonly int SquadSizeRandomBonus = 30;

		[Desc("Production queues AI uses for buildings.")]
		public readonly string[] BuildingQueues = { "Building" };

		[Desc("Production queues AI uses for defenses.")]
		public readonly string[] DefenseQueues = { "Defense" };

		[Desc("Delay (in ticks) between giving out orders to units.")]
		public readonly int AssignRolesInterval = 20;

		[Desc("Delay (in ticks) between attempting rush attacks.")]
		public readonly int RushInterval = 600;

		[Desc("Delay (in ticks) between updating squads.")]
		public readonly int AttackForceInterval = 30;

		[Desc("Minimum delay (in ticks) between creating squads.")]
		public readonly int MinimumAttackForceDelay = 0;

		[Desc("Minimum portion of pending orders to issue each tick (e.g. 5 issues at least 1/5th of all pending orders). Excess orders remain queued for subsequent ticks.")]
		public readonly int MinOrderQuotientPerTick = 5;

		[Desc("Minimum excess power the AI should try to maintain.")]
		public readonly int MinimumExcessPower = 0;

		[Desc("Delay (in ticks) between structure production checks when there is no active production.",
			"A StructureProductionRandomBonusDelay is added to this.")]
		public readonly int StructureProductionInactiveDelay = 125;

		[Desc("Delay (in ticks) between structure production checks when actively building things.",
			"A StructureProductionRandomBonusDelay is added to this.")]
		public readonly int StructureProductionActiveDelay = 10;

		[Desc("A random delay (in ticks) of up to this is added to active/inactive production delays.")]
		public readonly int StructureProductionRandomBonusDelay = 10;

		[Desc("Delay (in ticks) until retrying to build structure after the last 3 consecutive attempts failed.")]
		public readonly int StructureProductionResumeDelay = 1500;

		[Desc("After how many failed attempts to place a structure should AI give up and wait",
			"for StructureProductionResumeDelay before retrying.")]
		public readonly int MaximumFailedPlacementAttempts = 3;

		[Desc("Delay (in ticks) until rechecking for new BaseProviders.")]
		public readonly int CheckForNewBasesDelay = 1500;

		[Desc("Minimum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MinimumDefenseRadius = 5;

		[Desc("Maximum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MaximumDefenseRadius = 20;

		[Desc("Try to build another production building if there is too much cash.")]
		public readonly int NewProductionCashThreshold = 5000;

		[Desc("Only produce units as long as there are less than this amount of units idling inside the base.")]
		public readonly int IdleBaseUnitsMaximum = 12;

		[Desc("Radius in cells around enemy BaseBuilder (Construction Yard) where AI scans for targets to rush.")]
		public readonly int RushAttackScanRadius = 15;

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

		[Desc("Radius in cells around a factory scanned for rally points by the AI.")]
		public readonly int RallyPointScanRadius = 8;

		[Desc("Radius in cells around the center of the base to expand.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Radius in cells around each building with ProvideBuildableArea",
			"to check for a 3x3 area of water where naval structures can be built.",
			"Should match maximum adjacency of naval structures.")]
		public readonly int CheckForWaterRadius = 8;

		[Desc("Production queues AI uses for producing units.")]
		public readonly string[] UnitQueues = { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };

		[Desc("Should the AI repair its buildings if damaged?")]
		public readonly bool ShouldRepairBuildings = true;

		string IBotInfo.Name { get { return this.Name; } }

		[Desc("What units to the AI should build.", "What % of the total army must be this type of unit.")]
		public readonly Dictionary<string, float> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("What buildings to the AI should build.", "What % of the total base must be this type of building.")]
		public readonly Dictionary<string, float> BuildingFractions = null;

		[Desc("Tells the AI what unit types fall under the same common name.")]
		public readonly Dictionary<string, string[]> UnitsCommonNames = null;

		[Desc("Tells the AI what building types fall under the same common name.")]
		public readonly Dictionary<string, string[]> BuildingCommonNames = null;

		[Desc("What buildings should the AI have a maximum limit to build.")]
		public readonly Dictionary<string, int> BuildingLimits = null;

		// TODO Update OpenRA.Utility/Command.cs#L300 to first handle lists and also read nested ones
		[Desc("Tells the AI how to use its support powers.")]
		[FieldLoader.LoadUsing("LoadDecisions")]
		public readonly List<SupportPowerDecision> PowerDecisions = new List<SupportPowerDecision>();

		static object LoadDecisions(MiniYaml yaml)
		{
			var ret = new List<SupportPowerDecision>();
			foreach (var d in yaml.Nodes)
				if (d.Key.Split('@')[0] == "SupportPowerDecision")
					ret.Add(new SupportPowerDecision(d.Value));

			return ret;
		}

		public object Create(ActorInitializer init) { return new HackyAI(this, init); }
	}

	public class Enemy { public int Aggro; }

	public enum BuildingType { Building, Defense, Refinery }

	public sealed class HackyAI : ITick, IBot, INotifyDamage
	{
		public MersenneTwister Random { get; private set; }
		public readonly HackyAIInfo Info;

		public CPos GetRandomBaseCenter()
		{
			var randomBaseBuilding = World.Actors.Where(
				a => a.Owner == Player
					&& a.HasTrait<BaseBuilding>()
					&& !a.HasTrait<Mobile>())
				.RandomOrDefault(Random);

			return randomBaseBuilding != null ? randomBaseBuilding.Location : initialBaseCenter;
		}

		public Player Player { get; private set; }

		readonly Func<Actor, bool> isEnemyUnit;
		Dictionary<SupportPowerInstance, int> waitingPowers = new Dictionary<SupportPowerInstance, int>();
		Dictionary<string, SupportPowerDecision> powerDecisions = new Dictionary<string, SupportPowerDecision>();

		CPos initialBaseCenter;
		PowerManager playerPower;
		SupportPowerManager supportPowerMngr;
		PlayerResources playerResource;
		bool enabled;
		int ticks;

		BitArray resourceTypeIndices;

		RushFuzzy rushFuzzy = new RushFuzzy();

		Cache<Player, Enemy> aggro = new Cache<Player, Enemy>(_ => new Enemy());
		List<BaseBuilder> builders = new List<BaseBuilder>();

		List<Squad> squads = new List<Squad>();
		List<Actor> unitsHangingAroundTheBase = new List<Actor>();

		// Units that the ai already knows about. Any unit not on this list needs to be given a role.
		List<Actor> activeUnits = new List<Actor>();

		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		public readonly World World;
		public Map Map { get { return World.Map; } }
		IBotInfo IBot.Info { get { return this.Info; } }

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int minAttackForceDelayTicks;

		readonly Queue<Order> orders = new Queue<Order>();

		public HackyAI(HackyAIInfo info, ActorInitializer init)
		{
			Info = info;
			World = init.World;
			isEnemyUnit = unit =>
				Player.Stances[unit.Owner] == Stance.Enemy && !unit.HasTrait<Husk>() && unit.HasTrait<ITargetable>();

			foreach (var decision in info.PowerDecisions)
				powerDecisions.Add(decision.OrderName, decision);
		}

		public static void BotDebug(string s, params object[] args)
		{
			if (Game.Settings.Debug.BotDebug)
				Game.Debug(s, args);
		}

		// Called by the host's player creation code
		public void Activate(Player p)
		{
			Player = p;
			enabled = true;
			playerPower = p.PlayerActor.Trait<PowerManager>();
			supportPowerMngr = p.PlayerActor.Trait<SupportPowerManager>();
			playerResource = p.PlayerActor.Trait<PlayerResources>();

			foreach (var building in Info.BuildingQueues)
				builders.Add(new BaseBuilder(this, building, p, playerPower, playerResource));
			foreach (var defense in Info.DefenseQueues)
				builders.Add(new BaseBuilder(this, defense, p, playerPower, playerResource));

			Random = new MersenneTwister((int)p.PlayerActor.ActorID);

			// Avoid all AIs trying to rush in the same tick, randomize their initial rush a little.
			var smallFractionOfRushInterval = Info.RushInterval / 20;
			rushTicks = Random.Next(Info.RushInterval - smallFractionOfRushInterval, Info.RushInterval + smallFractionOfRushInterval);

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = Random.Next(0, Info.AssignRolesInterval);
			attackForceTicks = Random.Next(0, Info.AttackForceInterval);
			minAttackForceDelayTicks = Random.Next(0, Info.MinimumAttackForceDelay);

			resourceTypeIndices = new BitArray(World.TileSet.TerrainInfo.Length); // Big enough
			foreach (var t in Map.Rules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>())
				resourceTypeIndices.Set(World.TileSet.GetTerrainIndex(t.TerrainType), true);
		}

		// TODO: Possibly give this a more generic name when terrain type is unhardcoded
		public bool EnoughWaterToBuildNaval()
		{
			var baseProviders = World.Actors.Where(
				a => a.Owner == Player
					&& a.HasTrait<BaseProvider>()
					&& !a.HasTrait<Mobile>());

			foreach (var b in baseProviders)
			{
				// TODO: Unhardcode terrain type
				// TODO2: Properly check building foundation rather than 3x3 area
				var playerWorld = Player.World;
				var countWaterCells = Map.FindTilesInCircle(b.Location, Info.MaxBaseRadius)
					.Where(c => playerWorld.Map.Contains(c)
						&& playerWorld.Map.GetTerrainInfo(c).IsWater
						&& Util.AdjacentCells(playerWorld, Target.FromCell(playerWorld, c))
							.All(a => playerWorld.Map.GetTerrainInfo(a).IsWater))
					.Count();

				if (countWaterCells > 0)
					return true;
			}

			return false;
		}

		// Check whether we have at least one building providing buildable area close enough to water to build naval structures
		public bool CloseEnoughToWater()
		{
			var areaProviders = World.Actors.Where(
				a => a.Owner == Player
					&& a.HasTrait<GivesBuildableArea>()
					&& !a.HasTrait<Mobile>());

			foreach (var a in areaProviders)
			{
				// TODO: Unhardcode terrain type
				// TODO2: Properly check building foundation rather than 3x3 area
				var playerWorld = Player.World;
				var adjacentWater = Map.FindTilesInCircle(a.Location, Info.CheckForWaterRadius)
					.Where(c => playerWorld.Map.Contains(c)
						&& playerWorld.Map.GetTerrainInfo(c).IsWater
						&& Util.AdjacentCells(playerWorld, Target.FromCell(playerWorld, c))
							.All(b => playerWorld.Map.GetTerrainInfo(b).IsWater))
					.Count();

				if (adjacentWater > 0)
					return true;
			}

			return false;
		}

		public void QueueOrder(Order order)
		{
			orders.Enqueue(order);
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var unit = buildableThings.Random(Random);
			return HasAdequateAirUnitReloadBuildings(unit) ? unit : null;
		}

		ActorInfo ChooseUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var myUnits = Player.World
				.ActorsWithTrait<IPositionable>()
				.Where(a => a.Actor.Owner == Player)
				.Select(a => a.Actor.Info.Name).ToList();

			foreach (var unit in Info.UnitsToBuild.Shuffle(Random))
				if (buildableThings.Any(b => b.Name == unit.Key))
					if (myUnits.Count(a => a == unit.Key) < unit.Value * myUnits.Count)
						if (HasAdequateAirUnitReloadBuildings(Map.Rules.Actors[unit.Key]))
							return Map.Rules.Actors[unit.Key];

			return null;
		}

		int CountBuilding(string frac, Player owner)
		{
			return World.ActorsWithTrait<Building>()
				.Count(a => a.Actor.Owner == owner && a.Actor.Info.Name == frac);
		}

		int CountUnits(string unit, Player owner)
		{
			return World.ActorsWithTrait<IPositionable>()
				.Count(a => a.Actor.Owner == owner && a.Actor.Info.Name == unit);
		}

		int? CountBuildingByCommonName(string commonName, Player owner)
		{
			if (!Info.BuildingCommonNames.ContainsKey(commonName))
				return null;

			return World.ActorsWithTrait<Building>()
				.Count(a => a.Actor.Owner == owner && Info.BuildingCommonNames[commonName].Contains(a.Actor.Info.Name));
		}

		public ActorInfo GetBuildingInfoByCommonName(string commonName, Player owner)
		{
			if (commonName == "ConstructionYard")
				return Map.Rules.Actors.Where(k => Info.BuildingCommonNames[commonName].Contains(k.Key)).Random(Random).Value;

			return GetInfoByCommonName(Info.BuildingCommonNames, commonName, owner);
		}

		public ActorInfo GetUnitInfoByCommonName(string commonName, Player owner)
		{
			return GetInfoByCommonName(Info.UnitsCommonNames, commonName, owner);
		}

		public ActorInfo GetInfoByCommonName(Dictionary<string, string[]> names, string commonName, Player owner)
		{
			if (!names.Any() || !names.ContainsKey(commonName))
				throw new InvalidOperationException("Can't find {0} in the HackyAI UnitsCommonNames definition.".F(commonName));

			return Map.Rules.Actors.Where(k => names[commonName].Contains(k.Key)).Random(Random).Value;
		}

		public bool HasAdequateFact()
		{
			// Require at least one construction yard, unless we have no vehicles factory (can't build it).
			return CountBuildingByCommonName("ConstructionYard", Player) > 0 ||
				CountBuildingByCommonName("VehiclesFactory", Player) == 0;
		}

		public bool HasAdequateProc()
		{
			// Require at least one refinery, unless we have no power (can't build it).
			return CountBuildingByCommonName("Refinery", Player) > 0 ||
				CountBuildingByCommonName("Power", Player) == 0;
		}

		public bool HasMinimumProc()
		{
			// Require at least two refineries, unless we have no power (can't build it)
			// or barracks (higher priority?)
			return CountBuildingByCommonName("Refinery", Player) >= 2 ||
				CountBuildingByCommonName("Power", Player) == 0 ||
				CountBuildingByCommonName("Barracks", Player) == 0;
		}

		// For mods like RA (number of building must match the number of aircraft)
		bool HasAdequateAirUnitReloadBuildings(ActorInfo actorInfo)
		{
			var aircraftInfo = actorInfo.Traits.GetOrDefault<AircraftInfo>();
			if (aircraftInfo == null)
				return true;

			var ammoPoolsInfo = actorInfo.Traits.WithInterface<AmmoPoolInfo>();

			if (ammoPoolsInfo.Any(x => !x.SelfReloads))
			{
				var countOwnAir = CountUnits(actorInfo.Name, Player);
				var countBuildings = aircraftInfo.RearmBuildings.Sum(b => CountBuilding(b, Player));
				if (countOwnAir >= countBuildings)
					return false;
			}

			return true;
		}

		CPos defenseCenter;
		public CPos? ChooseBuildLocation(string actorType, bool distanceToBaseIsImportant, BuildingType type)
		{
			var bi = Map.Rules.Actors[actorType].Traits.GetOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			// Find the buildable cell that is closest to pos and centered around center
			Func<CPos, CPos, int, int, CPos?> findPos = (center, target, minRange, maxRange) =>
			{
				var cells = Map.FindTilesInAnnulus(center, minRange, maxRange);

				// Sort by distance to target if we have one
				if (center != target)
					cells = cells.OrderBy(c => (c - target).LengthSquared);
				else
					cells = cells.Shuffle(Random);

				foreach (var cell in cells)
				{
					if (!World.CanPlaceBuilding(actorType, bi, cell, null))
						continue;

					if (distanceToBaseIsImportant && !bi.IsCloseEnoughToBase(World, Player, actorType, cell))
						continue;

					return cell;
				}

				return null;
			};

			var baseCenter = GetRandomBaseCenter();

			switch (type)
			{
				case BuildingType.Defense:

					// Build near the closest enemy structure
					var closestEnemy = World.Actors.Where(a => !a.Disposed && a.HasTrait<Building>() && Player.Stances[a.Owner] == Stance.Enemy)
						.ClosestTo(World.Map.CenterOfCell(defenseCenter));

					var targetCell = closestEnemy != null ? closestEnemy.Location : baseCenter;
					return findPos(defenseCenter, targetCell, Info.MinimumDefenseRadius, Info.MaximumDefenseRadius);

				case BuildingType.Refinery:

					// Try and place the refinery near a resource field
					var nearbyResources = Map.FindTilesInCircle(baseCenter, Info.MaxBaseRadius)
						.Where(a => resourceTypeIndices.Get(Map.GetTerrainIndex(a)))
						.Shuffle(Random);

					foreach (var c in nearbyResources)
					{
						var found = findPos(c, baseCenter, 0, Info.MaxBaseRadius);
						if (found != null)
							return found;
					}

					// Try and find a free spot somewhere else in the base
					return findPos(baseCenter, baseCenter, 0, Info.MaxBaseRadius);

				case BuildingType.Building:
					return findPos(baseCenter, baseCenter, 0, distanceToBaseIsImportant ? Info.MaxBaseRadius : Map.MaxTilesInCircleRange);
			}

			// Can't find a build location
			return null;
		}

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			ticks++;

			if (ticks == 1)
				InitializeBase(self);

			if (ticks % FeedbackTime == 0)
				ProductionUnits(self);

			AssignRolesToIdleUnits(self);
			SetRallyPointsForNewProductionBuildings(self);
			TryToUseSupportPower(self);

			foreach (var b in builders)
				b.Tick();

			var ordersToIssueThisTick = Math.Min((orders.Count + Info.MinOrderQuotientPerTick - 1) / Info.MinOrderQuotientPerTick, orders.Count);
			for (var i = 0; i < ordersToIssueThisTick; i++)
				World.IssueOrder(orders.Dequeue());
		}

		internal Actor ChooseEnemyTarget()
		{
			if (Player.WinState != WinState.Undefined)
				return null;

			var liveEnemies = World.Players
				.Where(p => Player != p && Player.Stances[p] == Stance.Enemy && p.WinState == WinState.Undefined);

			if (!liveEnemies.Any())
				return null;

			var leastLikedEnemies = liveEnemies
				.GroupBy(e => aggro[e].Aggro)
				.MaxByOrDefault(g => g.Key);

			var enemy = (leastLikedEnemies != null) ?
				leastLikedEnemies.Random(Random) : liveEnemies.FirstOrDefault();

			// Pick something worth attacking owned by that player
			var target = World.Actors
				.Where(a => a.Owner == enemy && a.HasTrait<IOccupySpace>())
				.ClosestTo(World.Map.CenterOfCell(GetRandomBaseCenter()));

			if (target == null)
			{
				/* Assume that "enemy" has nothing. Cool off on attacks. */
				aggro[enemy].Aggro = aggro[enemy].Aggro / 2 - 1;
				Log.Write("debug", "Bot {0} couldn't find target for player {1}", Player.ClientIndex, enemy.ClientIndex);

				return null;
			}

			// Bump the aggro slightly to avoid changing our mind
			if (leastLikedEnemies.Count() > 1)
				aggro[enemy].Aggro++;

			return target;
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			return World.Actors.Where(isEnemyUnit).ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return World.FindActorsInCircle(pos, radius).Where(isEnemyUnit).ClosestTo(pos);
		}

		List<Actor> FindEnemyConstructionYards()
		{
			return World.Actors.Where(a => Player.Stances[a.Owner] == Stance.Enemy && !a.IsDead
				&& a.HasTrait<BaseBuilding>() && !a.HasTrait<Mobile>()).ToList();
		}

		void CleanSquads()
		{
			squads.RemoveAll(s => !s.IsValid);
			foreach (var s in squads)
				s.Units.RemoveAll(a => a.IsDead || a.Owner != Player);
		}

		// Use of this function requires that one squad of this type. Hence it is a piece of shit
		Squad GetSquadOfType(SquadType type)
		{
			return squads.FirstOrDefault(s => s.Type == type);
		}

		Squad RegisterNewSquad(SquadType type, Actor target = null)
		{
			var ret = new Squad(this, type, target);
			squads.Add(ret);
			return ret;
		}

		void AssignRolesToIdleUnits(Actor self)
		{
			CleanSquads();
			activeUnits.RemoveAll(a => a.IsDead || a.Owner != Player);
			unitsHangingAroundTheBase.RemoveAll(a => a.IsDead || a.Owner != Player);

			if (--rushTicks <= 0)
			{
				rushTicks = Info.RushInterval;
				TryToRushAttack();
			}

			if (--attackForceTicks <= 0)
			{
				attackForceTicks = Info.AttackForceInterval;
				foreach (var s in squads)
					s.Update();
			}

			if (--assignRolesTicks > 0)
				return;

			assignRolesTicks = Info.AssignRolesInterval;

			GiveOrdersToIdleHarvesters();
			FindNewUnits(self);
			if (--minAttackForceDelayTicks <= 0)
			{
				minAttackForceDelayTicks = Info.MinimumAttackForceDelay;
				CreateAttackForce();
			}

			FindAndDeployBackupMcv(self);
		}

		void GiveOrdersToIdleHarvesters()
		{
			// Find idle harvesters and give them orders:
			foreach (var a in activeUnits)
			{
				var harv = a.TraitOrDefault<Harvester>();
				if (harv == null)
					continue;

				if (!a.IsIdle)
				{
					var act = a.GetCurrentActivity();

					// A Wait activity is technically idle:
					if ((act.GetType() != typeof(Wait)) &&
						(act.NextActivity == null || act.NextActivity.GetType() != typeof(FindResources)))
						continue;
				}

				if (!harv.IsEmpty)
					continue;

				// Tell the idle harvester to quit slacking:
				QueueOrder(new Order("Harvest", a, false));
			}
		}

		void FindNewUnits(Actor self)
		{
			var newUnits = self.World.ActorsWithTrait<IPositionable>()
				.Where(a => a.Actor.Owner == Player && !a.Actor.HasTrait<BaseBuilding>()
					&& !activeUnits.Contains(a.Actor))
				.Select(a => a.Actor);

			foreach (var a in newUnits)
			{
				if (a.HasTrait<Harvester>())
					QueueOrder(new Order("Harvest", a, false));
				else
					unitsHangingAroundTheBase.Add(a);

				if (a.HasTrait<Aircraft>() && a.HasTrait<AttackBase>())
				{
					var air = GetSquadOfType(SquadType.Air);
					if (air == null)
						air = RegisterNewSquad(SquadType.Air);

					air.Units.Add(a);
				}

				activeUnits.Add(a);
			}
		}

		void CreateAttackForce()
		{
			// Create an attack force when we have enough units around our base.
			// (don't bother leaving any behind for defense)
			var randomizedSquadSize = Info.SquadSize + Random.Next(Info.SquadSizeRandomBonus);

			if (unitsHangingAroundTheBase.Count >= randomizedSquadSize)
			{
				var attackForce = RegisterNewSquad(SquadType.Assault);

				foreach (var a in unitsHangingAroundTheBase)
					if (!a.HasTrait<Aircraft>())
						attackForce.Units.Add(a);

				unitsHangingAroundTheBase.Clear();
			}
		}

		void TryToRushAttack()
		{
			var allEnemyBaseBuilder = FindEnemyConstructionYards();
			var ownUnits = activeUnits
				.Where(unit => unit.HasTrait<AttackBase>() && !unit.HasTrait<Aircraft>() && unit.IsIdle).ToList();

			if (!allEnemyBaseBuilder.Any() || (ownUnits.Count < Info.SquadSize))
				return;

			foreach (var b in allEnemyBaseBuilder)
			{
				var enemies = World.FindActorsInCircle(b.CenterPosition, WDist.FromCells(Info.RushAttackScanRadius))
					.Where(unit => Player.Stances[unit.Owner] == Stance.Enemy && unit.HasTrait<AttackBase>()).ToList();

				if (rushFuzzy.CanAttack(ownUnits, enemies))
				{
					var target = enemies.Any() ? enemies.Random(Random) : b;
					var rush = GetSquadOfType(SquadType.Rush);
					if (rush == null)
						rush = RegisterNewSquad(SquadType.Rush, target);

					foreach (var a3 in ownUnits)
						rush.Units.Add(a3);

					return;
				}
			}
		}

		void ProtectOwn(Actor attacker)
		{
			var protectSq = GetSquadOfType(SquadType.Protection);
			if (protectSq == null)
				protectSq = RegisterNewSquad(SquadType.Protection, attacker);

			if (!protectSq.IsTargetValid)
				protectSq.TargetActor = attacker;

			if (!protectSq.IsValid)
			{
				var ownUnits = World.FindActorsInCircle(World.Map.CenterOfCell(GetRandomBaseCenter()), WDist.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit => unit.Owner == Player && !unit.HasTrait<Building>()
						&& unit.HasTrait<AttackBase>());

				foreach (var a in ownUnits)
					protectSq.Units.Add(a);
			}
		}

		bool IsRallyPointValid(CPos x, BuildingInfo info)
		{
			return info != null && World.IsCellBuildable(x, info);
		}

		void SetRallyPointsForNewProductionBuildings(Actor self)
		{
			var buildings = self.World.ActorsWithTrait<RallyPoint>()
				.Where(rp => rp.Actor.Owner == Player &&
					!IsRallyPointValid(rp.Trait.Location, rp.Actor.Info.Traits.GetOrDefault<BuildingInfo>())).ToArray();

			foreach (var a in buildings)
				QueueOrder(new Order("SetRallyPoint", a.Actor, false) { TargetLocation = ChooseRallyLocationNear(a.Actor), SuppressVisualFeedback = true });
		}

		// Won't work for shipyards...
		CPos ChooseRallyLocationNear(Actor producer)
		{
			var possibleRallyPoints = Map.FindTilesInCircle(producer.Location, Info.RallyPointScanRadius)
				.Where(c => IsRallyPointValid(c, producer.Info.Traits.GetOrDefault<BuildingInfo>()));

			if (!possibleRallyPoints.Any())
			{
				BotDebug("Bot Bug: No possible rallypoint near {0}", producer.Location);
				return producer.Location;
			}

			return possibleRallyPoints.Random(Random);
		}

		void InitializeBase(Actor self)
		{
			// Find and deploy our mcv
			var mcv = self.World.Actors
				.FirstOrDefault(a => a.Owner == Player && a.HasTrait<BaseBuilding>());

			if (mcv != null)
			{
				initialBaseCenter = mcv.Location;
				defenseCenter = mcv.Location;

				// Don't transform the mcv if it is a fact
				// HACK: This needs to query against MCVs directly
				if (mcv.HasTrait<Mobile>())
					QueueOrder(new Order("DeployTransform", mcv, false));
			}
			else
				BotDebug("AI: Can't find BaseBuildUnit.");
		}

		// Find any newly constructed MCVs and deploy them at a sensible
		// backup location within the main base.
		void FindAndDeployBackupMcv(Actor self)
		{
			// HACK: This needs to query against MCVs directly
			var mcvs = self.World.Actors.Where(a => a.Owner == Player && a.HasTrait<BaseBuilding>() && a.HasTrait<Mobile>());
			if (!mcvs.Any())
				return;

			foreach (var mcv in mcvs)
			{
				if (mcv.IsMoving())
					continue;

				var factType = mcv.Info.Traits.Get<TransformsInfo>().IntoActor;
				var desiredLocation = ChooseBuildLocation(factType, false, BuildingType.Building);
				if (desiredLocation == null)
					continue;

				QueueOrder(new Order("Move", mcv, true) { TargetLocation = desiredLocation.Value });
				QueueOrder(new Order("DeployTransform", mcv, true));
			}
		}

		void TryToUseSupportPower(Actor self)
		{
			if (supportPowerMngr == null)
				return;

			var powers = supportPowerMngr.Powers.Where(p => !p.Value.Disabled);
			foreach (var kv in powers)
			{
				var sp = kv.Value;

				// Add power to dictionary if not in delay dictionary yet
				if (!waitingPowers.ContainsKey(sp))
					waitingPowers.Add(sp, 0);

				if (waitingPowers[sp] > 0)
					waitingPowers[sp]--;

				// If we have recently tried and failed to find a use location for a power, then do not try again until later
				var isDelayed = waitingPowers[sp] > 0;
				if (sp.Ready && !isDelayed && powerDecisions.ContainsKey(sp.Info.OrderName))
				{
					var powerDecision = powerDecisions[sp.Info.OrderName];
					if (powerDecision == null)
					{
						BotDebug("Bot Bug: FindAttackLocationToSupportPower, couldn't find powerDecision for {0}", sp.Info.OrderName);
						continue;
					}

					var attackLocation = FindCoarseAttackLocationToSupportPower(sp);
					if (attackLocation == null)
					{
						BotDebug("AI: {1} can't find suitable coarse attack location for support power {0}. Delaying rescan.", sp.Info.OrderName, Player.PlayerName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(this);

						continue;
					}

					// Found a target location, check for precise target
					attackLocation = FindFineAttackLocationToSupportPower(sp, (CPos)attackLocation);
					if (attackLocation == null)
					{
						BotDebug("AI: {1} can't find suitable final attack location for support power {0}. Delaying rescan.", sp.Info.OrderName, Player.PlayerName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(this);

						continue;
					}

					// Valid target found, delay by a few ticks to avoid rescanning before power fires via order
					BotDebug("AI: {2} found new target location {0} for support power {1}.", attackLocation, sp.Info.OrderName, Player.PlayerName);
					waitingPowers[sp] += 10;
					QueueOrder(new Order(sp.Info.OrderName, supportPowerMngr.Self, false) { TargetLocation = attackLocation.Value, SuppressVisualFeedback = true });
				}
			}
		}

		/// <summary>Scans the map in chunks, evaluating all actors in each.</summary>
		CPos? FindCoarseAttackLocationToSupportPower(SupportPowerInstance readyPower)
		{
			CPos? bestLocation = null;
			var bestAttractiveness = 0;
			var powerDecision = powerDecisions[readyPower.Info.OrderName];
			if (powerDecision == null)
			{
				BotDebug("Bot Bug: FindAttackLocationToSupportPower, couldn't find powerDecision for {0}", readyPower.Info.OrderName);
				return null;
			}

			var checkRadius = powerDecision.CoarseScanRadius;
			for (var i = 0; i < World.Map.MapSize.X; i += checkRadius)
			{
				for (var j = 0; j < World.Map.MapSize.Y; j += checkRadius)
				{
					var consideredAttractiveness = 0;

					var tl = World.Map.CenterOfCell(new CPos(i, j));
					var br = World.Map.CenterOfCell(new CPos(i + checkRadius, j + checkRadius));
					var targets = World.ActorMap.ActorsInBox(tl, br);

					consideredAttractiveness = powerDecision.GetAttractiveness(targets, Player);
					if (consideredAttractiveness <= bestAttractiveness || consideredAttractiveness < powerDecision.MinimumAttractiveness)
						continue;

					bestAttractiveness = consideredAttractiveness;
					bestLocation = new CPos(i, j);
				}
			}

			return bestLocation;
		}

		/// <summary>Detail scans an area, evaluating positions.</summary>
		CPos? FindFineAttackLocationToSupportPower(SupportPowerInstance readyPower, CPos checkPos, int extendedRange = 1)
		{
			CPos? bestLocation = null;
			var bestAttractiveness = 0;
			var powerDecision = powerDecisions[readyPower.Info.OrderName];
			if (powerDecision == null)
			{
				BotDebug("Bot Bug: FindAttackLocationToSupportPower, couldn't find powerDecision for {0}", readyPower.Info.OrderName);
				return null;
			}

			var checkRadius = powerDecision.CoarseScanRadius;
			var fineCheck = powerDecision.FineScanRadius;
			for (var i = 0 - extendedRange; i <= (checkRadius + extendedRange); i += fineCheck)
			{
				var x = checkPos.X + i;

				for (var j = 0 - extendedRange; j <= (checkRadius + extendedRange); j += fineCheck)
				{
					var y = checkPos.Y + j;
					var pos = World.Map.CenterOfCell(new CPos(x, y));
					var consideredAttractiveness = 0;
					consideredAttractiveness += powerDecision.GetAttractiveness(pos, Player);

					if (consideredAttractiveness <= bestAttractiveness || consideredAttractiveness < powerDecision.MinimumAttractiveness)
						continue;

					bestAttractiveness = consideredAttractiveness;
					bestLocation = new CPos(x, y);
				}
			}

			return bestLocation;
		}

		internal IEnumerable<ProductionQueue> FindQueues(string category)
		{
			return World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == Player && a.Trait.Info.Type == category && a.Trait.Enabled)
				.Select(a => a.Trait);
		}

		void ProductionUnits(Actor self)
		{
			// Stop building until economy is restored
			if (!HasAdequateProc())
				return;

			// No construction yards - Build a new MCV
			if (!HasAdequateFact() && !self.World.Actors.Any(a => a.Owner == Player && a.HasTrait<BaseBuilding>() && a.HasTrait<Mobile>()))
				BuildUnit("Vehicle", GetUnitInfoByCommonName("Mcv", Player).Name);

			foreach (var q in Info.UnitQueues)
				BuildUnit(q, unitsHangingAroundTheBase.Count < Info.IdleBaseUnitsMaximum);
		}

		void BuildUnit(string category, bool buildRandom)
		{
			// Pick a free queue
			var queue = FindQueues(category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null)
				return;

			var unit = buildRandom ?
				ChooseRandomUnitToBuild(queue) :
				ChooseUnitToBuild(queue);

			if (unit != null
				&& Info.UnitsToBuild.ContainsKey(unit.Name)
				&& (!Info.UnitLimits.ContainsKey(unit.Name)
					|| World.Actors.Count(a => a.Info.Name == unit.Name && a.Owner == Player) < Info.UnitLimits[unit.Name]))

				QueueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
		}

		void BuildUnit(string category, string name)
		{
			var queue = FindQueues(category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null)
				return;

			if (Map.Rules.Actors[name] != null)
				QueueOrder(Order.StartProduction(queue.Actor, name, 1));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!enabled || e.Attacker == null)
				return;

			if (e.Attacker.Owner.Stances[self.Owner] == Stance.Neutral)
				return;

			var rb = self.TraitOrDefault<RepairableBuilding>();

			if (Info.ShouldRepairBuildings && rb != null)
			{
				if (e.DamageState > DamageState.Light && e.PreviousDamageState <= DamageState.Light && !rb.RepairActive)
				{
					BotDebug("Bot noticed damage {0} {1}->{2}, repairing.",
						self, e.PreviousDamageState, e.DamageState);
					QueueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, false) { TargetActor = self });
				}
			}

			if (e.Attacker.Disposed)
				return;

			if (!e.Attacker.HasTrait<ITargetable>())
				return;

			if (e.Damage > 0)
				aggro[e.Attacker.Owner].Aggro += e.Damage;

			// Protected harvesters or building
			if ((self.HasTrait<Harvester>() || self.HasTrait<Building>()) &&
				Player.Stances[e.Attacker.Owner] == Stance.Enemy)
			{
				defenseCenter = e.Attacker.Location;
				ProtectOwn(e.Attacker);
			}
		}
	}
}
