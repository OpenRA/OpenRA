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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	/* non-turreted attack */
	public class Attack : Activity
	{
		[Flags]
		protected enum AttackStatus { UnableToAttack, NeedsToTurn, NeedsToMove, Attacking }

		readonly AttackFrontal[] attackTraits;
		readonly RevealsShroud[] revealsShroud;
		readonly IMove move;
		readonly IFacing facing;
		readonly IPositionable positionable;
		readonly bool forceAttack;
		protected Target target;

		WDist minRange;
		WDist maxRange;
		Activity turnActivity;
		Activity moveActivity;
		AttackStatus attackStatus = AttackStatus.UnableToAttack;

		public Attack(Actor self, Target target, bool allowMovement, bool forceAttack)
		{
			this.target = target;
			this.forceAttack = forceAttack;

			attackTraits = self.TraitsImplementing<AttackFrontal>().ToArray();
			revealsShroud = self.TraitsImplementing<RevealsShroud>().ToArray();
			facing = self.Trait<IFacing>();
			positionable = self.Trait<IPositionable>();

			move = allowMovement ? self.TraitOrDefault<IMove>() : null;
		}

		protected virtual Target RecalculateTarget(Actor self)
		{
			return target.Recalculate(self.Owner);
		}

		public override Activity Tick(Actor self)
		{
			target = RecalculateTarget(self);
			turnActivity = moveActivity = null;
			attackStatus = AttackStatus.UnableToAttack;

			foreach (var attack in attackTraits.Where(x => !x.IsTraitDisabled))
			{
				var status = TickAttack(self, attack);
				attack.IsAiming = status == AttackStatus.Attacking || status == AttackStatus.NeedsToTurn;
			}

			if (attackStatus.HasFlag(AttackStatus.Attacking))
				return this;

			if (attackStatus.HasFlag(AttackStatus.NeedsToTurn))
				return turnActivity;

			if (attackStatus.HasFlag(AttackStatus.NeedsToMove))
				return moveActivity;

			return NextActivity;
		}

		protected virtual AttackStatus TickAttack(Actor self, AttackFrontal attack)
		{
			if (IsCanceled)
				return AttackStatus.UnableToAttack;

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
				moveActivity = ActivityUtils.SequenceActivities(move.MoveWithinRange(target, sightRange), this);
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
				moveActivity = ActivityUtils.SequenceActivities(move.MoveWithinRange(target, minRange, maxRange), this);
				return AttackStatus.NeedsToMove;
			}

			var targetedPosition = attack.GetTargetPosition(pos, target);
			var desiredFacing = (targetedPosition - pos).Yaw.Facing;
			if (!Util.FacingWithinTolerance(facing.Facing, desiredFacing, ((AttackFrontalInfo)attack.Info).FacingTolerance))
			{
				attackStatus |= AttackStatus.NeedsToTurn;
				turnActivity = ActivityUtils.SequenceActivities(new Turn(self, desiredFacing), this);
				return AttackStatus.NeedsToTurn;
			}

			attackStatus |= AttackStatus.Attacking;
			attack.DoAttack(self, target, armaments);

			return AttackStatus.Attacking;
		}
	}

	public static class TargetExts
	{
		/// <summary>Update (Frozen)Actor targets to account for visibility changes or actor replacement</summary>
		public static Target Recalculate(this Target t, Player viewer)
		{
			// Check whether the target has transformed into something else
			// HACK: This relies on knowing the internal implementation details of Target
			if (t.Type == TargetType.Invalid && t.Actor != null && t.Actor.ReplacedByActor != null)
				t = Target.FromActor(t.Actor.ReplacedByActor);

			// Bot-controlled units aren't yet capable of understanding visibility changes
			if (viewer.IsBot)
				return t;

			if (t.Type == TargetType.Actor)
			{
				// Actor has been hidden under the fog
				if (!t.Actor.CanBeViewedByPlayer(viewer))
				{
					// Replace with FrozenActor if applicable, otherwise drop the target
					var frozen = viewer.FrozenActorLayer.FromID(t.Actor.ActorID);
					return frozen != null ? Target.FromFrozenActor(frozen) : Target.Invalid;
				}
			}
			else if (t.Type == TargetType.FrozenActor)
			{
				// Frozen actor has been revealed
				if (!t.FrozenActor.Visible || !t.FrozenActor.IsValid)
				{
					// Original actor is still alive
					if (t.FrozenActor.Actor != null && t.FrozenActor.Actor.CanBeViewedByPlayer(viewer))
						return Target.FromActor(t.FrozenActor.Actor);

					// Original actor was killed while hidden
					if (t.Actor == null)
						return Target.Invalid;
				}
			}

			return t;
		}
	}
}
