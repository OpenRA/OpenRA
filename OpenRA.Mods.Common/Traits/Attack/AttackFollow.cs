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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// TODO: Add CurleyShuffle (TD, TS), Circle (Generals Gunship-style)
	public enum AttackType { Follow, Strafe }

	[Desc("Actor will follow units until in range to attack them.")]
	public class AttackFollowInfo : AttackBaseInfo
	{
		[Desc("Automatically acquire and fire on targets of opportunity when not actively attacking.")]
		public readonly bool OpportunityFire = true;

		[Desc("Keep firing on targets even after attack order is cancelled")]
		public readonly bool PersistentTargeting = true;

		[Desc("Can attack and move at the same time.")]
		public readonly bool FireWhileMoving = true;

		[Desc("Attack behavior. Currently supported types are Follow (default) and Strafe.")]
		public readonly AttackType AttackType = AttackType.Follow;

		[Desc("Delay, in game ticks, before strafing actor turns to attack.")]
		public readonly int AttackTurnDelay = 50;

		[Desc("Does this actor cancel its attack activity when it needs to resupply? Setting this" +
			" to 'false' will make the actor resume attack after reloading.")]
		public readonly bool AbortOnResupply = true;

		public override object Create(ActorInitializer init) { return new AttackFollow(init.Self, this); }
	}

	public class AttackFollow : AttackBase, INotifyOwnerChanged, IDisableAutoTarget, INotifyStanceChanged
	{
		public new readonly AttackFollowInfo Info;
		public Target RequestedTarget { get; private set; }
		public Target OpportunityTarget { get; private set; }

		public IMove Move;
		public Mobile Mobile;
		public Aircraft Aircraft;

		AutoTarget autoTarget;
		bool requestedForceAttack;
		Activity requestedTargetPresetForActivity;
		bool opportunityForceAttack;
		bool opportunityTargetIsPersistentTarget;

		public void SetRequestedTarget(Actor self, Target target, bool isForceAttack = false)
		{
			RequestedTarget = target;
			requestedForceAttack = isForceAttack;
			requestedTargetPresetForActivity = null;
		}

		public void ClearRequestedTarget()
		{
			if (Info.PersistentTargeting)
			{
				OpportunityTarget = RequestedTarget;
				opportunityForceAttack = requestedForceAttack;
				opportunityTargetIsPersistentTarget = true;
			}

			RequestedTarget = Target.Invalid;
			requestedTargetPresetForActivity = null;
		}

		public AttackFollow(Actor self, AttackFollowInfo info)
			: base(self, info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			Move = self.TraitOrDefault<IMove>();
			Mobile = self.TraitOrDefault<Mobile>();
			Aircraft = self.TraitOrDefault<Aircraft>();
			autoTarget = self.TraitOrDefault<AutoTarget>();
			base.Created(self);
		}

		protected bool CanAimAtTarget(Actor self, Target target, bool forceAttack)
		{
			if (target.Type == TargetType.Actor && !target.Actor.CanBeViewedByPlayer(self.Owner))
				return false;

			if (target.Type == TargetType.FrozenActor && !target.FrozenActor.IsValid)
				return false;

			var pos = self.CenterPosition;
			var armaments = ChooseArmamentsForTarget(target, forceAttack);
			foreach (var a in armaments)
				if (target.IsInRange(pos, a.MaxRange()) && (a.Weapon.MinRange == WDist.Zero || !target.IsInRange(pos, a.Weapon.MinRange)))
					if (TargetInFiringArc(self, target, Info.FacingTolerance) && (Info.FireWhileMoving || !Move.CurrentMovementTypes.HasFlag(MovementType.Horizontal)))
						return true;

			return false;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			// Aircraft don't fire while landed or when outside the map.
			if (Aircraft != null && (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < Aircraft.Info.MinAirborneAltitude
				|| !self.World.Map.Contains(self.Location)))
				return false;

			if (!base.CanAttack(self, target))
				return false;

			if (!Info.FireWhileMoving && Move.CurrentMovementTypes.HasFlag(MovementType.Horizontal))
				return false;

			return TargetInFiringArc(self, target, Info.FacingTolerance);
		}

		protected override void Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				RequestedTarget = OpportunityTarget = Target.Invalid;
				opportunityTargetIsPersistentTarget = false;
			}

			if (requestedTargetPresetForActivity != null)
			{
				// RequestedTarget was set by OnQueueAttackActivity in preparation for a queued activity
				// requestedTargetPresetForActivity will be cleared once the activity starts running and calls UpdateRequestedTarget
				if (self.CurrentActivity != null && self.CurrentActivity.NextActivity == requestedTargetPresetForActivity)
				{
					bool targetIsHiddenActor;
					RequestedTarget = RequestedTarget.Recalculate(self.Owner, out targetIsHiddenActor);
				}

				// Requested activity has been canceled
				else
					ClearRequestedTarget();
			}

			// Can't fire on anything
			if (Mobile != null && !Mobile.CanInteractWithGroundLayer(self))
				return;

			if (RequestedTarget.Type != TargetType.Invalid)
			{
				IsAiming = CanAimAtTarget(self, RequestedTarget, requestedForceAttack);
				if (IsAiming)
					DoAttack(self, RequestedTarget);
			}
			else
			{
				IsAiming = false;

				if (OpportunityTarget.Type != TargetType.Invalid)
					IsAiming = CanAimAtTarget(self, OpportunityTarget, opportunityForceAttack);

				if (!IsAiming && Info.OpportunityFire && autoTarget != null &&
				    !autoTarget.IsTraitDisabled && autoTarget.Stance >= UnitStance.Defend)
				{
					OpportunityTarget = autoTarget.ScanForTarget(self, false, false);
					opportunityForceAttack = false;
					opportunityTargetIsPersistentTarget = false;

					if (OpportunityTarget.Type != TargetType.Invalid)
						IsAiming = CanAimAtTarget(self, OpportunityTarget, opportunityForceAttack);
				}

				if (IsAiming)
					DoAttack(self, OpportunityTarget);
			}

			base.Tick(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			if (Aircraft != null)
				return new FlyAttack(self, newTarget, forceAttack, targetLineColor);

			return new AttackActivity(self, newTarget, allowMove, forceAttack, targetLineColor);
		}

		public override void OnResolveAttackOrder(Actor self, Activity activity, Target target, bool queued, bool forceAttack)
		{
			// We can improve responsiveness for turreted actors by preempting
			// the last order (usually a move) and setting the target immediately
			if (!queued)
			{
				RequestedTarget = target;
				requestedForceAttack = forceAttack;
				requestedTargetPresetForActivity = activity;
			}
		}

		public override void OnStopOrder(Actor self)
		{
			RequestedTarget = OpportunityTarget = Target.Invalid;
			opportunityTargetIsPersistentTarget = false;
			base.OnStopOrder(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RequestedTarget = OpportunityTarget = Target.Invalid;
			opportunityTargetIsPersistentTarget = false;
		}

		bool IDisableAutoTarget.DisableAutoTarget(Actor self)
		{
			return RequestedTarget.Type != TargetType.Invalid ||
				(opportunityTargetIsPersistentTarget && OpportunityTarget.Type != TargetType.Invalid);
		}

		void INotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel opportunity targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance || opportunityForceAttack)
				return;

			if (OpportunityTarget.Type == TargetType.Actor)
			{
				var a = OpportunityTarget.Actor;
				if (!autoTarget.HasValidTargetPriority(self, a.Owner, a.GetEnabledTargetTypes()))
					OpportunityTarget = Target.Invalid;
			}
			else if (OpportunityTarget.Type == TargetType.FrozenActor)
			{
				var fa = OpportunityTarget.FrozenActor;
				if (!autoTarget.HasValidTargetPriority(self, fa.Owner, fa.TargetTypes))
					OpportunityTarget = Target.Invalid;
			}
		}
	}
}
