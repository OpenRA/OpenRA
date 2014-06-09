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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.RA.AI
{
	public sealed class HackyAIInfo : IBotInfo, ITraitInfo
	{
		public readonly string Name = "Unnamed Bot";
		public readonly int SquadSize = 8;

		public readonly int AssignRolesInterval = 20;
		public readonly int RushInterval = 600;
		public readonly int AttackForceInterval = 30;

		[Desc("By what factor should power output exceed power consumption.")]
		public readonly float ExcessPowerFactor = 1.2f;
		[Desc("By what minimum amount should power output exceed power consumption.")]
		public readonly int MinimumExcessPower = 50;
		[Desc("Only produce units as long as there are less than this amount of units idling inside the base.")]
		public readonly int IdleBaseUnitsMaximum = 12;
		[Desc("Radius in cells around enemy BaseBuilder (Construction Yard) where AI scans for targets to rush.")]
		public readonly int RushAttackScanRadius = 15;
		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;
		[Desc("Radius in cells around a factory scanned for rally points by the AI.")]
		public readonly int RallyPointScanRadius = 8;

		// Temporary hack to maintain previous rallypoint behavior.
		public readonly string RallypointTestBuilding = "fact";
		public readonly string[] UnitQueues = { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };
		public readonly bool ShouldRepairBuildings = true;

		string IBotInfo.Name { get { return this.Name; } }

		[FieldLoader.LoadUsing("LoadUnits")]
		public readonly Dictionary<string, float> UnitsToBuild = null;

		[FieldLoader.LoadUsing("LoadBuildings")]
		public readonly Dictionary<string, float> BuildingFractions = null;

		[FieldLoader.LoadUsing("LoadUnitsCommonNames")]
		public readonly Dictionary<string, string[]> UnitsCommonNames = null;

		[FieldLoader.LoadUsing("LoadBuildingsCommonNames")]
		public readonly Dictionary<string, string[]> BuildingCommonNames = null;

		[FieldLoader.LoadUsing("LoadBuildingLimits")]
		public readonly Dictionary<string, int> BuildingLimits = null;

		static object LoadList<T>(MiniYaml y, string field)
		{
			return y.NodesDict.ContainsKey(field)
				? y.NodesDict[field].NodesDict.ToDictionary(
					a => a.Key,
					a => FieldLoader.GetValue<T>(field, a.Value.Value))
				: new Dictionary<string, T>();
		}

		static object LoadUnits(MiniYaml y) { return LoadList<float>(y, "UnitsToBuild"); }
		static object LoadBuildings(MiniYaml y) { return LoadList<float>(y, "BuildingFractions"); }

		static object LoadUnitsCommonNames(MiniYaml y) { return LoadList<string[]>(y, "UnitsCommonNames"); }
		static object LoadBuildingsCommonNames(MiniYaml y) { return LoadList<string[]>(y, "BuildingCommonNames"); }

		static object LoadBuildingLimits(MiniYaml y) { return LoadList<int>(y, "BuildingLimits"); }

		public object Create(ActorInitializer init) { return new HackyAI(this, init); }
	}

	public class Enemy { public int Aggro; }

	public enum BuildingType { Building, Defense, Refinery }

	public sealed class HackyAI : ITick, IBot, INotifyDamage
	{
		bool enabled;
		public int ticks;
		public Player p;
		public MersenneTwister random;
		public CPos baseCenter;
		PowerManager playerPower;
		SupportPowerManager supportPowerMngr;
		PlayerResources playerResource;
		BuildingInfo rallypointTestBuilding;
		internal readonly HackyAIInfo Info;

		string[] resourceTypes;

		RushFuzzy rushFuzzy = new RushFuzzy();

		Cache<Player, Enemy> aggro = new Cache<Player, Enemy>(_ => new Enemy());
		BaseBuilder[] builders;

		List<Squad> squads = new List<Squad>();
		List<Actor> unitsHangingAroundTheBase = new List<Actor>();

		// Units that the ai already knows about. Any unit not on this list needs to be given a role.
		List<Actor> activeUnits = new List<Actor>();

		const int MaxBaseDistance = 40;
		public const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

		public readonly World world;
		public Map Map { get { return world.Map; } }
		IBotInfo IBot.Info { get { return this.Info; } }

		public HackyAI(HackyAIInfo info, ActorInitializer init)
		{
			Info = info;
			world = init.world;

			// Temporary hack.
			rallypointTestBuilding = Map.Rules.Actors[Info.RallypointTestBuilding].Traits.Get<BuildingInfo>();
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
			builders = new BaseBuilder[] {
				new BaseBuilder(this, "Building", q => ChooseBuildingToBuild(q, false)),
				new BaseBuilder(this, "Defense", q => ChooseBuildingToBuild(q, true))
			};

			random = new MersenneTwister((int)p.PlayerActor.ActorID);

			resourceTypes = Map.Rules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>()
				.Select(t => t.TerrainType).ToArray();
		}

		static int GetPowerProvidedBy(ActorInfo building)
		{
			var bi = building.Traits.GetOrDefault<BuildingInfo>();
			return bi != null ? bi.Power : 0;
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

			foreach (var unit in Info.UnitsToBuild)
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

		ActorInfo GetBuildingInfoByCommonName(string commonName, Player owner)
		{
			if (commonName == "ConstructionYard")
				return Map.Rules.Actors.Where(k => Info.BuildingCommonNames[commonName].Contains(k.Key)).Random(random).Value;

			return GetInfoByCommonName(Info.BuildingCommonNames, commonName, owner);
		}

		ActorInfo GetUnitInfoByCommonName(string commonName, Player owner)
		{
			return GetInfoByCommonName(Info.UnitsCommonNames, commonName, owner);
		}

		ActorInfo GetInfoByCommonName(Dictionary<string, string[]> names, string commonName, Player owner)
		{
			if (!names.Any() || !names.ContainsKey(commonName))
				return null;

			return Map.Rules.Actors.Where(k => names[commonName].Contains(k.Key) &&
				k.Value.Traits.Get<BuildableInfo>().Owner.Contains(owner.Country.Race)).Random(random).Value;
		}

		bool HasAdequatePower()
		{
			// note: CNC `fact` provides a small amount of power. don't get jammed because of that.
			return playerPower.PowerProvided > Info.MinimumExcessPower &&
				playerPower.PowerProvided > playerPower.PowerDrained * Info.ExcessPowerFactor;
		}

		bool HasAdequateFact()
		{
			// Require at least one construction yard, unless we have no vehicles factory (can't build it).
			return CountBuildingByCommonName("ConstructionYard", p) > 0 ||
				CountBuildingByCommonName("VehiclesFactory", p) == 0;
		}

		bool HasAdequateProc()
		{
			// Require at least one refinery, unless we have no power (can't build it).
			return CountBuildingByCommonName("Refinery", p) > 0 ||
				CountBuildingByCommonName("Power", p) == 0;
		}

		bool HasMinimumProc()
		{
			// Require at least two refineries, unless we have no power (can't build it)
			// or barracks (higher priority?)
			return CountBuildingByCommonName("Refinery", p) >= 2 ||
				CountBuildingByCommonName("Power", p) == 0 ||
				CountBuildingByCommonName("Barracks", p) == 0;
		}

		bool HasAdequateNumber(string frac, Player owner)
		{
			if (Info.BuildingLimits.ContainsKey(frac))
				return CountBuilding(frac, owner) < Info.BuildingLimits[frac];

			return true;
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

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue, bool isDefense)
		{
			var buildableThings = queue.BuildableItems();

			if (!isDefense)
			{
				// Try to maintain 20% excess power
				if (!HasAdequatePower())
					return buildableThings.Where(a => GetPowerProvidedBy(a) > 0)
						.MaxByOrDefault(a => GetPowerProvidedBy(a));

				if (playerResource.AlertSilo)
					return GetBuildingInfoByCommonName("Silo", p);

				if (!HasAdequateProc() || !HasMinimumProc())
					return GetBuildingInfoByCommonName("Refinery", p);
			}

			var myBuildings = p.World
				.ActorsWithTrait<Building>()
				.Where(a => a.Actor.Owner == p)
				.Select(a => a.Actor.Info.Name)
				.ToArray();

			foreach (var frac in Info.BuildingFractions)
				if (buildableThings.Any(b => b.Name == frac.Key))
					if (myBuildings.Count(a => a == frac.Key) < frac.Value * myBuildings.Length && HasAdequateNumber(frac.Key, p) &&
						playerPower.ExcessPower >= Map.Rules.Actors[frac.Key].Traits.Get<BuildingInfo>().Power)
						return Map.Rules.Actors[frac.Key];

			return null;
		}

		bool NoBuildingsUnder(IEnumerable<CPos> cells)
		{
			var bi = world.WorldActor.Trait<BuildingInfluence>();
			return cells.All(c => bi.GetBuildingAt(c) == null);
		}

		CPos defenseCenter;
		public CPos? ChooseBuildLocation(string actorType, BuildingType type)
		{
			return ChooseBuildLocation(actorType, true, MaxBaseDistance, type);
		}

		public CPos? ChooseBuildLocation(string actorType, bool distanceToBaseIsImportant, int maxBaseDistance, BuildingType type)
		{
			var bi = Map.Rules.Actors[actorType].Traits.GetOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			Func<WPos, CPos, CPos?> findPos = (pos, center) =>
			{
				for (var k = MaxBaseDistance; k >= 0; k--)
				{
					var tlist = world.FindTilesInCircle(center, k)
						.OrderBy(a => (a.CenterPosition - pos).LengthSquared);

					foreach (var t in tlist)
						if (world.CanPlaceBuilding(actorType, bi, t, null))
							if (bi.IsCloseEnoughToBase(world, p, actorType, t))
								if (NoBuildingsUnder(Util.ExpandFootprint(FootprintUtils.Tiles(Map.Rules, actorType, bi, t), false)))
									return t;
				}

				return null;
			};

			switch (type)
			{
				case BuildingType.Defense:
					Actor enemyBase = FindEnemyBuildingClosestToPos(baseCenter.CenterPosition);
					return enemyBase != null ? findPos(enemyBase.CenterPosition, defenseCenter) : null;

				case BuildingType.Refinery:
					var tilesPos = world.FindTilesInCircle(baseCenter, MaxBaseDistance)
						.Where(a => resourceTypes.Contains(world.GetTerrainType(new CPos(a.X, a.Y))));
					if (tilesPos.Any())
					{
						var pos = tilesPos.MinBy(a => (a.CenterPosition - baseCenter.CenterPosition).LengthSquared);
						return findPos(pos.CenterPosition, baseCenter);
					}
					return null;

				case BuildingType.Building:
					for (var k = 0; k < maxBaseDistance; k++)
					{
						foreach (var t in world.FindTilesInCircle(baseCenter, k))
						{
							if (world.CanPlaceBuilding(actorType, bi, t, null))
							{
								if (distanceToBaseIsImportant && !bi.IsCloseEnoughToBase(world, p, actorType, t))
									continue;

								if (NoBuildingsUnder(Util.ExpandFootprint(FootprintUtils.Tiles(Map.Rules, actorType, bi, t), false)))
									return t;
							}
						}
					}

					break;
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
				.ClosestTo(baseCenter.CenterPosition);

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

		Actor FindEnemyBuildingClosestToPos(WPos pos)
		{
			var closestBuilding = world.Actors.Where(a => p.Stances[a.Owner] == Stance.Enemy
			   && !a.Destroyed && a.HasTrait<Building>()).ClosestTo(pos);

			return closestBuilding;
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

		Squad RegisterNewSquad(SquadType type, Actor target)
		{
			var ret = new Squad(this, type, target);
			squads.Add(ret);
			return ret;
		}
		Squad RegisterNewSquad(SquadType type)
		{
			return RegisterNewSquad(type, null);
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
				BotDebug("AI: Found a newly built unit");
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
				var ownUnits = world.FindActorsInCircle(baseCenter.CenterPosition, WRange.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit => unit.Owner == p && !unit.HasTrait<Building>()
						&& unit.HasTrait<AttackBase>()).ToList();

				foreach (var a in ownUnits)
					protectSq.units.Add(a);
			}
		}

		bool IsRallyPointValid(CPos x)
		{
			// This is actually WRONG as soon as HackyAI is building units with
			// a variety of movement capabilities. (has always been wrong)
			return world.IsCellBuildable(x, rallypointTestBuilding);
		}

		void SetRallyPointsForNewProductionBuildings(Actor self)
		{
			var buildings = self.World.ActorsWithTrait<RallyPoint>()
				.Where(rp => rp.Actor.Owner == p &&
					!IsRallyPointValid(rp.Trait.rallyPoint)).ToArray();

			if (buildings.Length > 0)
				BotDebug("Bot {0} needs to find rallypoints for {1} buildings.",
					p.PlayerName, buildings.Length);

			foreach (var a in buildings)
				world.IssueOrder(new Order("SetRallyPoint", a.Actor, false) { TargetLocation = ChooseRallyLocationNear(a.Actor.Location) });
		}

		// Won't work for shipyards...
		CPos ChooseRallyLocationNear(CPos startPos)
		{
			var possibleRallyPoints = world.FindTilesInCircle(startPos, Info.RallyPointScanRadius)
				.Where(IsRallyPointValid);

			if (!possibleRallyPoints.Any())
			{
				BotDebug("Bot Bug: No possible rallypoint near {0}", startPos);
				return startPos;
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
			var maxBaseDistance = Math.Max(world.Map.MapSize.X, world.Map.MapSize.Y);

			// HACK: Assumes all MCVs deploy into the same construction yard footprint
			var mcvInfo = GetUnitInfoByCommonName("Mcv", p);
			if (mcvInfo == null)
				return;

			var factType = mcvInfo.Traits.Get<TransformsInfo>().IntoActor;

			// HACK: This needs to query against MCVs directly
			var mcvs = self.World.Actors.Where(a => a.Owner == p && a.HasTrait<BaseBuilding>() && a.HasTrait<Mobile>());
			if (!mcvs.Any())
				return;

			foreach (var mcv in mcvs)
			{
				if (mcv.IsMoving())
					continue;

				var desiredLocation = ChooseBuildLocation(factType, false, maxBaseDistance, BuildingType.Building);
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
				if (sp.Ready)
				{
					var attackLocation = FindAttackLocationToSupportPower(5);
					if (attackLocation == null)
						return;

					world.IssueOrder(new Order(sp.Info.OrderName, supportPowerMngr.self, false) { TargetLocation = attackLocation.Value });
				}
			}
		}

		CPos? FindAttackLocationToSupportPower(int radiusOfPower)
		{
			CPos? resLoc = null;
			var countUnits = 0;

			var x = (world.Map.MapSize.X % radiusOfPower) == 0 ? world.Map.MapSize.X : world.Map.MapSize.X + radiusOfPower;
			var y = (world.Map.MapSize.Y % radiusOfPower) == 0 ? world.Map.MapSize.Y : world.Map.MapSize.Y + radiusOfPower;

			for (int i = 0; i < x; i += radiusOfPower * 2)
			{
				for (int j = 0; j < y; j += radiusOfPower * 2)
				{
					var pos = new CPos(i, j);
					var targets = world.FindActorsInCircle(pos.CenterPosition, WRange.FromCells(radiusOfPower)).ToList();
					var enemies = targets.Where(unit => p.Stances[unit.Owner] == Stance.Enemy).ToList();
					var ally = targets.Where(unit => p.Stances[unit.Owner] == Stance.Ally || unit.Owner == p).ToList();

					if (enemies.Count < ally.Count || !enemies.Any())
						continue;

					if (enemies.Count > countUnits)
					{
						countUnits = enemies.Count;
						resLoc = enemies.Random(random).Location;
					}
				}
			}

			return resLoc;
		}

		internal IEnumerable<ProductionQueue> FindQueues(string category)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p && a.Trait.Info.Type == category)
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
				world.IssueOrder(Order.StartProduction(queue.self, unit.Name, 1));
		}

		void BuildUnit(string category, string name)
		{
			var queue = FindQueues(category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null)
				return;

			if (Map.Rules.Actors[name] != null)
				world.IssueOrder(Order.StartProduction(queue.self, name, 1));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// TODO: Surely we want to do this even if their destroyer died?
			if (!enabled || e.Attacker.Destroyed)
				return;

			if (!e.Attacker.HasTrait<ITargetable>())
				return;

			if (Info.ShouldRepairBuildings && self.HasTrait<RepairableBuilding>())
			{
				if (e.DamageState > DamageState.Light && e.PreviousDamageState <= DamageState.Light)
				{
					BotDebug("Bot noticed damage {0} {1}->{2}, repairing.",
						self, e.PreviousDamageState, e.DamageState);
					world.IssueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, false)
						{ TargetActor = self });
				}
			}

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
