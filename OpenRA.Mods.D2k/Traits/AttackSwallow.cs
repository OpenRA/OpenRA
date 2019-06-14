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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Sandworms use this attack model.")]
	class AttackSwallowInfo : AttackBaseInfo
	{
		[Desc("The number of ticks it takes to return underground.")]
		public readonly int ReturnDelay = 60;

		[Desc("The number of ticks it takes to get in place under the target to attack.")]
		public readonly int AttackDelay = 30;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while attacking.")]
		public readonly string AttackingCondition = null;

		public readonly string WormAttackSound = "WORM.WAV";

		[NotificationReference("Speech")]
		public readonly string WormAttackNotification = "WormAttack";

		public override object Create(ActorInitializer init) { return new AttackSwallow(init.Self, this); }
	}

	class AttackSwallow : AttackBase
	{
		public readonly new AttackSwallowInfo Info;

		public AttackSwallow(Actor self, AttackSwallowInfo info)
			: base(self, info)
		{
			Info = info;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			return TargetInFiringArc(self, target, Info.FacingTolerance);
		}

		public override void DoAttack(Actor self, Target target)
		{
			// This is so that the worm does not launch an attack against a target that has reached solid rock
			if (target.Type != TargetType.Actor || !CanAttack(self, target))
			{
				self.CancelActivity();
				return;
			}

			var a = ChooseArmamentsForTarget(target, true).FirstOrDefault();
			if (a == null)
				return;

			if (!target.IsInRange(self.CenterPosition, a.MaxRange()))
				return;

			self.CancelActivity();
			self.QueueActivity(new SwallowActor(self, target, a, facing));
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new SwallowTarget(self, newTarget, allowMove, forceAttack);
		}

		public class SwallowTarget : Activity, IActivityNotifyStanceChanged
		{
			[Flags]
			protected enum AttackStatus { UnableToAttack, NeedsToTurn, NeedsToMove, Attacking }

			readonly AttackFollow[] attackTraits;
			readonly RevealsShroud[] revealsShroud;
			readonly IMove move;
			readonly IFacing facing;
			readonly IPositionable positionable;
			readonly bool forceAttack;

			protected Target target;
			Target lastVisibleTarget;
			WDist lastVisibleMaximumRange;
			BitSet<TargetableType> lastVisibleTargetTypes;
			Player lastVisibleOwner;
			bool useLastVisibleTarget;

			WDist minRange;
			WDist maxRange;
			AttackStatus attackStatus = AttackStatus.UnableToAttack;

			public SwallowTarget(Actor self, Target target, bool allowMovement, bool forceAttack)
			{
				this.target = target;
				this.forceAttack = forceAttack;

				attackTraits = self.TraitsImplementing<AttackFollow>().ToArray();
				revealsShroud = self.TraitsImplementing<RevealsShroud>().ToArray();
				facing = self.Trait<IFacing>();
				positionable = self.Trait<IPositionable>();

				move = allowMovement ? self.TraitOrDefault<IMove>() : null;

				// The target may become hidden between the initial order request and the first tick (e.g. if queued)
				// Moving to any position (even if quite stale) is still better than immediately giving up
				if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
					|| target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
				{
					lastVisibleTarget = Target.FromPos(target.CenterPosition);
					lastVisibleMaximumRange = attackTraits.Where(x => !x.IsTraitDisabled)
						.Min(x => x.GetMaximumRangeVersusTarget(target));

					if (target.Type == TargetType.Actor)
					{
						lastVisibleOwner = target.Actor.Owner;
						lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();
					}
					else if (target.Type == TargetType.FrozenActor)
					{
						lastVisibleOwner = target.FrozenActor.Owner;
						lastVisibleTargetTypes = target.FrozenActor.TargetTypes;
					}
				}
			}

			protected Target RecalculateTarget(Actor self, out bool targetIsHiddenActor)
			{
				// Worms ignore visibility, so don't need to recalculate targets
				targetIsHiddenActor = false;
				return target;
			}

			public override bool Tick(Actor self)
			{
				if (IsCanceling)
					return true;

				if (target.Type == TargetType.Actor)
				{
					lastVisibleTarget = Target.FromTargetPositions(target);
					lastVisibleMaximumRange = attackTraits.Where(x => !x.IsTraitDisabled)
						.Min(x => x.GetMaximumRangeVersusTarget(target));

					lastVisibleOwner = target.Actor.Owner;
					lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();
				}

				useLastVisibleTarget = !target.IsValidFor(self);

				// Target is hidden or dead, and we don't have a fallback position to move towards
				if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
					return true;

				var pos = self.CenterPosition;
				var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

				// We don't know where the target actually is, so move to where we last saw it
				if (useLastVisibleTarget)
				{
					// We've reached the assumed position but it is not there or we can't move any further - give up
					if (checkTarget.IsInRange(pos, lastVisibleMaximumRange) || move == null || lastVisibleMaximumRange == WDist.Zero)
						return true;

					// Move towards the last known position
					QueueChild(move.MoveWithinRange(target, WDist.Zero, lastVisibleMaximumRange, checkTarget.CenterPosition, Color.Red));
					return false;
				}

				attackStatus = AttackStatus.UnableToAttack;

				foreach (var attack in attackTraits.Where(x => !x.IsTraitDisabled))
				{
					var status = TickAttack(self, attack);
					attack.IsAiming = status == AttackStatus.Attacking || status == AttackStatus.NeedsToTurn;
				}

				if (attackStatus >= AttackStatus.NeedsToTurn)
					return false;

				return true;
			}

			protected virtual AttackStatus TickAttack(Actor self, AttackFollow attack)
			{
				if (!target.IsValidFor(self))
					return AttackStatus.UnableToAttack;

				if (attack.Info.AttackRequiresEnteringCell && !positionable.CanEnterCell(target.Actor.Location, null, false))
					return AttackStatus.UnableToAttack;

				if (!attack.Info.TargetFrozenActors && !forceAttack && target.Type == TargetType.FrozenActor)
				{
					// Try to move within range, drop the target otherwise
					if (move == null)
						return AttackStatus.UnableToAttack;

					var rs = revealsShroud
						.Where(Exts.IsTraitEnabled)
						.MaxByOrDefault(s => s.Range);

					// Default to 2 cells if there are no active traits
					var sightRange = rs != null ? rs.Range : WDist.FromCells(2);

					attackStatus |= AttackStatus.NeedsToMove;
					QueueChild(move.MoveWithinRange(target, sightRange, target.CenterPosition, Color.Red));
					return AttackStatus.NeedsToMove;
				}

				// Drop the target once none of the weapons are effective against it
				var armaments = attack.ChooseArmamentsForTarget(target, forceAttack).ToList();
				if (armaments.Count == 0)
					return AttackStatus.UnableToAttack;

				// Update ranges
				minRange = armaments.Max(a => a.Weapon.MinRange);
				maxRange = armaments.Min(a => a.MaxRange());

				var pos = self.CenterPosition;
				var mobile = move as Mobile;
				if (!target.IsInRange(pos, maxRange)
					|| (minRange.Length != 0 && target.IsInRange(pos, minRange))
					|| (mobile != null && !mobile.CanInteractWithGroundLayer(self)))
				{
					// Try to move within range, drop the target otherwise
					if (move == null)
						return AttackStatus.UnableToAttack;

					attackStatus |= AttackStatus.NeedsToMove;
					var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;
					QueueChild(move.MoveWithinRange(target, minRange, maxRange, checkTarget.CenterPosition, Color.Red));
					return AttackStatus.NeedsToMove;
				}

				if (!attack.TargetInFiringArc(self, target, attack.Info.FacingTolerance))
				{
					var desiredFacing = (attack.GetTargetPosition(pos, target) - pos).Yaw.Facing;
					attackStatus |= AttackStatus.NeedsToTurn;
					QueueChild(new Turn(self, desiredFacing));
					return AttackStatus.NeedsToTurn;
				}

				attackStatus |= AttackStatus.Attacking;
				attack.DoAttack(self, target);

				return AttackStatus.Attacking;
			}

			void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
			{
				// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
				if (newStance > oldStance || forceAttack)
					return;

				if (!autoTarget.HasValidTargetPriority(self, lastVisibleOwner, lastVisibleTargetTypes))
					target = Target.Invalid;
			}
		}
	}
}
