#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Power;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	public sealed class HackyAIInfo : IBotInfo, ITraitInfo
	{
		[Desc("Ingame name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

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

		[Desc("How long to wait (in ticks) between structure production checks when there is no active production.")]
		public readonly int StructureProductionInactiveDelay = 125;

		[Desc("How long to wait (in ticks) between structure production checks ticks when actively building things.")]
		public readonly int StructureProductionActiveDelay = 10;

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

		[Desc("Production queues AI uses for producing units.")]
		public readonly string[] UnitQueues = { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };

		[Desc("Should the AI repair its buildings if damaged?")]
		public readonly bool ShouldRepairBuildings = true;

		string IBotInfo.Name { get { return this.Name; } }

		[Desc("What units to the AI should build.", "What % of the total army must be this type of unit.")]
		[FieldLoader.LoadUsing("LoadUnits")]
		public readonly Dictionary<string, float> UnitsToBuild = null;

		[Desc("What buildings to the AI should build.", "What % of the total base must be this type of building.")]
		[FieldLoader.LoadUsing("LoadBuildings")]
		public readonly Dictionary<string, float> BuildingFractions = null;

		[Desc("Tells the AI what unit types fall under the same common name.")]
		[FieldLoader.LoadUsing("LoadUnitsCommonNames")]
		public readonly Dictionary<string, string[]> UnitsCommonNames = null;

		[Desc("Tells the AI what building types fall under the same common name.")]
		[FieldLoader.LoadUsing("LoadBuildingsCommonNames")]
		public readonly Dictionary<string, string[]> BuildingCommonNames = null;

		[Desc("What buildings should the AI have max limits n.", "What is the limit of the building.")]
		[FieldLoader.LoadUsing("LoadBuildingLimits")]
		public readonly Dictionary<string, int> BuildingLimits = null;

		// TODO Update OpenRA.Utility/Command.cs#L300 to first handle lists and also read nested ones
		[Desc("Tells the AI how to use its support powers.")]
		[FieldLoader.LoadUsing("LoadDecisions")]
		public readonly List<SupportPowerDecision> PowerDecisions = new List<SupportPowerDecision>();

		static object LoadList<T>(MiniYaml y, string field)
		{
			var nd = y.ToDictionary();
			return nd.ContainsKey(field)
				? nd[field].ToDictionary(my => FieldLoader.GetValue<T>(field, my.Value))
				: new Dictionary<string, T>();
		}

		static object LoadUnits(MiniYaml y) { return LoadList<float>(y, "UnitsToBuild"); }
		static object LoadBuildings(MiniYaml y) { return LoadList<float>(y, "BuildingFractions"); }

		static object LoadUnitsCommonNames(MiniYaml y) { return LoadList<string[]>(y, "UnitsCommonNames"); }
		static object LoadBuildingsCommonNames(MiniYaml y) { return LoadList<string[]>(y, "BuildingCommonNames"); }

		static object LoadBuildingLimits(MiniYaml y) { return LoadList<int>(y, "BuildingLimits"); }

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
		public MersenneTwister random { get; private set; }
		public readonly HackyAIInfo Info;
		public CPos baseCenter { get; private set; }
		public Player p { get; private set; }

		Dictionary<SupportPowerInstance, int> waitingPowers = new Dictionary<SupportPowerInstance, int>();
		Dictionary<string, SupportPowerDecision> powerDecisions = new Dictionary<string, SupportPowerDecision>();

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

		public const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

		public readonly World world;
		public Map Map { get { return world.Map; } }
		IBotInfo IBot.Info { get { return this.Info; } }

		public HackyAI(HackyAIInfo info, ActorInitializer init)
		{
			Info = info;
			world = init.world;
			
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
			this.p = p;
			enabled = true;
			playerPower = p.PlayerActor.Trait<PowerManager>();
			supportPowerMngr = p.PlayerActor.Trait<SupportPowerManager>();
			playerResource = p.PlayerActor.Trait<PlayerResources>();

			foreach (var building in Info.BuildingQueues) 
				builders.Add(new BaseBuilder(this, building, p, playerPower, playerResource));
			foreach (var defense in Info.DefenseQueues) 
				builders.Add(new BaseBuilder(this, defense, p, playerPower, playerResource));

			random = new MersenneTwister((int)p.PlayerActor.ActorID);

			resourceTypeIndices = new BitArray(world.TileSet.TerrainInfo.Length); // Big enough
			foreach (var t in Map.Rules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>())
				resourceTypeIndices.Set(world.TileSet.GetTerrainIndex(t.TerrainType), true);
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var unit = buildableThings.ElementAtOrDefault(random.Next(buildableThings.Count()));
			return HasAdequateAirUnits(unit) ? unit : null;
		}

		ActorInfo ChooseUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var myUnits = p.World
				.ActorsWithTrait<IPositionable>()
				.Where(a => a.Actor.Owner == p)
				.Select(a => a.Actor.Info.Name).ToArray();

			foreach (var unit in Info.UnitsToBuild.Shuffle(random))
				if (buildableThings.Any(b => b.Name == unit.Key))
					if (myUnits.Count(a => a == unit.Key) < unit.Value * myUnits.Length)
						if (HasAdequateAirUnits(Map.Rules.Actors[unit.Key]))
							return Map.Rules.Actors[unit.Key];

			return null;
		}

		int CountBuilding(string frac, Player owner)
		{
			return world.ActorsWithTrait<Building>()
				.Count(a => a.Actor.Owner == owner && a.Actor.Info.Name == frac);
		}

		int CountUnits(string unit, Player owner)
		{
			return world.ActorsWithTrait<IPositionable>()
				.Count(a => a.Actor.Owner == owner && a.Actor.Info.Name == unit);
		}

		int? CountBuildingByCommonName(string commonName, Player owner)
		{
			if (!Info.BuildingCommonNames.ContainsKey(commonName))
				return null;

			return world.ActorsWithTrait<Building>()
				.Count(a => a.Actor.Owner == owner && Info.BuildingCommonNames[commonName].Contains(a.Actor.Info.Name));
		}

		public ActorInfo GetBuildingInfoByCommonName(string commonName, Player owner)
		{
			if (commonName == "ConstructionYard")
				return Map.Rules.Actors.Where(k => Info.BuildingCommonNames[commonName].Contains(k.Key)).Random(random).Value;

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

			return Map.Rules.Actors.Where(k => names[commonName].Contains(k.Key)).Random(random).Value;
		}

		public bool HasAdequateFact()
		{
			// Require at least one construction yard, unless we have no vehicles factory (can't build it).
			return CountBuildingByCommonName("ConstructionYard", p) > 0 ||
				CountBuildingByCommonName("VehiclesFactory", p) == 0;
		}

		public bool HasAdequateProc()
		{
			// Require at least one refinery, unless we have no power (can't build it).
			return CountBuildingByCommonName("Refinery", p) > 0 ||
				CountBuildingByCommonName("Power", p) == 0;
		}

		public bool HasMinimumProc()
		{
			// Require at least two refineries, unless we have no power (can't build it)
			// or barracks (higher priority?)
			return CountBuildingByCommonName("Refinery", p) >= 2 ||
				CountBuildingByCommonName("Power", p) == 0 ||
				CountBuildingByCommonName("Barracks", p) == 0;
		}

		// For mods like RA (number of building must match the number of aircraft)
		bool HasAdequateAirUnits(ActorInfo actorInfo)
		{
			if (!actorInfo.Traits.Contains<ReloadsInfo>() && actorInfo.Traits.Contains<LimitedAmmoInfo>() 
				&& actorInfo.Traits.Contains<AircraftInfo>())
			{
				var countOwnAir = CountUnits(actorInfo.Name, p);
				var countBuildings = CountBuilding(actorInfo.Traits.Get<AircraftInfo>().RearmBuildings.FirstOrDefault(), p);
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
					cells = cells.Shuffle(random);

				foreach (var cell in cells)
				{
					if (!world.CanPlaceBuilding(actorType, bi, cell, null))
						continue;

					if (distanceToBaseIsImportant && !bi.IsCloseEnoughToBase(world, p, actorType, cell))
						continue;

					return cell;
				}

				return null;
			};

			switch (type)
			{
				case BuildingType.Defense:

					// Build near the closest enemy structure
					var closestEnemy = world.Actors.Where(a => !a.Destroyed && a.HasTrait<Building>() && p.Stances[a.Owner] == Stance.Enemy)
						.ClosestTo(world.Map.CenterOfCell(defenseCenter));

					var targetCell = closestEnemy != null ? closestEnemy.Location : baseCenter;
					return findPos(defenseCenter, targetCell, Info.MinimumDefenseRadius, Info.MaximumDefenseRadius);

				case BuildingType.Refinery:

					// Try and place the refinery near a resource field
					var nearbyResources = Map.FindTilesInCircle(baseCenter, Info.MaxBaseRadius)
						.Where(a => resourceTypeIndices.Get(Map.GetTerrainIndex(a)))
						.Shuffle(random);

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

			if (ticks % feedbackTime == 0)
				ProductionUnits(self);
			
			AssignRolesToIdleUnits(self);
			SetRallyPointsForNewProductionBuildings(self);
			TryToUseSupportPower(self);

			foreach (var b in builders)
				b.Tick();
		}

		internal Actor ChooseEnemyTarget()
		{
			if (p.WinState != WinState.Undefined)
				return null;

			var liveEnemies = world.Players
				.Where(q => p != q && p.Stances[q] == Stance.Enemy && q.WinState == WinState.Undefined);

			if (!liveEnemies.Any())
				return null;

			var leastLikedEnemies = liveEnemies
				.GroupBy(e => aggro[e].Aggro)
				.MaxByOrDefault(g => g.Key);

			var enemy = (leastLikedEnemies != null) ?
				leastLikedEnemies.Random(random) : liveEnemies.FirstOrDefault();

			// Pick something worth attacking owned by that player
			var target = world.Actors
				.Where(a => a.Owner == enemy && a.HasTrait<IOccupySpace>())
				.ClosestTo(world.Map.CenterOfCell(baseCenter));

			if (target == null)
			{
				/* Assume that "enemy" has nothing. Cool off on attacks. */
				aggro[enemy].Aggro = aggro[enemy].Aggro / 2 - 1;
				Log.Write("debug", "Bot {0} couldn't find target for player {1}", this.p.ClientIndex, enemy.ClientIndex);

				return null;
			}

			// Bump the aggro slightly to avoid changing our mind
			if (leastLikedEnemies.Count() > 1)
				aggro[enemy].Aggro++;

			return target;
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			var allEnemyUnits = world.Actors
				.Where(unit => p.Stances[unit.Owner] == Stance.Enemy && !unit.HasTrait<Husk>() &&
					unit.HasTrait<ITargetable>());

			return allEnemyUnits.ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WRange radius)
		{
			var enemyUnits = world.FindActorsInCircle(pos, radius)
				.Where(unit => p.Stances[unit.Owner] == Stance.Enemy &&
					!unit.HasTrait<Husk>() && unit.HasTrait<ITargetable>()).ToList();

			if (enemyUnits.Count > 0)
				return enemyUnits.ClosestTo(pos);

			return null;
		}

		List<Actor> FindEnemyConstructionYards()
		{
			return world.Actors.Where(a => p.Stances[a.Owner] == Stance.Enemy && !a.IsDead()
				&& a.HasTrait<BaseBuilding>() && !a.HasTrait<Mobile>()).ToList();
		}

		void CleanSquads()
		{
			squads.RemoveAll(s => !s.IsValid);
			foreach (var s in squads)
				s.units.RemoveAll(a => a.IsDead() || a.Owner != p);
		}

		// Use of this function requires that one squad of this type. Hence it is a piece of shit
		Squad GetSquadOfType(SquadType type)
		{
			return squads.FirstOrDefault(s => s.type == type);
		}

		Squad RegisterNewSquad(SquadType type, Actor target = null)
		{
			var ret = new Squad(this, type, target);
			squads.Add(ret);
			return ret;
		}

		int assignRolesTicks = 0;
		int rushTicks = 0;
		int attackForceTicks = 0;

		void AssignRolesToIdleUnits(Actor self)
		{
			CleanSquads();
			activeUnits.RemoveAll(a => a.IsDead() || a.Owner != p); 
			unitsHangingAroundTheBase.RemoveAll(a => a.IsDead() || a.Owner != p);

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
			CreateAttackForce();
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
				world.IssueOrder(new Order("Harvest", a, false));
			}   
		}

		void FindNewUnits(Actor self)
		{
			var newUnits = self.World.ActorsWithTrait<IPositionable>()
				.Where(a => a.Actor.Owner == p && !a.Actor.HasTrait<BaseBuilding>()
					&& !activeUnits.Contains(a.Actor))
				.Select(a => a.Actor);

			foreach (var a in newUnits)
			{
				if (a.HasTrait<Harvester>())
					world.IssueOrder(new Order("Harvest", a, false));
				else
					unitsHangingAroundTheBase.Add(a);

				if (a.HasTrait<Aircraft>() && a.HasTrait<AttackBase>())
				{
					var air = GetSquadOfType(SquadType.Air);
					if (air == null)
						air = RegisterNewSquad(SquadType.Air);

					air.units.Add(a);
				}

				activeUnits.Add(a);
			}  
		}

		void CreateAttackForce()
		{
			// Create an attack force when we have enough units around our base.
			// (don't bother leaving any behind for defense)
			var randomizedSquadSize = Info.SquadSize + random.Next(30);

			if (unitsHangingAroundTheBase.Count >= randomizedSquadSize)
			{
				var attackForce = RegisterNewSquad(SquadType.Assault);

				foreach (var a in unitsHangingAroundTheBase)
					if (!a.HasTrait<Aircraft>())
						attackForce.units.Add(a);

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
				var enemies = world.FindActorsInCircle(b.CenterPosition, WRange.FromCells(Info.RushAttackScanRadius))
					.Where(unit => p.Stances[unit.Owner] == Stance.Enemy && unit.HasTrait<AttackBase>()).ToList();
				
				if (rushFuzzy.CanAttack(ownUnits, enemies))
				{
					var target = enemies.Any() ? enemies.Random(random) : b;
					var rush = GetSquadOfType(SquadType.Rush);
					if (rush == null)
						rush = RegisterNewSquad(SquadType.Rush, target);

					foreach (var a3 in ownUnits)
						rush.units.Add(a3);
		  
					return;
				}
			}
		}

		void ProtectOwn(Actor attacker)
		{
			var protectSq = GetSquadOfType(SquadType.Protection);
			if (protectSq == null)
				protectSq = RegisterNewSquad(SquadType.Protection, attacker);

			if (!protectSq.TargetIsValid)
				protectSq.Target = attacker;

			if (!protectSq.IsValid)
			{
				var ownUnits = world.FindActorsInCircle(world.Map.CenterOfCell(baseCenter), WRange.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit => unit.Owner == p && !unit.HasTrait<Building>()
						&& unit.HasTrait<AttackBase>()).ToList();

				foreach (var a in ownUnits)
					protectSq.units.Add(a);
			}
		}

		bool IsRallyPointValid(CPos x, BuildingInfo info)
		{
			return info != null && world.IsCellBuildable(x, info);
		}

		void SetRallyPointsForNewProductionBuildings(Actor self)
		{
			var buildings = self.World.ActorsWithTrait<RallyPoint>()
				.Where(rp => rp.Actor.Owner == p &&
					!IsRallyPointValid(rp.Trait.Location, rp.Actor.Info.Traits.GetOrDefault<BuildingInfo>())).ToArray();

			foreach (var a in buildings)
				world.IssueOrder(new Order("SetRallyPoint", a.Actor, false) { TargetLocation = ChooseRallyLocationNear(a.Actor), SuppressVisualFeedback = true });
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

			return possibleRallyPoints.Random(random);
		}

		void InitializeBase(Actor self)
		{
			// Find and deploy our mcv
			var mcv = self.World.Actors
				.FirstOrDefault(a => a.Owner == p && a.HasTrait<BaseBuilding>());

			if (mcv != null)
			{
				baseCenter = mcv.Location;
				defenseCenter = baseCenter;

				// Don't transform the mcv if it is a fact
				// HACK: This needs to query against MCVs directly
				if (mcv.HasTrait<Mobile>())
					world.IssueOrder(new Order("DeployTransform", mcv, false));
			}
			else
				BotDebug("AI: Can't find BaseBuildUnit.");
		}

		// Find any newly constructed MCVs and deploy them at a sensible
		// backup location within the main base.
		void FindAndDeployBackupMcv(Actor self)
		{
			// HACK: This needs to query against MCVs directly
			var mcvs = self.World.Actors.Where(a => a.Owner == p && a.HasTrait<BaseBuilding>() && a.HasTrait<Mobile>());
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

				world.IssueOrder(new Order("Move", mcv, true) { TargetLocation = desiredLocation.Value });
				world.IssueOrder(new Order("DeployTransform", mcv, true));
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
				var isDelayed = (waitingPowers[sp] > 0);
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
						BotDebug("AI: {1} can't find suitable coarse attack location for support power {0}. Delaying rescan.", sp.Info.OrderName, p.PlayerName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(this);

						continue;
					}

					// Found a target location, check for precise target
					attackLocation = FindFineAttackLocationToSupportPower(sp, (CPos)attackLocation);
					if (attackLocation == null)
					{
						BotDebug("AI: {1} can't find suitable final attack location for support power {0}. Delaying rescan.", sp.Info.OrderName, p.PlayerName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(this);

						continue;
					}

					// Valid target found, delay by a few ticks to avoid rescanning before power fires via order
					BotDebug("AI: {2} found new target location {0} for support power {1}.", attackLocation, sp.Info.OrderName, p.PlayerName);
					waitingPowers[sp] += 10;
					world.IssueOrder(new Order(sp.Info.OrderName, supportPowerMngr.self, false) { TargetLocation = attackLocation.Value, SuppressVisualFeedback = true });
				}
			}
		}

		///<summary>Scans the map in chunks, evaluating all actors in each.</summary>
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
			for (var i = 0; i < world.Map.MapSize.X; i += checkRadius)
			{
				for (var j = 0; j < world.Map.MapSize.Y; j += checkRadius)
				{
					var consideredAttractiveness = 0;

					var tl = world.Map.CenterOfCell(new CPos(i, j));
					var br = world.Map.CenterOfCell(new CPos(i + checkRadius, j + checkRadius));
					var targets = world.ActorMap.ActorsInBox(tl, br);
					
					consideredAttractiveness = powerDecision.GetAttractiveness(targets, p);
					if (consideredAttractiveness <= bestAttractiveness || consideredAttractiveness < powerDecision.MinimumAttractiveness)
						continue;

					bestAttractiveness = consideredAttractiveness;
					bestLocation = new CPos(i, j);
				}
			}

			return bestLocation;
		}

		///<summary>Detail scans an area, evaluating positions.</summary>
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
			for (var i = (0 - extendedRange); i <= (checkRadius + extendedRange); i += fineCheck)
			{
				var x = checkPos.X + i;

				for (var j = (0 - extendedRange); j <= (checkRadius + extendedRange); j += fineCheck)
				{
					var y = checkPos.Y + j;
					var pos = world.Map.CenterOfCell(new CPos(x, y));
					var consideredAttractiveness = 0;
					consideredAttractiveness += powerDecision.GetAttractiveness(pos, p);

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
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p && a.Trait.Info.Type == category && a.Trait.Enabled)
				.Select(a => a.Trait);
		}

		void ProductionUnits(Actor self)
		{
			// Stop building until economy is restored
			if (!HasAdequateProc())
				return;

			// No construction yards - Build a new MCV
			if (!HasAdequateFact() && !self.World.Actors.Any(a => a.Owner == p && a.HasTrait<BaseBuilding>() && a.HasTrait<Mobile>()))
				BuildUnit("Vehicle", GetUnitInfoByCommonName("Mcv", p).Name);

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

			if (unit != null && Info.UnitsToBuild.Any(u => u.Key == unit.Name))
				world.IssueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
		}

		void BuildUnit(string category, string name)
		{
			var queue = FindQueues(category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null)
				return;

			if (Map.Rules.Actors[name] != null)
				world.IssueOrder(Order.StartProduction(queue.Actor, name, 1));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!enabled)
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
					world.IssueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, false)
						{ TargetActor = self });
				}
			}

			if (e.Attacker.Destroyed)
				return;

			if (!e.Attacker.HasTrait<ITargetable>())
				return;

			if (e.Attacker != null && e.Damage > 0)
				aggro[e.Attacker.Owner].Aggro += e.Damage;

			// Protected harvesters or building
			if ((self.HasTrait<Harvester>() || self.HasTrait<Building>()) &&
				p.Stances[e.Attacker.Owner] == Stance.Enemy)
			{
				defenseCenter = e.Attacker.Location;
				ProtectOwn(e.Attacker);
			}
		}
	}
}
