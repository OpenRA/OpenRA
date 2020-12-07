#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum UnitStance { HoldFire, ReturnFire, Defend, AttackAnything }

	[RequireExplicitImplementation]
	public interface IActivityNotifyStanceChanged : IActivityInterface
	{
		void StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance);
	}

	[RequireExplicitImplementation]
	public interface INotifyStanceChanged
	{
		void StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance);
	}

	[Desc("The actor will automatically engage the enemy when it is in range.")]
	public class AutoTargetInfo : ConditionalTraitInfo, Requires<AttackBaseInfo>, IEditorActorOptions
	{
		[Desc("It will try to hunt down the enemy if it is set to AttackAnything.")]
		public readonly bool AllowMovement = true;

		[Desc("It will try to pivot to face the enemy if stance is not HoldFire.")]
		public readonly bool AllowTurning = true;

		[Desc("Scan for new targets when idle.")]
		public readonly bool ScanOnIdle = true;

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
					var init = actor.GetInitOrDefault<StanceInit>(this);
					var stance = init != null ? init.Value : InitialStance;
					return stances[(int)stance];
				},
				(actor, value) => actor.ReplaceInit(new StanceInit(this, (UnitStance)stances.IndexOf(value))));
		}
	}

	public class AutoTarget : ConditionalTrait<AutoTargetInfo>, INotifyIdle, INotifyDamage, ITick, IResolveOrder, ISync, INotifyOwnerChanged
	{
		public readonly IEnumerable<AttackBase> ActiveAttackBases;

		readonly bool allowMovement;

		[Sync]
		int nextScanTime = 0;

		public UnitStance Stance { get { return stance; } }

		[Sync]
		public Actor Aggressor;

		// NOT SYNCED: do not refer to this anywhere other than UI code
		public UnitStance PredictedStance;

		UnitStance stance;
		IDisableAutoTarget[] disableAutoTarget;
		INotifyStanceChanged[] notifyStanceChanged;
		IEnumerable<AutoTargetPriorityInfo> activeTargetPriorities;
		int conditionToken = Actor.InvalidConditionToken;

		public void SetStance(Actor self, UnitStance value)
		{
			if (stance == value)
				return;

			var oldStance = stance;
			stance = value;
			ApplyStanceCondition(self);

			foreach (var nsc in notifyStanceChanged)
				nsc.StanceChanged(self, this, oldStance, stance);

			if (self.CurrentActivity != null)
				foreach (var a in self.CurrentActivity.ActivitiesImplementing<IActivityNotifyStanceChanged>())
					a.StanceChanged(self, this, oldStance, stance);
		}

		void ApplyStanceCondition(Actor self)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (Info.ConditionByStance.TryGetValue(stance, out var condition))
				conditionToken = self.GrantCondition(condition);
		}

		public AutoTarget(ActorInitializer init, AutoTargetInfo info)
			: base(info)
		{
			var self = init.Self;
			ActiveAttackBases = self.TraitsImplementing<AttackBase>().ToArray().Where(Exts.IsTraitEnabled);

			stance = init.GetValue<StanceInit, UnitStance>(self.Owner.IsBot || !self.Owner.Playable ? info.InitialStanceAI : info.InitialStance);

			PredictedStance = stance;

			allowMovement = Info.AllowMovement && self.TraitOrDefault<IMove>() != null;
		}

		protected override void Created(Actor self)
		{
			// AutoTargetPriority and their Priorities are fixed - so we can safely cache them with ToArray.
			// IsTraitEnabled can change over time, and so must appear after the ToArray so it gets re-evaluated each time.
			activeTargetPriorities =
				self.TraitsImplementing<AutoTargetPriority>()
					.OrderByDescending(ati => ati.Info.Priority).ToArray()
					.Where(Exts.IsTraitEnabled).Select(atp => atp.Info);

			disableAutoTarget = self.TraitsImplementing<IDisableAutoTarget>().ToArray();
			notifyStanceChanged = self.TraitsImplementing<INotifyStanceChanged>().ToArray();
			ApplyStanceCondition(self);

			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			PredictedStance = self.Owner.IsBot || !self.Owner.Playable ? Info.InitialStanceAI : Info.InitialStance;
			SetStance(self, PredictedStance);
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

			foreach (var dat in disableAutoTarget)
				if (dat.DisableAutoTarget(self))
					return;

			if (!attacker.IsInWorld)
			{
				// If the aggressor is in a transport, then attack the transport instead
				var passenger = attacker.TraitOrDefault<Passenger>();
				if (passenger != null && passenger.Transport != null)
					attacker = passenger.Transport;
			}

			// Don't fire at an invisible enemy when we can't move to reveal it
			var allowMove = allowMovement && Stance > UnitStance.Defend;
			if (!allowMove && !attacker.CanBeViewedByPlayer(self.Owner))
				return;

			// Not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			var attackerAsTarget = Target.FromActor(attacker);
			if (!ActiveAttackBases.Any(a => a.HasAnyValidWeapons(attackerAsTarget)))
				return;

			// Don't retaliate against own units force-firing on us. It's usually not what the player wanted.
			if (attacker.AppearsFriendlyTo(self))
				return;

			Aggressor = attacker;

			Attack(self, Target.FromActor(Aggressor), allowMove);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled || !Info.ScanOnIdle || Stance < UnitStance.Defend)
				return;

			var allowMove = allowMovement && Stance > UnitStance.Defend;
			var allowTurn = Info.AllowTurning && Stance > UnitStance.HoldFire;
			ScanAndAttack(self, allowMove, allowTurn);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (nextScanTime > 0)
				--nextScanTime;
		}

		public Target ScanForTarget(Actor self, bool allowMove, bool allowTurn, bool ignoreScanInterval = false)
		{
			if ((ignoreScanInterval || nextScanTime <= 0) && ActiveAttackBases.Any())
			{
				foreach (var dat in disableAutoTarget)
					if (dat.DisableAutoTarget(self))
						return Target.Invalid;

				if (!ignoreScanInterval)
					nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);

				foreach (var ab in ActiveAttackBases)
				{
					// If we can't attack right now, there's no need to try and find a target.
					var attackStances = ab.UnforcedAttackTargetStances();
					if (attackStances != OpenRA.Traits.PlayerRelationship.None)
					{
						var range = Info.ScanRadius > 0 ? WDist.FromCells(Info.ScanRadius) : ab.GetMaximumRange();
						return ChooseTarget(self, ab, attackStances, range, allowMove, allowTurn);
					}
				}
			}

			return Target.Invalid;
		}

		public void ScanAndAttack(Actor self, bool allowMove, bool allowTurn)
		{
			var target = ScanForTarget(self, allowMove, allowTurn);
			if (target.Type != TargetType.Invalid)
				Attack(self, target, allowMove);
		}

		void Attack(Actor self, in Target target, bool allowMove)
		{
			foreach (var ab in ActiveAttackBases)
				ab.AttackTarget(target, AttackSource.AutoTarget, false, allowMove);
		}

		public bool HasValidTargetPriority(Actor self, Player owner, BitSet<TargetableType> targetTypes)
		{
			if (owner == null || Stance <= UnitStance.ReturnFire)
				return false;

			return activeTargetPriorities.Any(ati =>
			{
				// Incompatible stances
				if (!ati.ValidRelationships.HasStance(self.Owner.RelationshipWith(owner)))
					return false;

				// Incompatible target types
				if (!ati.ValidTargets.Overlaps(targetTypes) || ati.InvalidTargets.Overlaps(targetTypes))
					return false;

				return true;
			});
		}

		Target ChooseTarget(Actor self, AttackBase ab, PlayerRelationship attackStances, WDist scanRange, bool allowMove, bool allowTurn)
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
				Player owner;
				if (target.Type == TargetType.Actor)
				{
					// PERF: Most units can only attack enemy units. If this is the case but the target is not an enemy, we
					// can bail early and avoid the more expensive targeting checks and armament selection. For groups of
					// allied units, this helps significantly reduce the cost of auto target scans. This is important as
					// these groups will continuously rescan their allies until an enemy finally comes into range.
					if (attackStances == PlayerRelationship.Enemy && !target.Actor.AppearsHostileTo(self))
						continue;

					// Check whether we can auto-target this actor
					targetTypes = target.Actor.GetEnabledTargetTypes();

					if (PreventsAutoTarget(self, target.Actor) || !target.Actor.CanBeViewedByPlayer(self.Owner))
						continue;

					owner = target.Actor.Owner;
				}
				else if (target.Type == TargetType.FrozenActor)
				{
					if (attackStances == PlayerRelationship.Enemy && self.Owner.RelationshipWith(target.FrozenActor.Owner) == PlayerRelationship.Ally)
						continue;

					targetTypes = target.FrozenActor.TargetTypes;
					owner = target.FrozenActor.Owner;
				}
				else
					continue;

				var validPriorities = activePriorities.Where(ati =>
				{
					// Already have a higher priority target
					if (ati.Priority < chosenTargetPriority)
						return false;

					// Incompatible stances
					if (!ati.ValidRelationships.HasStance(self.Owner.RelationshipWith(owner)))
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

				if (!allowTurn && !ab.TargetInFiringArc(self, target, ab.Info.FacingTolerance))
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
			foreach (var deat in target.TraitsImplementing<IDisableEnemyAutoTarget>())
				if (deat.DisableEnemyAutoTarget(target, attacker))
					return true;

			return false;
		}
	}

	public class StanceInit : ValueActorInit<UnitStance>, ISingleInstanceInit
	{
		public StanceInit(TraitInfo info, UnitStance value)
			: base(info, value) { }
	}
}
