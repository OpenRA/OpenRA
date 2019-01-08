#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits.BotModules.Squads;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Manages AI squads.")]
	public class SquadManagerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are valid for naval squads.")]
		public readonly HashSet<string> NavalUnitsTypes = new HashSet<string>();

		[Desc("Actor types that should generally be excluded from attack squads.")]
		public readonly HashSet<string> ExcludeFromSquadsTypes = new HashSet<string>();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> NavalProductionTypes = new HashSet<string>();

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

		[Desc("Random number of up to this many units is added to squad size when creating an attack squad.")]
		public readonly int SquadSizeRandomBonus = 30;

		[Desc("Delay (in ticks) between giving out orders to units.")]
		public readonly int AssignRolesInterval = 50;

		[Desc("Delay (in ticks) between attempting rush attacks.")]
		public readonly int RushInterval = 600;

		[Desc("Delay (in ticks) between updating squads.")]
		public readonly int AttackForceInterval = 75;

		[Desc("Minimum delay (in ticks) between creating squads.")]
		public readonly int MinimumAttackForceDelay = 0;

		[Desc("Radius in cells around enemy BaseBuilder (Construction Yard) where AI scans for targets to rush.")]
		public readonly int RushAttackScanRadius = 15;

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

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

		public override object Create(ActorInitializer init) { return new SquadManagerBotModule(init.Self, this); }
	}

	public class SquadManagerBotModule : ConditionalTrait<SquadManagerBotModuleInfo>, IBotTick, IBotRespondToAttack, IBotPositionsUpdated
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = World.Actors.Where(a => a.Owner == Player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(World.LocalRandom);

			return randomConstructionYard != null ? randomConstructionYard.Location : initialBaseCenter;
		}

		public readonly World World;
		public readonly Player Player;

		readonly Predicate<Actor> unitCannotBeOrdered;

		public List<Squad> Squads = new List<Squad>();

		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotNotifyIdleBaseUnits[] notifyIdleBaseUnits;

		CPos initialBaseCenter;
		List<Actor> unitsHangingAroundTheBase = new List<Actor>();

		// Units that the bot already knows about. Any unit not on this list needs to be given a role.
		List<Actor> activeUnits = new List<Actor>();

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int minAttackForceDelayTicks;

		public SquadManagerBotModule(Actor self, SquadManagerBotModuleInfo info)
			: base(info)
		{
			World = self.World;
			Player = self.Owner;

			unitCannotBeOrdered = a => a.Owner != Player || a.IsDead || !a.IsInWorld;
		}

		public bool IsEnemyUnit(Actor a)
		{
			return a != null && !a.IsDead && Player.Stances[a.Owner] == Stance.Enemy
				&& !a.Info.HasTraitInfo<HuskInfo>()
				&& !a.GetEnabledTargetTypes().IsEmpty;
		}

		protected override void TraitEnabled(Actor self)
		{
			notifyPositionsUpdated = Player.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			notifyIdleBaseUnits = Player.PlayerActor.TraitsImplementing<IBotNotifyIdleBaseUnits>().ToArray();

			// Avoid all AIs trying to rush in the same tick, randomize their initial rush a little.
			var smallFractionOfRushInterval = Info.RushInterval / 20;
			rushTicks = World.LocalRandom.Next(Info.RushInterval - smallFractionOfRushInterval, Info.RushInterval + smallFractionOfRushInterval);

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = World.LocalRandom.Next(0, Info.AssignRolesInterval);
			attackForceTicks = World.LocalRandom.Next(0, Info.AttackForceInterval);
			minAttackForceDelayTicks = World.LocalRandom.Next(0, Info.MinimumAttackForceDelay);
		}

		void IBotTick.BotTick(IBot bot)
		{
			AssignRolesToIdleUnits(bot);
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			return World.Actors.Where(IsEnemyUnit).ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return World.FindActorsInCircle(pos, radius).Where(IsEnemyUnit).ClosestTo(pos);
		}

		void CleanSquads()
		{
			Squads.RemoveAll(s => !s.IsValid);
			foreach (var s in Squads)
				s.Units.RemoveAll(unitCannotBeOrdered);
		}

		// HACK: Use of this function requires that there is one squad of this type.
		Squad GetSquadOfType(SquadType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		Squad RegisterNewSquad(IBot bot, SquadType type, Actor target = null)
		{
			var ret = new Squad(bot, this, type, target);
			Squads.Add(ret);
			return ret;
		}

		void AssignRolesToIdleUnits(IBot bot)
		{
			CleanSquads();

			activeUnits.RemoveAll(unitCannotBeOrdered);
			unitsHangingAroundTheBase.RemoveAll(unitCannotBeOrdered);
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			if (--rushTicks <= 0)
			{
				rushTicks = Info.RushInterval;
				TryToRushAttack(bot);
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
				FindNewUnits(bot);
			}

			if (--minAttackForceDelayTicks <= 0)
			{
				minAttackForceDelayTicks = Info.MinimumAttackForceDelay;
				CreateAttackForce(bot);
			}
		}

		void FindNewUnits(IBot bot)
		{
			var newUnits = World.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player &&
					!Info.ExcludeFromSquadsTypes.Contains(a.Info.Name) &&
					!activeUnits.Contains(a));

			foreach (var a in newUnits)
			{
				unitsHangingAroundTheBase.Add(a);

				if (a.Info.HasTraitInfo<AircraftInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>())
				{
					var air = GetSquadOfType(SquadType.Air);
					if (air == null)
						air = RegisterNewSquad(bot, SquadType.Air);

					air.Units.Add(a);
				}
				else if (Info.NavalUnitsTypes.Contains(a.Info.Name))
				{
					var ships = GetSquadOfType(SquadType.Naval);
					if (ships == null)
						ships = RegisterNewSquad(bot, SquadType.Naval);

					ships.Units.Add(a);
				}

				activeUnits.Add(a);
			}

			// Notifying here rather than inside the loop, should be fine and saves a bunch of notification calls
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
		}

		void CreateAttackForce(IBot bot)
		{
			// Create an attack force when we have enough units around our base.
			// (don't bother leaving any behind for defense)
			var randomizedSquadSize = Info.SquadSize + World.LocalRandom.Next(Info.SquadSizeRandomBonus);

			if (unitsHangingAroundTheBase.Count >= randomizedSquadSize)
			{
				var attackForce = RegisterNewSquad(bot, SquadType.Assault);

				foreach (var a in unitsHangingAroundTheBase)
					if (!a.Info.HasTraitInfo<AircraftInfo>())
						attackForce.Units.Add(a);

				unitsHangingAroundTheBase.Clear();
				foreach (var n in notifyIdleBaseUnits)
					n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
			}
		}

		void TryToRushAttack(IBot bot)
		{
			var allEnemyBaseBuilder = AIUtils.FindEnemiesByCommonName(Info.ConstructionYardTypes, Player);

			// TODO: This should use common names & ExcludeFromSquads instead of hardcoding TraitInfo checks
			var ownUnits = activeUnits
				.Where(unit => unit.IsIdle && unit.Info.HasTraitInfo<AttackBaseInfo>()
					&& !unit.Info.HasTraitInfo<AircraftInfo>() && !unit.Info.HasTraitInfo<HarvesterInfo>()).ToList();

			if (!allEnemyBaseBuilder.Any() || ownUnits.Count < Info.SquadSize)
				return;

			foreach (var b in allEnemyBaseBuilder)
			{
				// Don't rush enemy aircraft!
				var enemies = World.FindActorsInCircle(b.CenterPosition, WDist.FromCells(Info.RushAttackScanRadius))
					.Where(unit => IsEnemyUnit(unit) && unit.Info.HasTraitInfo<AttackBaseInfo>() && !unit.Info.HasTraitInfo<AircraftInfo>()).ToList();

				if (AttackOrFleeFuzzy.Rush.CanAttack(ownUnits, enemies))
				{
					var target = enemies.Any() ? enemies.Random(World.LocalRandom) : b;
					var rush = GetSquadOfType(SquadType.Rush);
					if (rush == null)
						rush = RegisterNewSquad(bot, SquadType.Rush, target);

					foreach (var a3 in ownUnits)
						rush.Units.Add(a3);

					return;
				}
			}
		}

		void ProtectOwn(IBot bot, Actor attacker)
		{
			var protectSq = GetSquadOfType(SquadType.Protection);
			if (protectSq == null)
				protectSq = RegisterNewSquad(bot, SquadType.Protection, attacker);

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

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!IsEnemyUnit(e.Attacker))
				return;

			// Protected priority assets, MCVs, harvesters and buildings
			// TODO: Use *CommonNames, instead of hard-coding trait(info)s.
			if (self.Info.HasTraitInfo<HarvesterInfo>() || self.Info.HasTraitInfo<BuildingInfo>() || self.Info.HasTraitInfo<BaseBuildingInfo>())
			{
				foreach (var n in notifyPositionsUpdated)
					n.UpdatedDefenseCenter(e.Attacker.Location);

				ProtectOwn(bot, e.Attacker);
			}
		}
	}
}
