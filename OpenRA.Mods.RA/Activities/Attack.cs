#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	/* non-turreted attack */
	public class Attack : Activity
	{
		protected Target Target;
		AttackBase attack;
		IFacing facing;
		Activity move;
		WRange minRange;
		WRange maxRange;

		public Attack(Actor self, Target target, WRange minRange, WRange maxRange, bool allowMovement)
		{
			Target = target;
			this.minRange = minRange;
			this.maxRange = maxRange;

			attack = self.Trait<AttackBase>();
			facing = self.Trait<IFacing>();

			var imove = self.TraitOrDefault<IMove>();
			move = allowMovement && imove != null ? imove.MoveWithinRange(target, minRange, maxRange) : null;
		}

		public override Activity Tick(Actor self)
		{
			var ret = InnerTick(self, attack);
			attack.IsAttacking = (ret == this);
			return ret;
		}

		protected virtual Activity InnerTick(Actor self, AttackBase attack)
		{
			if (IsCanceled)
				return NextActivity;

			var type = Target.Type;
			if (!Target.IsValidFor(self) || type == TargetType.FrozenActor)
				return NextActivity;

			// Drop the target if it moves under the shroud / fog.
			if (type == TargetType.Actor && !self.Owner.Shroud.IsTargetable(Target.Actor))
				return NextActivity;

			if (move != null && (!Target.IsInRange(self.CenterPosition, maxRange) || Target.IsInRange(self.CenterPosition, minRange)))
			{
				// Try to move within range
				move.Tick(self);
			}
			else
			{
				var desiredFacing = Util.GetFacing(Target.CenterPosition - self.CenterPosition, 0);
				if (facing.Facing != desiredFacing)
					return Util.SequenceActivities(new Turn(desiredFacing), this);

				attack.DoAttack(self, Target);
			}

			return this;
		}
	}
}
