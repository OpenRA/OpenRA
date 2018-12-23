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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	public sealed class HackyAIInfo : IBotInfo, ITraitInfo
	{
		public class UnitCategories
		{
			public readonly HashSet<string> Mcv = new HashSet<string>();
			public readonly HashSet<string> NavalUnits = new HashSet<string>();
			public readonly HashSet<string> ExcludeFromSquads = new HashSet<string>();
		}

		// TODO: Move this to SquadManagerBotModule later
		public class BuildingCategories
		{
			public readonly HashSet<string> ConstructionYard = new HashSet<string>();
			public readonly HashSet<string> VehiclesFactory = new HashSet<string>();
			public readonly HashSet<string> Refinery = new HashSet<string>();
			public readonly HashSet<string> Power = new HashSet<string>();
			public readonly HashSet<string> Barracks = new HashSet<string>();
			public readonly HashSet<string> Production = new HashSet<string>();
			public readonly HashSet<string> NavalProduction = new HashSet<string>();
			public readonly HashSet<string> Silo = new HashSet<string>();
		}

		[FieldLoader.Require]
		[Desc("Internal id for this bot.")]
		public readonly string Type = null;

		[Desc("Human-readable name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

		[Desc("Random number of up to this many units is added to squad size when creating an attack squad.")]
		public readonly int SquadSizeRandomBonus = 30;

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

		[Desc("Only produce units as long as there are less than this amount of units idling inside the base.")]
		public readonly int IdleBaseUnitsMaximum = 12;

		[Desc("Radius in cells around enemy BaseBuilder (Construction Yard) where AI scans for targets to rush.")]
		public readonly int RushAttackScanRadius = 15;

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

		[Desc("Minimum distance in cells from center of the base when checking for MCV deployment location.")]
		public readonly int MinBaseRadius = 2;

		[Desc("Maximum distance in cells from center of the base when checking for MCV deployment location.",
			"Only applies if RestrictMCVDeploymentFallbackToBase is enabled and there's at least one construction yard.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Radius in cells that squads should scan for enemies around their position while idle.")]
		public readonly int IdleScanRadius = 10;

		[Desc("Radius in cells that squads should scan for danger around their position to make flee decisions.")]
		public readonly int DangerScanRadius = 10;

		[Desc("Radius in cells that attack squads should scan for enemies around their position when trying to attack.")]
		public readonly int AttackScanRadius = 12;

		[Desc("Radius in cells that protecting squads should scan for enemies around their position.")]
		public readonly int ProtectionScanRadius = 8;

		[Desc("Should deployment of additional MCVs be restricted to MaxBaseRadius if explicit deploy locations are missing or occupied?")]
		public readonly bool RestrictMCVDeploymentFallbackToBase = true;

		[Desc("Production queues AI uses for producing units.")]
		public readonly HashSet<string> UnitQueues = new HashSet<string> { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };

		[Desc("What units to the AI should build.", "What % of the total army must be this type of unit.")]
		public readonly Dictionary<string, float> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("Tells the AI what unit types fall under the same common name. Supported entries are Mcv and ExcludeFromSquads.")]
		[FieldLoader.LoadUsing("LoadUnitCategories", true)]
		public readonly UnitCategories UnitsCommonNames;

		// TODO: Move this to SquadManagerBotModule later
		[Desc("Tells the AI what building types fall under the same common name.",
			"Possible keys are ConstructionYard, Power, Refinery, Silo, Barracks, Production, VehiclesFactory, NavalProduction.")]
		[FieldLoader.LoadUsing("LoadBuildingCategories", true)]
		public readonly BuildingCategories BuildingCommonNames;

		static object LoadUnitCategories(MiniYaml yaml)
		{
			var categories = yaml.Nodes.First(n => n.Key == "UnitsCommonNames");
			return FieldLoader.Load<UnitCategories>(categories.Value);
		}

		// TODO: Move this to SquadManagerBotModule later
		static object LoadBuildingCategories(MiniYaml yaml)
		{
			var categories = yaml.Nodes.First(n => n.Key == "BuildingCommonNames");
			return FieldLoader.Load<BuildingCategories>(categories.Value);
		}

		string IBotInfo.Type { get { return Type; } }

		string IBotInfo.Name { get { return Name; } }

		public object Create(ActorInitializer init) { return new HackyAI(this, init); }
	}

	public sealed class HackyAI : ITick, IBot, INotifyDamage, IBotPositionsUpdated
	{
		// DEPRECATED: Modules should use World.LocalRandom.
		public MersenneTwister Random { get; private set; }
		public readonly HackyAIInfo Info;

		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = World.Actors.Where(a => a.Owner == Player &&
				Info.BuildingCommonNames.ConstructionYard.Contains(a.Info.Name))
				.RandomOrDefault(Random);

			return randomConstructionYard != null ? randomConstructionYard.Location : initialBaseCenter;
		}

		public bool IsEnabled;
		public List<Squad> Squads = new List<Squad>();
		public Player Player { get; private set; }

		readonly Queue<Order> orders = new Queue<Order>();

		readonly Func<Actor, bool> isEnemyUnit;
		readonly Predicate<Actor> unitCannotBeOrdered;

		IBotTick[] tickModules;
		IBotRespondToAttack[] attackResponseModules;
		IBotPositionsUpdated[] positionsUpdatedModules;

		CPos initialBaseCenter;
		int ticks;

		List<Actor> unitsHangingAroundTheBase = new List<Actor>();

		// Units that the ai already knows about. Any unit not on this list needs to be given a role.
		List<Actor> activeUnits = new List<Actor>();

		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		public readonly World World;
		public Map Map { get { return World.Map; } }
		IBotInfo IBot.Info { get { return Info; } }

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int minAttackForceDelayTicks;

		public HackyAI(HackyAIInfo info, ActorInitializer init)
		{
			Info = info;
			World = init.World;

			if (World.Type == WorldType.Editor)
				return;

			isEnemyUnit = unit =>
				Player.Stances[unit.Owner] == Stance.Enemy
					&& !unit.Info.HasTraitInfo<HuskInfo>()
					&& unit.Info.HasTraitInfo<ITargetableInfo>();

			unitCannotBeOrdered = a => a.Owner != Player || a.IsDead || !a.IsInWorld;
		}

		// Called by the host's player creation code
		public void Activate(Player p)
		{
			Player = p;
			IsEnabled = true;
			tickModules = p.PlayerActor.TraitsImplementing<IBotTick>().ToArray();
			attackResponseModules = p.PlayerActor.TraitsImplementing<IBotRespondToAttack>().ToArray();
			positionsUpdatedModules = p.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();

			Random = new MersenneTwister(Game.CosmeticRandom.Next());

			// Avoid all AIs trying to rush in the same tick, randomize their initial rush a little.
			var smallFractionOfRushInterval = Info.RushInterval / 20;
			rushTicks = Random.Next(Info.RushInterval - smallFractionOfRushInterval, Info.RushInterval + smallFractionOfRushInterval);

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = Random.Next(0, Info.AssignRolesInterval);
			attackForceTicks = Random.Next(0, Info.AttackForceInterval);
			minAttackForceDelayTicks = Random.Next(0, Info.MinimumAttackForceDelay);
		}

		void IBot.QueueOrder(Order order)
		{
			orders.Enqueue(order);
		}

		// DEPRECATED: Modules should use IBot.QueueOrder instead
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
				.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player)
				.Select(a => a.Info.Name).ToList();

			foreach (var unit in Info.UnitsToBuild.Shuffle(Random))
				if (buildableThings.Any(b => b.Name == unit.Key))
					if (myUnits.Count(a => a == unit.Key) < unit.Value * myUnits.Count)
						if (HasAdequateAirUnitReloadBuildings(Map.Rules.Actors[unit.Key]))
							return Map.Rules.Actors[unit.Key];

			return null;
		}

		bool HasAdequateConstructionYardCount
		{
			get
			{
				// Require at least one construction yard, unless we have no vehicles factory (can't build it).
				return AIUtils.CountBuildingByCommonName(Info.BuildingCommonNames.ConstructionYard, Player) > 0 ||
					AIUtils.CountBuildingByCommonName(Info.BuildingCommonNames.VehiclesFactory, Player) == 0;
			}
		}

		bool HasAdequateRefineryCount
		{
			get
			{
				// Require at least one refinery, unless we can't build it.
				return AIUtils.CountBuildingByCommonName(Info.BuildingCommonNames.Refinery, Player) > 0 ||
					AIUtils.CountBuildingByCommonName(Info.BuildingCommonNames.Power, Player) == 0 ||
					AIUtils.CountBuildingByCommonName(Info.BuildingCommonNames.ConstructionYard, Player) == 0;
			}
		}

		// For mods like RA (number of RearmActors must match the number of aircraft)
		bool HasAdequateAirUnitReloadBuildings(ActorInfo actorInfo)
		{
			var aircraftInfo = actorInfo.TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo == null)
				return true;

			// If actor isn't Rearmable, it doesn't need a RearmActor to reload
			var rearmableInfo = actorInfo.TraitInfoOrDefault<RearmableInfo>();
			if (rearmableInfo == null)
				return true;

			var countOwnAir = AIUtils.CountActorsWithTrait<IPositionable>(actorInfo.Name, Player);
			var countBuildings = rearmableInfo.RearmActors.Sum(b => AIUtils.CountActorsWithTrait<Building>(b, Player));
			if (countOwnAir >= countBuildings)
				return false;

			return true;
		}

		CPos? ChooseMcvDeployLocation(string actorType, bool distanceToBaseIsImportant)
		{
			var actorInfo = World.Map.Rules.Actors[actorType];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			// Find the buildable cell that is closest to pos and centered around center
			Func<CPos, CPos, int, int, CPos?> findPos = (center, target, minRange, maxRange) =>
			{
				var cells = World.Map.FindTilesInAnnulus(center, minRange, maxRange);

				// Sort by distance to target if we have one
				if (center != target)
					cells = cells.OrderBy(c => (c - target).LengthSquared);
				else
					cells = cells.Shuffle(World.LocalRandom);

				foreach (var cell in cells)
				{
					if (!World.CanPlaceBuilding(cell, actorInfo, bi, null))
						continue;

					if (distanceToBaseIsImportant && !bi.IsCloseEnoughToBase(World, Player, actorInfo, cell))
						continue;

					return cell;
				}

				return null;
			};

			var baseCenter = GetRandomBaseCenter();

			return findPos(baseCenter, baseCenter, Info.MinBaseRadius,
				distanceToBaseIsImportant ? Info.MaxBaseRadius : World.Map.Grid.MaximumTileSearchRange);
		}

		void ITick.Tick(Actor self)
		{
			if (!IsEnabled)
				return;

			ticks++;

			if (ticks == 1)
				InitializeBase(self, false);

			if (ticks % FeedbackTime == 0)
				ProductionUnits(self);

			AssignRolesToIdleUnits(self);

			using (new PerfSample("bot_tick"))
			{
				Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, World, () =>
				{
					foreach (var t in tickModules)
						if (t.IsTraitEnabled())
							t.BotTick(this);
				});
			}

			var ordersToIssueThisTick = Math.Min((orders.Count + Info.MinOrderQuotientPerTick - 1) / Info.MinOrderQuotientPerTick, orders.Count);
			for (var i = 0; i < ordersToIssueThisTick; i++)
				World.IssueOrder(orders.Dequeue());
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			return World.Actors.Where(isEnemyUnit).ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return World.FindActorsInCircle(pos, radius).Where(isEnemyUnit).ClosestTo(pos);
		}

		void CleanSquads()
		{
			Squads.RemoveAll(s => !s.IsValid);
			foreach (var s in Squads)
				s.Units.RemoveAll(unitCannotBeOrdered);
		}

		// Use of this function requires that one squad of this type. Hence it is a piece of shit
		Squad GetSquadOfType(SquadType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		Squad RegisterNewSquad(SquadType type, Actor target = null)
		{
			var ret = new Squad(this, type, target);
			Squads.Add(ret);
			return ret;
		}

		void AssignRolesToIdleUnits(Actor self)
		{
			CleanSquads();

			activeUnits.RemoveAll(unitCannotBeOrdered);
			unitsHangingAroundTheBase.RemoveAll(unitCannotBeOrdered);

			if (--rushTicks <= 0)
			{
				rushTicks = Info.RushInterval;
				TryToRushAttack();
			}

			if (--attackForceTicks <= 0)
			{
				attackForceTicks = Info.AttackForceInterval;
				foreach (var s in Squads)
					s.Update();
			}

			if (--assignRolesTicks <= 0)
			{
				assignRolesTicks = Info.AssignRolesInterval;
				FindNewUnits(self);
				InitializeBase(self, true);
			}

			if (--minAttackForceDelayTicks <= 0)
			{
				minAttackForceDelayTicks = Info.MinimumAttackForceDelay;
				CreateAttackForce();
			}
		}

		void FindNewUnits(Actor self)
		{
			var newUnits = self.World.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player && !activeUnits.Contains(a));

			foreach (var a in newUnits)
			{
				if (Info.UnitsCommonNames.Mcv.Contains(a.Info.Name) || Info.UnitsCommonNames.ExcludeFromSquads.Contains(a.Info.Name))
					continue;

				unitsHangingAroundTheBase.Add(a);

				if (a.Info.HasTraitInfo<AircraftInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>())
				{
					var air = GetSquadOfType(SquadType.Air);
					if (air == null)
						air = RegisterNewSquad(SquadType.Air);

					air.Units.Add(a);
				}
				else if (Info.UnitsCommonNames.NavalUnits.Contains(a.Info.Name))
				{
					var ships = GetSquadOfType(SquadType.Naval);
					if (ships == null)
						ships = RegisterNewSquad(SquadType.Naval);

					ships.Units.Add(a);
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
					if (!a.Info.HasTraitInfo<AircraftInfo>())
						attackForce.Units.Add(a);

				unitsHangingAroundTheBase.Clear();
			}
		}

		void TryToRushAttack()
		{
			var allEnemyBaseBuilder = AIUtils.FindEnemiesByCommonName(Info.BuildingCommonNames.ConstructionYard, Player);
			var ownUnits = activeUnits
				.Where(unit => unit.IsIdle && unit.Info.HasTraitInfo<AttackBaseInfo>()
					&& !unit.Info.HasTraitInfo<AircraftInfo>() && !unit.Info.HasTraitInfo<HarvesterInfo>()).ToList();

			if (!allEnemyBaseBuilder.Any() || (ownUnits.Count < Info.SquadSize))
				return;

			foreach (var b in allEnemyBaseBuilder)
			{
				var enemies = World.FindActorsInCircle(b.CenterPosition, WDist.FromCells(Info.RushAttackScanRadius))
					.Where(unit => Player.Stances[unit.Owner] == Stance.Enemy && unit.Info.HasTraitInfo<AttackBaseInfo>()).ToList();

				if (AttackOrFleeFuzzy.Rush.CanAttack(ownUnits, enemies))
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
					.Where(unit => unit.Owner == Player && !unit.Info.HasTraitInfo<BuildingInfo>() && !unit.Info.HasTraitInfo<HarvesterInfo>()
						&& unit.Info.HasTraitInfo<AttackBaseInfo>());

				foreach (var a in ownUnits)
					protectSq.Units.Add(a);
			}
		}

		void InitializeBase(Actor self, bool chooseLocation)
		{
			var mcv = FindAndDeployMcv(self, chooseLocation);

			if (mcv == null)
				return;

			Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, World, () =>
			{
				foreach (var n in positionsUpdatedModules)
				{
					n.UpdatedBaseCenter(mcv.Location);
					n.UpdatedDefenseCenter(mcv.Location);
				}
			});
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		// Find any MCV and deploy them at a sensible location.
		Actor FindAndDeployMcv(Actor self, bool move)
		{
			var mcv = self.World.Actors.FirstOrDefault(a => a.Owner == Player &&
				Info.UnitsCommonNames.Mcv.Contains(a.Info.Name) && a.IsIdle);

			if (mcv == null)
				return null;

			// Don't try to move and deploy an undeployable actor
			var transformsInfo = mcv.Info.TraitInfoOrDefault<TransformsInfo>();
			if (transformsInfo == null)
				return null;

			// If we lack a base, we need to make sure we don't restrict deployment of the MCV to the base!
			var restrictToBase = Info.RestrictMCVDeploymentFallbackToBase &&
				AIUtils.CountBuildingByCommonName(Info.BuildingCommonNames.ConstructionYard, Player) > 0;

			if (move)
			{
				var desiredLocation = ChooseMcvDeployLocation(transformsInfo.IntoActor, restrictToBase);
				if (desiredLocation == null)
					return null;

				QueueOrder(new Order("Move", mcv, Target.FromCell(World, desiredLocation.Value), true));
			}

			QueueOrder(new Order("DeployTransform", mcv, true));

			return mcv;
		}

		void ProductionUnits(Actor self)
		{
			// Stop building until economy is restored
			if (!HasAdequateRefineryCount)
				return;

			// No construction yards - Build a new MCV
			if (Info.UnitsCommonNames.Mcv.Any() && HasAdequateConstructionYardCount &&
				!self.World.Actors.Any(a => a.Owner == Player && Info.UnitsCommonNames.Mcv.Contains(a.Info.Name)))
				BuildUnit("Vehicle", AIUtils.GetInfoByCommonName(Info.UnitsCommonNames.Mcv, Player).Name);

			foreach (var q in Info.UnitQueues)
				BuildUnit(q, unitsHangingAroundTheBase.Count < Info.IdleBaseUnitsMaximum);
		}

		void BuildUnit(string category, bool buildRandom)
		{
			// Pick a free queue
			var queue = AIUtils.FindQueues(Player, category).FirstOrDefault(q => !q.AllQueued().Any());
			if (queue == null)
				return;

			var unit = buildRandom ?
				ChooseRandomUnitToBuild(queue) :
				ChooseUnitToBuild(queue);

			if (unit == null)
				return;

			var name = unit.Name;

			if (Info.UnitsToBuild != null && !Info.UnitsToBuild.ContainsKey(name))
				return;

			if (Info.UnitLimits != null &&
				Info.UnitLimits.ContainsKey(name) &&
				World.Actors.Count(a => a.Owner == Player && a.Info.Name == name) >= Info.UnitLimits[name])
				return;

			QueueOrder(Order.StartProduction(queue.Actor, name, 1));
		}

		void BuildUnit(string category, string name)
		{
			var queue = AIUtils.FindQueues(Player, category).FirstOrDefault(q => !q.AllQueued().Any());
			if (queue == null)
				return;

			if (Map.Rules.Actors[name] != null)
				QueueOrder(Order.StartProduction(queue.Actor, name, 1));
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!IsEnabled)
				return;

			// TODO: Add an option to include this in CheckSyncUnchanged.
			// Checking sync for this is too expensive to include it by default,
			// so it should be implemented as separate sub-option checkbox.
			using (new PerfSample("bot_attack_response"))
			{
				Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, World, () =>
				{
					foreach (var t in attackResponseModules)
						if (t.IsTraitEnabled())
							t.RespondToAttack(this, self, e);
				});
			}

			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (e.Attacker.Owner.Stances[self.Owner] != Stance.Enemy)
				return;

			if (!e.Attacker.Info.HasTraitInfo<ITargetableInfo>())
				return;

			// Protected priority assets, MCVs, harvesters and buildings
			// TODO: Use *CommonNames, instead of hard-coding trait(info)s.
			if (self.Info.HasTraitInfo<HarvesterInfo>() || self.Info.HasTraitInfo<BuildingInfo>() || self.Info.HasTraitInfo<BaseBuildingInfo>())
				ProtectOwn(e.Attacker);
		}
	}
}
