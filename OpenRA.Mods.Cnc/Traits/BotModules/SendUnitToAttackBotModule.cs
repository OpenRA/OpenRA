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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Flags]
	public enum TargetDistance
	{
		Closest = 0,
		Furthest = 1,
		Random = 2
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Bot logic for units that should not be sent with a regular squad, like suicide or subterranean units.")]
	public sealed class SendUnitToAttackBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actors used for attack, and their options on attacking.")]
		[FieldLoader.LoadUsing(nameof(LoadOptions))]
		public readonly Dictionary<string, UnitAttackOptions> ActorTypesAndAttackOptions = default;

		[Desc("Target types that can be targeted.")]
		public readonly BitSet<TargetableType> ValidTargets = new("Structure");

		[Desc("Target types that can't be targeted.")]
		public readonly BitSet<TargetableType> InvalidTargets;

		[Desc("Player relationships that will be targeted.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("Should attack the furthest or closest target. Possible values are Closest, Furthest, Random",
			"Multiple values mean the distance randomizes between them")]
		public readonly TargetDistance[] TargetDistances = { TargetDistance.Closest };

		[Desc("Prepare unit, disguise unit and try attack target in this interval.")]
		public readonly int ScanTick = 463;

		[Desc("The total attack desire increases by this amount per scan",
			"Note: When there is no attack unit, the total attack desire will return to 0.")]
		public readonly int AttackDesireIncreasedPerScan = 10;

		static object LoadOptions(MiniYaml yaml)
		{
			var ret = new Dictionary<string, UnitAttackOptions>();
			var options = yaml.Nodes.FirstOrDefault(n => n.Key == "ActorTypesAndAttackOptions");
			if (options != null)
				foreach (var d in options.Value.Nodes)
				{
					ret.Add(d.Key, new UnitAttackOptions(d.Value));
				}

			return ret;
		}

		public override object Create(ActorInitializer init) { return new SendUnitToAttackBotModule(init.Self, this); }
	}

	public class SendUnitToAttackBotModule : ConditionalTrait<SendUnitToAttackBotModuleInfo>, IBotTick
	{
		const PlayerRelationship ValidDisguiseRelationship = PlayerRelationship.Ally | PlayerRelationship.Neutral;

		readonly World world;
		readonly Player player;

		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> isInvalidActor;

		readonly List<TraitPair<Disguise>> disguisePairs = new();
		BitSet<TargetableType> disguiseTypes;
		List<Actor> attackActors = new();

		int prepareAttackTicks;
		int disguiseDelayTicks;
		int assignAttackTicks;

		Player targetPlayer;
		int desireIncreased;

		public SendUnitToAttackBotModule(Actor self, SendUnitToAttackBotModuleInfo info)
		: base(info)
		{
			world = self.World;
			player = self.Owner;
			isInvalidActor = a => a == null || a.IsDead || !a.IsInWorld;
			unitCannotBeOrdered = a => isInvalidActor(a) || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || !(a.IsIdle || a.CurrentActivity is FlyIdle);
			desireIncreased = 0;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			// and we divide preparing stage, disguising stage and attacking stage for PERF.
			prepareAttackTicks = world.LocalRandom.Next(0, Info.ScanTick);
			disguiseDelayTicks = prepareAttackTicks + Info.ScanTick / 3;
			assignAttackTicks = disguiseDelayTicks + Info.ScanTick / 3;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--prepareAttackTicks <= 0)
			{
				prepareAttackTicks = Info.ScanTick;
				PrepareAttackTick(bot);
			}

			if (--disguiseDelayTicks < 0)
			{
				disguiseDelayTicks = Info.ScanTick;
				if (targetPlayer.WinState != WinState.Lost)
					DisguiseTicks(bot);
			}

			if (--assignAttackTicks <= 0)
			{
				assignAttackTicks = Info.ScanTick;
				if (targetPlayer.WinState != WinState.Lost)
					AttackTicks(bot);
			}
		}

		void PrepareAttackTick(IBot bot)
		{
			// Randomly choose target player to attack
			var targetPlayers = world.Players.Where(p => p.WinState != WinState.Lost && Info.ValidRelationships.HasRelationship(p.RelationshipWith(player))).ToList();
			if (targetPlayers.Count == 0)
				return;
			targetPlayer = targetPlayers.Random(world.LocalRandom);

			attackActors = world.ActorsHavingTrait<IPositionable>().Where(a =>
			{
				if (!unitCannotBeOrderedOrIsBusy(a) && Info.ActorTypesAndAttackOptions.TryGetValue(a.Info.Name, out var option))
				{
					if (option.TryGetHealed && TryGetHeal(bot, a))
						return false;

					if (option.AttackRequires.HasFlag(AttackRequires.CargoLoaded) && a.TraitsImplementing<Cargo>().FirstOrDefault(t => !t.IsTraitDisabled) is Cargo cargo && cargo.IsEmpty())
						return false;

					if (option.TryDisguise && a.TraitOrDefault<Disguise>() is Disguise disguise && !disguise.Disguised)
					{
						disguisePairs.Add(new TraitPair<Disguise>(a, disguise));
						disguiseTypes = disguiseTypes.Union(disguise.Info.TargetTypes);
						if (option.AttackRequires.HasFlag(AttackRequires.Disguised))
							return false;
					}

					return true;
				}

				return false;
			}).ToList();
		}

		void DisguiseTicks(IBot bot)
		{
			var invalidActors = new HashSet<Actor>();
			foreach (var p in disguisePairs)
			{
				if (isInvalidActor(p.Actor))
					invalidActors.Add(p.Actor);
			}

			disguisePairs.RemoveAll(p => invalidActors.Contains(p.Actor));

			if (disguisePairs.Count <= 0)
				return;

			var targets = world.Actors.Where(a =>
			{
				if (isInvalidActor(a) || !ValidDisguiseRelationship.HasRelationship(a.Owner.RelationshipWith(targetPlayer)) || a.Info.HasTraitInfo<DisguiseInfo>())
					return false;

				var t = a.GetEnabledTargetTypes();

				if (!disguiseTypes.Overlaps(t))
					return false;

				var hasModifier = false;
				var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
				foreach (var v in visModifiers)
				{
					if (v.IsVisible(a, player))
						return true;

					hasModifier = true;
				}

				return !hasModifier;
			});

			foreach (var t in targets)
			{
				invalidActors.Clear();
				foreach (var p in disguisePairs)
				{
					if (!p.Trait.Info.TargetTypes.Overlaps(t.GetEnabledTargetTypes()))
						continue;

					bot.QueueOrder(new Order("Disguise", p.Actor, Target.FromActor(t), true));
					invalidActors.Add(p.Actor);
					attackActors.Add(p.Actor);
				}

				disguisePairs.RemoveAll(p => invalidActors.Contains(p.Actor));
				if (disguisePairs.Count == 0)
					break;
			}

			disguisePairs.Clear();
		}

		void AttackTicks(IBot bot)
		{
			var attackdesire = 0;

			var invalidActors = new HashSet<Actor>();
			foreach (var a in attackActors)
			{
				if (unitCannotBeOrderedOrIsBusy(a))
					invalidActors.Add(a);
				else
					attackdesire += Info.ActorTypesAndAttackOptions[a.Info.Name].AttackDesireOfEach;
			}

			attackActors.RemoveAll(invalidActors.Contains);
			invalidActors.Clear();

			if (attackActors.Count == 0)
			{
				desireIncreased = 0;
				return;
			}

			desireIncreased += Info.AttackDesireIncreasedPerScan;
			if (desireIncreased + attackdesire < 100)
				return;

			var targets = world.Actors.Where(a =>
			{
				if (isInvalidActor(a) || a.Owner != targetPlayer)
					return false;

				var t = a.GetEnabledTargetTypes();

				if (!Info.ValidTargets.Overlaps(t) || Info.InvalidTargets.Overlaps(t))
					return false;

				var hasModifier = false;
				var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
				foreach (var v in visModifiers)
				{
					if (v.IsVisible(a, player))
						return true;

					hasModifier = true;
				}

				return !hasModifier;
			});

			var targetDistance = Info.TargetDistances.Random(world.LocalRandom);
			switch (targetDistance)
			{
				case TargetDistance.Closest:
					targets = targets.OrderBy(a => (a.CenterPosition - attackActors[0].CenterPosition).HorizontalLengthSquared);
					break;
				case TargetDistance.Furthest:
					targets = targets.OrderByDescending(a => (a.CenterPosition - attackActors[0].CenterPosition).HorizontalLengthSquared);
					break;
				case TargetDistance.Random:
					targets = targets.Shuffle(world.LocalRandom);
					break;
			}

			foreach (var t in targets)
			{
				foreach (var a in attackActors)
				{
					if (!AIUtils.PathExist(a, t.Location, t))
						continue;

					AssignAttackOrders(bot, a, t);
					invalidActors.Add(a);
				}

				attackActors.RemoveAll(invalidActors.Contains);
				invalidActors.Clear();

				if (attackActors.Count == 0)
					break;
			}

			attackActors.Clear();
		}

		void AssignAttackOrders(IBot bot, Actor attacker, Actor victim)
		{
			var option = Info.ActorTypesAndAttackOptions[attacker.Info.Name];
			if (option.MoveToOrderName != null)
				bot.QueueOrder(new Order(option.MoveToOrderName, attacker, Target.FromCell(world, victim.Location), true));

			bot.QueueOrder(new Order(option.AttackOrderName, attacker, Target.FromActor(victim), true));

			if (option.MoveBackOrderName != null)
				bot.QueueOrder(new Order(option.MoveBackOrderName, attacker, Target.FromCell(world, attacker.Location), true));
		}

		protected static bool TryGetHeal(IBot bot, Actor unit)
		{
			var health = unit.TraitOrDefault<IHealth>();

			if (health != null && health.DamageState > DamageState.Undamaged)
			{
				Actor repairBuilding = null;
				var orderId = "Repair";
				var repairable = unit.TraitOrDefault<Repairable>();
				if (repairable != null)
					repairBuilding = repairable.FindRepairBuilding(unit);
				else
				{
					var repairableNear = unit.TraitOrDefault<RepairableNear>();
					if (repairableNear != null)
					{
						orderId = "RepairNear";
						repairBuilding = repairableNear.FindRepairBuilding(unit);
					}
				}

				if (repairBuilding != null)
				{
					bot.QueueOrder(new Order(orderId, unit, Target.FromActor(repairBuilding), true));
					return true;
				}

				return false;
			}
			else
				return false;
		}
	}
}
