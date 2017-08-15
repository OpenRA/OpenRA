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

using System;
using System.Drawing;
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
		enum AttackStatus { UnableToAttack, NeedsToTurn, NeedsToMove, Attacking, FrozenSwitch }

		protected readonly Target Target;
		readonly AttackBase[] attackTraits;
		readonly IMove move;
		readonly IFacing facing;
		readonly IPositionable positionable;
		readonly bool forceAttack;
		readonly int facingTolerance;

		WDist minRange;
		WDist maxRange;
		Activity turnActivity;
		Activity moveActivity;
		Attack frozenSwitchActivity;
		AttackStatus attackStatus = AttackStatus.UnableToAttack;

		public Attack(Actor self, Target target, bool allowMovement, bool forceAttack, int facingTolerance)
		{
			Target = target;

			this.forceAttack = forceAttack;
			this.facingTolerance = facingTolerance;

			attackTraits = self.TraitsImplementing<AttackBase>().ToArray();
			facing = self.Trait<IFacing>();
			positionable = self.Trait<IPositionable>();

			move = allowMovement ? self.TraitOrDefault<IMove>() : null;
		}

		public override Activity Tick(Actor self)
		{
			turnActivity = moveActivity = null;
			attackStatus = AttackStatus.UnableToAttack;

			foreach (var attack in attackTraits.Where(x => !x.IsTraitDisabled))
			{
				var activity = InnerTick(self, attack);
				attack.IsAttacking = activity == this;
			}

			if (attackStatus.HasFlag(AttackStatus.Attacking))
				return this;

			if (attackStatus.HasFlag(AttackStatus.NeedsToTurn))
				return turnActivity;

			if (attackStatus.HasFlag(AttackStatus.NeedsToMove))
				return moveActivity;

			if (attackStatus.HasFlag(AttackStatus.FrozenSwitch))
			{
				// When shooting a non-frozen target which became frozen,
				// this makes sure that the target line will remain even after
				// the underlying actor is destroyed
				self.SetTargetLine(frozenSwitchActivity.Target, Color.Red);
				return frozenSwitchActivity;
			}

			return NextActivity;
		}

		protected bool TryUpdateFrozenActorTarget(Actor self)
		{
			Target target2 = Target.TryUpdateFrozenActorTarget(self);
			if (target2.Type == TargetType.Invalid)
				return false;
			frozenSwitchActivity = new Attack(self, target2, move != null, forceAttack, facingTolerance);
			return true;
		}

		protected virtual Activity InnerTick(Actor self, AttackBase attack)
		{
			if (IsCanceled)
				return NextActivity;

			if (!Target.IsValidFor(self))
				return NextActivity;

			if (!Target.IsOwnerTargetable(self))
			{
				if (TryUpdateFrozenActorTarget(self))
					attackStatus |= AttackStatus.FrozenSwitch;

				return NextActivity;
			}

			var positionableLoc = self.World.Map.CellContaining(Target.CenterPosition);
			if (attack.Info.AttackRequiresEnteringCell && !positionable.CanEnterCell(positionableLoc, null, false))
				return NextActivity;

			// Drop the target once none of the weapons are effective against it
			var armaments = attack.ChooseArmamentsForTarget(Target, forceAttack).ToList();
			if (armaments.Count == 0)
				return NextActivity;

			// Update ranges
			minRange = armaments.Max(a => a.Weapon.MinRange);
			maxRange = armaments.Min(a => a.MaxRange());

			var pos = self.CenterPosition;
			var mobile = move as Mobile;
			if (!Target.IsInRange(pos, maxRange)
				|| (minRange.Length != 0 && Target.IsInRange(pos, minRange))
				|| (mobile != null && !mobile.CanInteractWithGroundLayer(self)))
			{
				// Try to move within range, drop the target otherwise
				if (move == null)
					return NextActivity;

				attackStatus |= AttackStatus.NeedsToMove;
				moveActivity = ActivityUtils.SequenceActivities(move.MoveWithinRange(Target, minRange, maxRange), this);
				return NextActivity;
			}

			var targetedPosition = attack.GetTargetPosition(pos, Target);
			var desiredFacing = (targetedPosition - pos).Yaw.Facing;
			if (!Util.FacingWithinTolerance(facing.Facing, desiredFacing, facingTolerance))
			{
				attackStatus |= AttackStatus.NeedsToTurn;
				turnActivity = ActivityUtils.SequenceActivities(new Turn(self, desiredFacing), this);
				return NextActivity;
			}

			attackStatus |= AttackStatus.Attacking;
			attack.DoAttack(self, Target, armaments);

			return this;
		}
	}
}
