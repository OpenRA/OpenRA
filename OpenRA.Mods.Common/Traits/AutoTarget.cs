#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The actor will automatically engage the enemy when it is in range.")]
	public class AutoTargetInfo : ConditionalTraitInfo, IRulesetLoaded, Requires<AttackBaseInfo>, UsesInit<StanceInit>
	{
		[Desc("It will try to hunt down the enemy if it is not set to defend.")]
		public readonly bool AllowMovement = true;

		[Desc("Set to a value >1 to override weapons maximum range for this.")]
		public readonly int ScanRadius = -1;

		[Desc("Possible values are HoldFire, ReturnFire, Defend and AttackAnything.",
			"Used for computer-controlled players, both Lua-scripted and regular Skirmish AI alike.")]
		public readonly UnitStance InitialStanceAI = UnitStance.AttackAnything;

		[Desc("Possible values are HoldFire, ReturnFire, Defend and AttackAnything. Used for human players.")]
		public readonly UnitStance InitialStance = UnitStance.Defend;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while in the HoldFire stance.")]
		public readonly string HoldFireCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while in the ReturnFire stance.")]
		public readonly string ReturnFireCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while in the Defend stance.")]
		public readonly string DefendCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while in the AttackAnything stance.")]
		public readonly string AttackAnythingCondition = null;

		[FieldLoader.Ignore]
		public readonly Dictionary<UnitStance, string> ConditionByStance = new Dictionary<UnitStance, string>();

		[Desc("Allow the player to change the unit stance.")]
		public readonly bool EnableStances = true;

		[Desc("Ticks to wait until next AutoTarget: attempt.")]
		public readonly int MinimumScanTimeInterval = 3;

		[Desc("Ticks to wait until next AutoTarget: attempt.")]
		public readonly int MaximumScanTimeInterval = 8;

		public readonly bool TargetWhenIdle = true;

		public readonly bool TargetWhenDamaged = true;

		public override object Create(ActorInitializer init) { return new AutoTarget(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			base.RulesetLoaded(rules, info);

			if (HoldFireCondition != null)
				ConditionByStance[UnitStance.HoldFire] = HoldFireCondition;

			if (ReturnFireCondition != null)
				ConditionByStance[UnitStance.ReturnFire] = ReturnFireCondition;

			if (DefendCondition != null)
				ConditionByStance[UnitStance.Defend] = DefendCondition;

			if (AttackAnythingCondition != null)
				ConditionByStance[UnitStance.AttackAnything] = AttackAnythingCondition;
		}
	}

	public enum UnitStance { HoldFire, ReturnFire, Defend, AttackAnything }

	public class AutoTarget : ConditionalTrait<AutoTargetInfo>, INotifyIdle, INotifyDamage, ITick, IResolveOrder, ISync, INotifyCreated
	{
		readonly IEnumerable<AttackBase> activeAttackBases;
		readonly AttackFollow[] attackFollows;
		[Sync] int nextScanTime = 0;

		public UnitStance Stance { get { return stance; } }

		[Sync] public Actor Aggressor;
		[Sync] public Actor TargetedActor;

		// NOT SYNCED: do not refer to this anywhere other than UI code
		public UnitStance PredictedStance;

		UnitStance stance;
		ConditionManager conditionManager;
		AutoTargetPriority[] targetPriorities;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public void SetStance(Actor self, UnitStance value)
		{
			if (stance == value)
				return;

			stance = value;
			ApplyStanceCondition(self);
		}

		void ApplyStanceCondition(Actor self)
		{
			if (conditionManager == null)
				return;

			if (conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

			string condition;
			if (Info.ConditionByStance.TryGetValue(stance, out condition))
				conditionToken = conditionManager.GrantCondition(self, condition);
		}

		public AutoTarget(ActorInitializer init, AutoTargetInfo info)
			: base(info)
		{
			var self = init.Self;
			activeAttackBases = self.TraitsImplementing<AttackBase>().ToArray().Where(Exts.IsTraitEnabled);

			if (init.Contains<StanceInit>())
				stance = init.Get<StanceInit, UnitStance>();
			else
				stance = self.Owner.IsBot || !self.Owner.Playable ? info.InitialStanceAI : info.InitialStance;

			PredictedStance = stance;
			attackFollows = self.TraitsImplementing<AttackFollow>().ToArray();
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			targetPriorities = self.TraitsImplementing<AutoTargetPriority>().ToArray();
			ApplyStanceCondition(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetUnitStance" && Info.EnableStances)
				SetStance(self, (UnitStance)order.ExtraData);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (!self.IsIdle || !Info.TargetWhenDamaged)
				return;

			var attacker = e.Attacker;
			if (attacker.Disposed || Stance < UnitStance.ReturnFire)
				return;

			if (!attacker.IsInWorld && !attacker.Disposed)
			{
				// If the aggressor is in a transport, then attack the transport instead
				var passenger = attacker.TraitOrDefault<Passenger>();
				if (passenger != null && passenger.Transport != null)
					attacker = passenger.Transport;
			}

			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			var attackerAsTarget = Target.FromActor(attacker);
			if (!activeAttackBases.Any(a => a.HasAnyValidWeapons(attackerAsTarget)))
				return;

			// don't retaliate against own units force-firing on us. It's usually not what the player wanted.
			if (attacker.AppearsFriendlyTo(self))
				return;

			// don't retaliate against healers
			if (e.Damage.Value < 0)
				return;

			Aggressor = attacker;

			bool allowMove;
			if (ShouldAttack(out allowMove))
				Attack(self, Aggressor, allowMove);
		}

		public void TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Stance < UnitStance.Defend || !Info.TargetWhenIdle)
				return;

			bool allowMove;
			if (ShouldAttack(out allowMove))
				ScanAndAttack(self, allowMove);
		}

		bool ShouldAttack(out bool allowMove)
		{
			allowMove = Info.AllowMovement && Stance != UnitStance.Defend;

			// PERF: Avoid LINQ.
			foreach (var attackFollow in attackFollows)
				if (!attackFollow.IsTraitDisabled && attackFollow.IsReachableTarget(attackFollow.Target, allowMove))
					return false;

			return true;
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (nextScanTime > 0)
				--nextScanTime;
		}

		public Actor ScanForTarget(Actor self, bool allowMove)
		{
			if (nextScanTime <= 0 && activeAttackBases.Any())
			{
				nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);

				foreach (var ab in activeAttackBases)
				{
					// If we can't attack right now, there's no need to try and find a target.
					var attackStances = ab.UnforcedAttackTargetStances();
					if (attackStances != OpenRA.Traits.Stance.None)
					{
						var range = Info.ScanRadius > 0 ? WDist.FromCells(Info.ScanRadius) : ab.GetMaximumRange();
						return ChooseTarget(self, ab, attackStances, range, allowMove);
					}
				}
			}

			return null;
		}

		public void ScanAndAttack(Actor self, bool allowMove)
		{
			var targetActor = ScanForTarget(self, allowMove);
			if (targetActor != null)
				Attack(self, targetActor, allowMove);
		}

		void Attack(Actor self, Actor targetActor, bool allowMove)
		{
			TargetedActor = targetActor;
			var target = Target.FromActor(targetActor);

			foreach (var ab in activeAttackBases)
				ab.AttackTarget(target, false, allowMove);
		}

		Actor ChooseTarget(Actor self, AttackBase ab, Stance attackStances, WDist scanRange, bool allowMove)
		{
			Actor chosenTarget = null;
			var chosenTargetPriority = int.MinValue;
			int chosenTargetRange = 0;

			var activePriorities = targetPriorities.Where(Exts.IsTraitEnabled)
				.Select(at => at.Info)
				.OrderByDescending(ati => ati.Priority)
				.ToList();

			if (!activePriorities.Any())
				return null;

			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, scanRange);
			foreach (var actor in actorsInRange)
			{
				// PERF: Most units can only attack enemy units. If this is the case but the target is not an enemy, we
				// can bail early and avoid the more expensive targeting checks and armament selection. For groups of
				// allied units, this helps significantly reduce the cost of auto target scans. This is important as
				// these groups will continuously rescan their allies until an enemy finally comes into range.
				if (attackStances == OpenRA.Traits.Stance.Enemy && !actor.AppearsHostileTo(self))
					continue;

				// Check whether we can auto-target this actor
				var targetTypes = actor.TraitsImplementing<ITargetable>()
					.Where(Exts.IsTraitEnabled).SelectMany(t => t.TargetTypes)
					.ToHashSet();

				var target = Target.FromActor(actor);
				var validPriorities = activePriorities.Where(ati =>
				{
					// Already have a higher priority target
					if (ati.Priority < chosenTargetPriority)
						return false;

					// Incompatible target types
					if (!targetTypes.Overlaps(ati.ValidTargets) || targetTypes.Overlaps(ati.InvalidTargets))
						return false;

					return true;
				}).ToList();

				if (!validPriorities.Any() || PreventsAutoTarget(self, actor) || !self.Owner.CanTargetActor(actor))
					continue;

				// Make sure that we can actually fire on the actor
				var armaments = ab.ChooseArmamentsForTarget(target, false);
				if (!allowMove)
					armaments = armaments.Where(arm =>
						target.IsInRange(self.CenterPosition, arm.MaxRange()) &&
						!target.IsInRange(self.CenterPosition, arm.Weapon.MinRange));

				if (!armaments.Any())
					continue;

				// Evaluate whether we want to target this actor
				var targetRange = (target.CenterPosition - self.CenterPosition).Length;
				foreach (var ati in validPriorities)
				{
					if (chosenTarget == null || chosenTargetPriority < ati.Priority
						|| (chosenTargetPriority == ati.Priority && targetRange < chosenTargetRange))
					{
						chosenTarget = actor;
						chosenTargetPriority = ati.Priority;
						chosenTargetRange = targetRange;
					}
				}
			}

			return chosenTarget;
		}

		bool PreventsAutoTarget(Actor attacker, Actor target)
		{
			foreach (var pat in target.TraitsImplementing<IPreventsAutoTarget>())
				if (pat.PreventsAutoTarget(target, attacker))
					return true;

			return false;
		}
	}

	public class StanceInit : IActorInit<UnitStance>
	{
		[FieldFromYamlKey] readonly UnitStance value = UnitStance.AttackAnything;
		public StanceInit() { }
		public StanceInit(UnitStance init) { value = init; }
		public UnitStance Value(World world) { return value; }
	}
}
