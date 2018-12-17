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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum UnitStance { HoldFire, ReturnFire, Defend, AttackAnything }

	[Desc("The actor will automatically engage the enemy when it is in range.")]
	public class AutoTargetInfo : ConditionalTraitInfo, Requires<AttackBaseInfo>, IEditorActorOptions
	{
		[Desc("It will try to hunt down the enemy if it is set to AttackAnything.")]
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

		[Desc("Display order for the stance dropdown in the map editor")]
		public readonly int EditorStanceDisplayOrder = 1;

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

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			// Indexed by UnitStance
			var stances = new[] { "holdfire", "returnfire", "defend", "attackanything" };

			var labels = new Dictionary<string, string>()
			{
				{ "holdfire", "Hold Fire" },
				{ "returnfire", "Return Fire" },
				{ "defend", "Defend" },
				{ "attackanything", "Attack Anything" },
			};

			yield return new EditorActorDropdown("Stance", EditorStanceDisplayOrder, labels,
				actor =>
				{
					var init = actor.Init<StanceInit>();
					var stance = init != null ? init.Value(world) : InitialStance;
					return stances[(int)stance];
				},
				(actor, value) => actor.ReplaceInit(new StanceInit((UnitStance)stances.IndexOf(value))));
		}
	}

	public class AutoTarget : ConditionalTrait<AutoTargetInfo>, INotifyIdle, INotifyDamage, ITick, IResolveOrder, ISync, INotifyCreated
	{
		readonly IEnumerable<AttackBase> activeAttackBases;
		readonly AttackFollow[] attackFollows;
		[Sync] int nextScanTime = 0;

		public UnitStance Stance { get { return stance; } }

		[Sync] public Actor Aggressor;

		// NOT SYNCED: do not refer to this anywhere other than UI code
		public UnitStance PredictedStance;

		UnitStance stance;
		ConditionManager conditionManager;
		IEnumerable<AutoTargetPriorityInfo> activeTargetPriorities;
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
			// AutoTargetPriority and their Priorities are fixed - so we can safely cache them with ToArray.
			// IsTraitEnabled can change over time, and so must appear after the ToArray so it gets re-evaluated each time.
			activeTargetPriorities =
				self.TraitsImplementing<AutoTargetPriority>()
				.OrderByDescending(ati => ati.Info.Priority).ToArray()
				.Where(Exts.IsTraitEnabled).Select(atp => atp.Info);

			conditionManager = self.TraitOrDefault<ConditionManager>();
			ApplyStanceCondition(self);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetUnitStance" && Info.EnableStances)
				SetStance(self, (UnitStance)order.ExtraData);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || !self.IsIdle || Stance < UnitStance.ReturnFire)
				return;

			// Don't retaliate against healers
			if (e.Damage.Value < 0)
				return;

			var attacker = e.Attacker;
			if (attacker.Disposed)
				return;

			if (!attacker.IsInWorld)
			{
				// If the aggressor is in a transport, then attack the transport instead
				var passenger = attacker.TraitOrDefault<Passenger>();
				if (passenger != null && passenger.Transport != null)
					attacker = passenger.Transport;
			}

			// Not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			var attackerAsTarget = Target.FromActor(attacker);
			if (!activeAttackBases.Any(a => a.HasAnyValidWeapons(attackerAsTarget)))
				return;

			// Don't retaliate against own units force-firing on us. It's usually not what the player wanted.
			if (attacker.AppearsFriendlyTo(self))
				return;

			Aggressor = attacker;

			bool allowMove;
			if (ShouldAttack(out allowMove))
				Attack(self, Target.FromActor(Aggressor), allowMove);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled || Stance < UnitStance.Defend)
				return;

			bool allowMove;
			if (ShouldAttack(out allowMove))
				ScanAndAttack(self, allowMove);
		}

		bool ShouldAttack(out bool allowMove)
		{
			allowMove = Info.AllowMovement && Stance > UnitStance.Defend;

			// PERF: Avoid LINQ.
			foreach (var attackFollow in attackFollows)
				if (!attackFollow.IsTraitDisabled && attackFollow.IsReachableTarget(attackFollow.Target, allowMove))
					return false;

			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (nextScanTime > 0)
				--nextScanTime;
		}

		public Target ScanForTarget(Actor self, bool allowMove)
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

			return Target.Invalid;
		}

		public void ScanAndAttack(Actor self, bool allowMove)
		{
			var target = ScanForTarget(self, allowMove);
			if (target.Type != TargetType.Invalid)
				Attack(self, target, allowMove);
		}

		void Attack(Actor self, Target target, bool allowMove)
		{
			self.SetTargetLine(target, Color.Red, false);

			foreach (var ab in activeAttackBases)
				ab.AttackTarget(target, false, allowMove);
		}

		Target ChooseTarget(Actor self, AttackBase ab, Stance attackStances, WDist scanRange, bool allowMove)
		{
			var chosenTarget = Target.Invalid;
			var chosenTargetPriority = int.MinValue;
			int chosenTargetRange = 0;

			var activePriorities = activeTargetPriorities.ToList();
			if (activePriorities.Count == 0)
				return chosenTarget;

			var targetsInRange = self.World.FindActorsInCircle(self.CenterPosition, scanRange)
				.Select(Target.FromActor)
				.Concat(self.Owner.FrozenActorLayer.FrozenActorsInCircle(self.World, self.CenterPosition, scanRange)
					.Select(Target.FromFrozenActor));

			foreach (var target in targetsInRange)
			{
				BitSet<TargetableType> targetTypes;
				if (target.Type == TargetType.Actor)
				{
					// PERF: Most units can only attack enemy units. If this is the case but the target is not an enemy, we
					// can bail early and avoid the more expensive targeting checks and armament selection. For groups of
					// allied units, this helps significantly reduce the cost of auto target scans. This is important as
					// these groups will continuously rescan their allies until an enemy finally comes into range.
					if (attackStances == OpenRA.Traits.Stance.Enemy && !target.Actor.AppearsHostileTo(self))
						continue;

					// Check whether we can auto-target this actor
					targetTypes = target.Actor.GetEnabledTargetTypes();

					if (PreventsAutoTarget(self, target.Actor) || !target.Actor.CanBeViewedByPlayer(self.Owner))
						continue;
				}
				else if (target.Type == TargetType.FrozenActor)
				{
					if (attackStances == OpenRA.Traits.Stance.Enemy && self.Owner.Stances[target.FrozenActor.Owner] == OpenRA.Traits.Stance.Ally)
						continue;

					targetTypes = target.FrozenActor.TargetTypes;
				}
				else
					continue;

				var validPriorities = activePriorities.Where(ati =>
				{
					// Already have a higher priority target
					if (ati.Priority < chosenTargetPriority)
						return false;

					// Incompatible target types
					if (!ati.ValidTargets.Overlaps(targetTypes) || ati.InvalidTargets.Overlaps(targetTypes))
						return false;

					return true;
				}).ToList();

				if (validPriorities.Count == 0)
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
					if (chosenTarget.Type == TargetType.Invalid || chosenTargetPriority < ati.Priority
						|| (chosenTargetPriority == ati.Priority && targetRange < chosenTargetRange))
					{
						chosenTarget = target;
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
