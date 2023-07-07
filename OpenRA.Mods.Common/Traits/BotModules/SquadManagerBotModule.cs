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
using OpenRA.Mods.Common.Traits.BotModules.Squads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI squads.")]
	public class SquadManagerBotModuleInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[Desc("Actor types that are valid for naval squads.")]
		public readonly HashSet<string> NavalUnitsTypes = new();

		[ActorReference]
		[Desc("Actor types that are excluded from ground attacks.")]
		public readonly HashSet<string> AirUnitsTypes = new();

		[ActorReference]
		[Desc("Actor types that should generally be excluded from attack squads.")]
		public readonly HashSet<string> ExcludeFromSquadsTypes = new();

		[ActorReference]
		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new();

		[ActorReference]
		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> NavalProductionTypes = new();

		[ActorReference]
		[Desc("Own actor types that are prioritized when defending.")]
		public readonly HashSet<string> ProtectionTypes = new();

		[Desc("Target types are used for identifying aircraft.")]
		public readonly BitSet<TargetableType> AircraftTargetType = new("Air");

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

		[Desc("Enemy target types to never target.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (DangerScanRadius <= 0)
				throw new YamlException("DangerScanRadius must be greater than zero.");
		}

		public override object Create(ActorInitializer init) { return new SquadManagerBotModule(init.Self, this); }
	}

	public class SquadManagerBotModule : ConditionalTrait<SquadManagerBotModuleInfo>, IBotEnabled, IBotTick, IBotRespondToAttack, IBotPositionsUpdated, IGameSaveTraitData
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = World.Actors.Where(a => a.Owner == Player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(World.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public readonly World World;
		public readonly Player Player;

		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly List<Actor> unitsHangingAroundTheBase = new();

		// Units that the bot already knows about. Any unit not on this list needs to be given a role.
		readonly HashSet<Actor> activeUnits = new();

		public List<Squad> Squads = new();

		IBot bot;
		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotNotifyIdleBaseUnits[] notifyIdleBaseUnits;

		CPos initialBaseCenter;

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int minAttackForceDelayTicks;

		public SquadManagerBotModule(Actor self, SquadManagerBotModuleInfo info)
			: base(info)
		{
			World = self.World;
			Player = self.Owner;

			unitCannotBeOrdered = a => a == null || a.Owner != Player || a.IsDead || !a.IsInWorld;
		}

		// Use for proactive targeting.
		public bool IsPreferredEnemyUnit(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			if (targetTypes.IsEmpty || targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes))
				return false;

			return IsNotHiddenUnit(a);
		}

		bool IsNotHiddenUnit(Actor a)
		{
			var hasModifier = false;
			var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
			foreach (var v in visModifiers)
			{
				if (v.IsVisible(a, Player))
					return true;

				hasModifier = true;
			}

			return !hasModifier;
		}

		protected override void Created(Actor self)
		{
			notifyPositionsUpdated = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			notifyIdleBaseUnits = self.Owner.PlayerActor.TraitsImplementing<IBotNotifyIdleBaseUnits>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs trying to rush in the same tick, randomize their initial rush a little.
			var smallFractionOfRushInterval = Info.RushInterval / 20;
			rushTicks = World.LocalRandom.Next(Info.RushInterval - smallFractionOfRushInterval, Info.RushInterval + smallFractionOfRushInterval);

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = World.LocalRandom.Next(0, Info.AssignRolesInterval);
			attackForceTicks = World.LocalRandom.Next(0, Info.AttackForceInterval);
			minAttackForceDelayTicks = World.LocalRandom.Next(0, Info.MinimumAttackForceDelay);
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			this.bot = bot;
		}

		void IBotTick.BotTick(IBot bot)
		{
			AssignRolesToIdleUnits(bot);
		}

		internal static Actor ClosestTo(IEnumerable<Actor> ownActors, Actor targetActor)
		{
			// Return actors that can get within weapons range of the target.
			// First, let's determine the max weapons range for each of the actors.
			var target = Target.FromActor(targetActor);
			var ownActorsAndTheirAttackRanges = ownActors
				.Select(a => (Actor: a, AttackBases: a.TraitsImplementing<AttackBase>().Where(Exts.IsTraitEnabled)
					.Where(ab => ab.HasAnyValidWeapons(target)).ToList()))
				.Where(x => x.AttackBases.Count > 0)
				.Select(x => (x.Actor, Range: x.AttackBases.Max(ab => ab.GetMaximumRangeVersusTarget(target))))
				.ToDictionary(x => x.Actor, x => x.Range);

			// Now determine if each actor can either path directly to the target,
			// or if it can path to a nearby location at the edge of its weapon range to the target
			// A thorough check would check each position within the circle, but for performance
			// we'll only check 8 positions around the edge of the circle.
			// We need to account for the weapons range here to account for units such as boats.
			// They can't path directly to a land target,
			// but might be able to get close enough to shore to attack the target from range.
			return ownActorsAndTheirAttackRanges.Keys
				.ClosestToWithPathToAny(targetActor.World, a =>
				{
					var range = ownActorsAndTheirAttackRanges[a].Length;
					var rangeDiag = Exts.MultiplyBySqrtTwoOverTwo(range);
					return new[]
					{
						targetActor.CenterPosition,
						targetActor.CenterPosition + new WVec(range, 0, 0),
						targetActor.CenterPosition + new WVec(-range, 0, 0),
						targetActor.CenterPosition + new WVec(0, range, 0),
						targetActor.CenterPosition + new WVec(0, -range, 0),
						targetActor.CenterPosition + new WVec(rangeDiag, rangeDiag, 0),
						targetActor.CenterPosition + new WVec(-rangeDiag, rangeDiag, 0),
						targetActor.CenterPosition + new WVec(-rangeDiag, -rangeDiag, 0),
						targetActor.CenterPosition + new WVec(rangeDiag, -rangeDiag, 0),
					};
				});
		}

		internal IEnumerable<(Actor Actor, WVec Offset)> FindEnemies(IEnumerable<Actor> actors, Actor sourceActor)
		{
			// Check units are in fact enemies and not hidden.
			// Then check which are in weapons range of the source.
			var activeAttackBases = sourceActor.TraitsImplementing<AttackBase>().Where(Exts.IsTraitEnabled).ToArray();
			var enemiesAndSourceAttackRanges = actors
				.Where(IsPreferredEnemyUnit)
				.Select(a => (Actor: a, AttackBases: activeAttackBases.Where(ab => ab.HasAnyValidWeapons(Target.FromActor(a))).ToList()))
				.Where(x => x.AttackBases.Count > 0)
				.Select(x => (x.Actor, Range: x.AttackBases.Max(ab => ab.GetMaximumRangeVersusTarget(Target.FromActor(x.Actor)))))
				.ToDictionary(x => x.Actor, x => x.Range);

			// Now determine if the source actor can path directly to the target,
			// or if it can path to a nearby location at the edge of its weapon range to the target
			// A thorough check would check each position within the circle, but for performance
			// we'll only check 8 positions around the edge of the circle.
			// We need to account for the weapons range here to account for units such as boats.
			// They can't path directly to a land target,
			// but might be able to get close enough to shore to attack the target from range.
			return enemiesAndSourceAttackRanges.Keys
				.WithPathFrom(sourceActor, a =>
				{
					var range = enemiesAndSourceAttackRanges[a].Length;
					var rangeDiag = Exts.MultiplyBySqrtTwoOverTwo(range);
					return new[]
					{
						WVec.Zero,
						new WVec(range, 0, 0),
						new WVec(-range, 0, 0),
						new WVec(0, range, 0),
						new WVec(0, -range, 0),
						new WVec(rangeDiag, rangeDiag, 0),
						new WVec(-rangeDiag, rangeDiag, 0),
						new WVec(-rangeDiag, -rangeDiag, 0),
						new WVec(rangeDiag, -rangeDiag, 0),
					};
				})
				.Select(x => (x.Actor, x.ReachableOffsets.MinBy(o => o.LengthSquared)));
		}

		internal (Actor Actor, WVec Offset) FindClosestEnemy(Actor sourceActor)
		{
			return FindClosestEnemy(World.Actors, sourceActor);
		}

		internal (Actor Actor, WVec Offset) FindClosestEnemy(Actor sourceActor, WDist radius)
		{
			return FindClosestEnemy(World.FindActorsInCircle(sourceActor.CenterPosition, radius), sourceActor);
		}

		(Actor Actor, WVec Offset) FindClosestEnemy(IEnumerable<Actor> actors, Actor sourceActor)
		{
			return WorldUtils.ClosestToIgnoringPath(FindEnemies(actors, sourceActor), x => x.Actor, sourceActor);
		}

		void CleanSquads()
		{
			foreach (var s in Squads)
				s.Units.RemoveWhere(unitCannotBeOrdered);
			Squads.RemoveAll(s => !s.IsValid);
		}

		// HACK: Use of this function requires that there is one squad of this type.
		Squad GetSquadOfType(SquadType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		Squad RegisterNewSquad(IBot bot, SquadType type, (Actor Actor, WVec Offset) target = default)
		{
			var ret = new Squad(bot, this, type, target);
			Squads.Add(ret);
			return ret;
		}

		internal void UnregisterSquad(Squad squad)
		{
			activeUnits.ExceptWith(squad.Units);
			squad.Units.Clear();

			// CleanSquads will remove the squad from the Squads list.
			// We can't do that here as this is designed to be called from within Squad.Update
			// and thus would mutate the Squads list we are iterating over.
		}

		void AssignRolesToIdleUnits(IBot bot)
		{
			CleanSquads();

			activeUnits.RemoveWhere(unitCannotBeOrdered);
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
				if (Info.AirUnitsTypes.Contains(a.Info.Name))
				{
					var air = GetSquadOfType(SquadType.Air);
					air ??= RegisterNewSquad(bot, SquadType.Air);

					air.Units.Add(a);
				}
				else if (Info.NavalUnitsTypes.Contains(a.Info.Name))
				{
					var ships = GetSquadOfType(SquadType.Naval);
					ships ??= RegisterNewSquad(bot, SquadType.Naval);

					ships.Units.Add(a);
				}
				else
					unitsHangingAroundTheBase.Add(a);

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

				attackForce.Units.UnionWith(unitsHangingAroundTheBase);

				unitsHangingAroundTheBase.Clear();
				foreach (var n in notifyIdleBaseUnits)
					n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
			}
		}

		void TryToRushAttack(IBot bot)
		{
			var ownUnits = activeUnits
				.Where(unit =>
					unit.IsIdle
					&& unit.Info.HasTraitInfo<AttackBaseInfo>()
					&& !Info.AirUnitsTypes.Contains(unit.Info.Name)
					&& !Info.NavalUnitsTypes.Contains(unit.Info.Name)
					&& !Info.ExcludeFromSquadsTypes.Contains(unit.Info.Name))
				.ToList();

			if (ownUnits.Count < Info.SquadSize)
				return;

			var allEnemyBaseBuilder = FindEnemies(
				World.Actors.Where(a => Info.ConstructionYardTypes.Contains(a.Info.Name)),
				ownUnits.First())
				.ToList();

			if (allEnemyBaseBuilder.Count == 0 || ownUnits.Count < Info.SquadSize)
				return;

			foreach (var enemyBaseBuilder in allEnemyBaseBuilder)
			{
				// Don't rush enemy aircraft!
				var enemies = FindEnemies(
					World.FindActorsInCircle(enemyBaseBuilder.Actor.CenterPosition, WDist.FromCells(Info.RushAttackScanRadius))
						.Where(unit =>
							unit.Info.HasTraitInfo<AttackBaseInfo>()
							&& !Info.AirUnitsTypes.Contains(unit.Info.Name)
							&& !Info.NavalUnitsTypes.Contains(unit.Info.Name)),
					ownUnits.First())
					.ToList();

				if (AttackOrFleeFuzzy.Rush.CanAttack(ownUnits, enemies.Select(x => x.Actor).ToList()))
				{
					var target = enemies.Count > 0 ? enemies.Random(World.LocalRandom) : enemyBaseBuilder;
					var rush = GetSquadOfType(SquadType.Rush);
					rush ??= RegisterNewSquad(bot, SquadType.Rush, target);

					rush.Units.UnionWith(ownUnits);

					return;
				}
			}
		}

		void ProtectOwn(IBot bot, Actor attacker)
		{
			var protectSq = GetSquadOfType(SquadType.Protection);
			protectSq ??= RegisterNewSquad(bot, SquadType.Protection, (attacker, WVec.Zero));

			if (protectSq.IsValid && !protectSq.IsTargetValid(protectSq.CenterUnit()))
				protectSq.SetActorToTarget((attacker, WVec.Zero));

			if (!protectSq.IsValid)
			{
				var ownUnits = World.FindActorsInCircle(World.Map.CenterOfCell(GetRandomBaseCenter()), WDist.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit =>
						unit.Owner == Player
						&& !Info.ProtectionTypes.Contains(unit.Info.Name)
						&& unit.Info.HasTraitInfo<AttackBaseInfo>())
					.WithPathTo(World, attacker.CenterPosition);

				protectSq.Units.UnionWith(ownUnits);
			}
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!IsPreferredEnemyUnit(e.Attacker))
				return;

			if (Info.ProtectionTypes.Contains(self.Info.Name))
			{
				foreach (var n in notifyPositionsUpdated)
					n.UpdatedDefenseCenter(e.Attacker.Location);

				ProtectOwn(bot, e.Attacker);
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Squads", "", Squads.Select(s => new MiniYamlNode("Squad", s.Serialize())).ToList()),
				new MiniYamlNode("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new MiniYamlNode("UnitsHangingAroundTheBase", FieldSaver.FormatValue(unitsHangingAroundTheBase
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new MiniYamlNode("ActiveUnits", FieldSaver.FormatValue(activeUnits
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new MiniYamlNode("RushTicks", FieldSaver.FormatValue(rushTicks)),
				new MiniYamlNode("AssignRolesTicks", FieldSaver.FormatValue(assignRolesTicks)),
				new MiniYamlNode("AttackForceTicks", FieldSaver.FormatValue(attackForceTicks)),
				new MiniYamlNode("MinAttackForceDelayTicks", FieldSaver.FormatValue(minAttackForceDelayTicks)),
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();

			if (nodes.TryGetValue("InitialBaseCenter", out var initialBaseCenterNode))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value);

			if (nodes.TryGetValue("UnitsHangingAroundTheBase", out var unitsHangingAroundTheBaseNode))
			{
				unitsHangingAroundTheBase.Clear();
				unitsHangingAroundTheBase.AddRange(FieldLoader.GetValue<uint[]>("UnitsHangingAroundTheBase", unitsHangingAroundTheBaseNode.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			if (nodes.TryGetValue("ActiveUnits", out var activeUnitsNode))
			{
				activeUnits.Clear();
				activeUnits.UnionWith(FieldLoader.GetValue<uint[]>("ActiveUnits", activeUnitsNode.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			if (nodes.TryGetValue("RushTicks", out var rushTicksNode))
				rushTicks = FieldLoader.GetValue<int>("RushTicks", rushTicksNode.Value);

			if (nodes.TryGetValue("AssignRolesTicks", out var assignRolesTicksNode))
				assignRolesTicks = FieldLoader.GetValue<int>("AssignRolesTicks", assignRolesTicksNode.Value);

			if (nodes.TryGetValue("AttackForceTicks", out var attackForceTicksNode))
				attackForceTicks = FieldLoader.GetValue<int>("AttackForceTicks", attackForceTicksNode.Value);

			if (nodes.TryGetValue("MinAttackForceDelayTicks", out var minAttackForceDelayTicksNode))
				minAttackForceDelayTicks = FieldLoader.GetValue<int>("MinAttackForceDelayTicks", minAttackForceDelayTicksNode.Value);

			if (nodes.TryGetValue("Squads", out var squadsNode))
			{
				Squads.Clear();
				foreach (var n in squadsNode.Nodes)
					Squads.Add(Squad.Deserialize(bot, this, n.Value));
			}
		}
	}
}
