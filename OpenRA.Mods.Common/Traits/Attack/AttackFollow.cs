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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor will follow units until in range to attack them.")]
	public class AttackFollowInfo : AttackBaseInfo
	{
		[Desc("Automatically acquire and fire on targets of opportunity when not actively attacking.")]
		public readonly bool OpportunityFire = true;

		[Desc("Keep firing on targets even after attack order is cancelled")]
		public readonly bool PersistentTargeting = true;

		public override object Create(ActorInitializer init) { return new AttackFollow(init.Self, this); }
	}

	public class AttackFollow : AttackBase, INotifyOwnerChanged
	{
		public new readonly AttackFollowInfo Info;
		public Target RequestedTarget;
		public bool RequestedForceAttack;
		public int RequestedTargetLastTick;
		public Target OpportunityTarget;
		public bool OpportunityForceAttack;
		Mobile mobile;
		AutoTarget autoTarget;

		public AttackFollow(Actor self, AttackFollowInfo info)
			: base(self, info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			mobile = self.TraitOrDefault<Mobile>();
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
					if (TargetInFiringArc(self, target, Info.FacingTolerance))
						return true;

			return false;
		}

		protected override void Tick(Actor self)
		{
			if (IsTraitDisabled)
				RequestedTarget = OpportunityTarget = Target.Invalid;

			if (RequestedTargetLastTick != self.World.WorldTick)
			{
				// Activities tick before traits, so if we are here it means the activity didn't run
				// (either queued next or already cancelled) and we need to recalculate the target ourself
				bool targetIsHiddenActor;
				RequestedTarget = RequestedTarget.Recalculate(self.Owner, out targetIsHiddenActor);
			}

			// Can't fire on anything
			if (mobile != null && !mobile.CanInteractWithGroundLayer(self))
				return;

			if (RequestedTarget.Type != TargetType.Invalid)
			{
				IsAiming = CanAimAtTarget(self, RequestedTarget, RequestedForceAttack);
				if (IsAiming)
					DoAttack(self, RequestedTarget);
			}
			else
			{
				IsAiming = false;

				if (OpportunityTarget.Type != TargetType.Invalid)
					IsAiming = CanAimAtTarget(self, OpportunityTarget, OpportunityForceAttack);

				if (!IsAiming && Info.OpportunityFire && autoTarget != null &&
				    !autoTarget.IsTraitDisabled && autoTarget.Stance >= UnitStance.Defend)
				{
					OpportunityTarget = autoTarget.ScanForTarget(self, false, false);
					OpportunityForceAttack = false;

					if (OpportunityTarget.Type != TargetType.Invalid)
						IsAiming = CanAimAtTarget(self, OpportunityTarget, OpportunityForceAttack);
				}

				if (IsAiming)
					DoAttack(self, OpportunityTarget);
			}

			base.Tick(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new AttackActivity(self, newTarget, allowMove, forceAttack);
		}

		public override void OnQueueAttackActivity(Actor self, Target target, bool queued, bool allowMove, bool forceAttack)
		{
			// If not queued we know that the attack activity will run next
			// We can improve responsiveness for turreted actors by preempting
			// the last order (usually a move) and set the target immediately
			if (!queued)
			{
				RequestedTarget = target;
				RequestedForceAttack = forceAttack;
				RequestedTargetLastTick = self.World.WorldTick;
			}
		}

		public override void OnStopOrder(Actor self)
		{
			RequestedTarget = OpportunityTarget = Target.Invalid;
			base.OnStopOrder(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RequestedTarget = OpportunityTarget = Target.Invalid;
		}

		class AttackActivity : Activity
		{
			readonly AttackFollow attack;
			readonly RevealsShroud[] revealsShroud;
			readonly IMove move;
			readonly bool forceAttack;

			Target target;
			Target lastVisibleTarget;
			bool useLastVisibleTarget;
			WDist lastVisibleMaximumRange;
			WDist lastVisibleMinimumRange;
			bool wasMovingWithinRange;
			bool hasTicked;

			public AttackActivity(Actor self, Target target, bool allowMove, bool forceAttack)
			{
				attack = self.Trait<AttackFollow>();
				move = allowMove ? self.TraitOrDefault<IMove>() : null;
				revealsShroud = self.TraitsImplementing<RevealsShroud>().ToArray();

				this.target = target;
				this.forceAttack = forceAttack;

				// The target may become hidden between the initial order request and the first tick (e.g. if queued)
				// Moving to any position (even if quite stale) is still better than immediately giving up
				if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
				    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
				{
					lastVisibleTarget = Target.FromPos(target.CenterPosition);
					lastVisibleMaximumRange = attack.GetMaximumRangeVersusTarget(target);
					lastVisibleMinimumRange = attack.GetMinimumRangeVersusTarget(target);
				}
			}

			public override Activity Tick(Actor self)
			{
				if (ChildActivity != null)
				{
					ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
					if (ChildActivity != null)
						return this;
				}

				if (IsCanceling)
				{
					// Cancel the requested target, but keep firing on it while in range
					if (attack.Info.PersistentTargeting)
					{
						attack.OpportunityTarget = attack.RequestedTarget;
						attack.OpportunityForceAttack = attack.RequestedForceAttack;
					}

					attack.RequestedTarget = Target.Invalid;
					return NextActivity;
				}

				// Check that AttackFollow hasn't cancelled the target by modifying attack.Target
				// Having both this and AttackFollow modify that field is a horrible hack.
				if (hasTicked && attack.RequestedTarget.Type == TargetType.Invalid)
					return NextActivity;

				if (attack.IsTraitPaused)
					return this;

				bool targetIsHiddenActor;
				attack.RequestedForceAttack = forceAttack;
				attack.RequestedTarget = target = target.Recalculate(self.Owner, out targetIsHiddenActor);
				attack.RequestedTargetLastTick = self.World.WorldTick;
				hasTicked = true;

				if (!targetIsHiddenActor && target.Type == TargetType.Actor)
				{
					lastVisibleTarget = Target.FromTargetPositions(target);
					lastVisibleMaximumRange = attack.GetMaximumRangeVersusTarget(target);
					lastVisibleMinimumRange = attack.GetMinimumRange();

					// Try and sit at least one cell away from the min or max ranges to give some leeway if the target starts moving.
					if (move != null && target.Actor.Info.HasTraitInfo<IMoveInfo>())
					{
						var preferMinRange = Math.Min(lastVisibleMinimumRange.Length + 1024, lastVisibleMaximumRange.Length);
						var preferMaxRange = Math.Max(lastVisibleMaximumRange.Length - 1024, lastVisibleMinimumRange.Length);
						lastVisibleMaximumRange = new WDist((lastVisibleMaximumRange.Length - 1024).Clamp(preferMinRange, preferMaxRange));
					}
				}

				var oldUseLastVisibleTarget = useLastVisibleTarget;
				var maxRange = lastVisibleMaximumRange;
				var minRange = lastVisibleMinimumRange;
				useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

				// Most actors want to be able to see their target before shooting
				if (target.Type == TargetType.FrozenActor && !attack.Info.TargetFrozenActors && !forceAttack)
				{
					var rs = revealsShroud
						.Where(Exts.IsTraitEnabled)
						.MaxByOrDefault(s => s.Range);

					// Default to 2 cells if there are no active traits
					var sightRange = rs != null ? rs.Range : WDist.FromCells(2);
					if (sightRange < maxRange)
						maxRange = sightRange;
				}

				// If we are ticking again after previously sequencing a MoveWithRange then that move must have completed
				// Either we are in range and can see the target, or we've lost track of it and should give up
				if (wasMovingWithinRange && targetIsHiddenActor)
				{
					attack.RequestedTarget = Target.Invalid;
					return NextActivity;
				}

				// Update target lines if required
				if (useLastVisibleTarget != oldUseLastVisibleTarget)
					self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, Color.Red, false);

				// Target is hidden or dead, and we don't have a fallback position to move towards
				if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				{
					attack.RequestedTarget = Target.Invalid;
					return NextActivity;
				}

				var pos = self.CenterPosition;
				var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

				// We've reached the required range - if the target is visible and valid then we wait
				// otherwise if it is hidden or dead we give up
				if (checkTarget.IsInRange(pos, maxRange) && !checkTarget.IsInRange(pos, minRange))
				{
					if (useLastVisibleTarget)
					{
						attack.RequestedTarget = Target.Invalid;
						return NextActivity;
					}

					return this;
				}

				// We can't move into range, so give up
				if (move == null || maxRange == WDist.Zero || maxRange < minRange)
				{
					attack.RequestedTarget = Target.Invalid;
					return NextActivity;
				}

				wasMovingWithinRange = true;
				QueueChild(self, move.MoveWithinRange(target, minRange, maxRange, checkTarget.CenterPosition, Color.Red), true);
				return this;
			}
		}
	}
}
