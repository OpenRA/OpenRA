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
		WDist Range;
		bool AllowMovement;

		int nextPathTime;

		const int delayBetweenPathingAttempts = 20;
		const int delaySpread = 5;

		public Attack(Target target, WDist range)
			: this(target, range, true) {}

		public Attack(Target target, WDist range, bool allowMovement)
		{
			Target = target;
			Range = range;
			AllowMovement = allowMovement;
		}

		public override Activity Tick(Actor self)
		{
			var attack = self.Trait<AttackBase>();
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

			// TODO: This is horrible, and probably wrong. Work out what it is trying to solve, then redo it properly.
			if (type == TargetType.Actor && !self.Owner.HasFogVisibility() && Target.Actor.HasTrait<Mobile>() && !self.Owner.Shroud.IsTargetable(Target.Actor))
				return NextActivity;

			if (!Target.IsInRange(self.CenterPosition, Range))
			{
				if (--nextPathTime > 0)
					return this;

				nextPathTime = self.World.SharedRandom.Next(delayBetweenPathingAttempts - delaySpread,
					delayBetweenPathingAttempts + delaySpread);

				return (AllowMovement) ? Util.SequenceActivities(self.Trait<IMove>().MoveWithinRange(Target, Range), this) : NextActivity;
			}

			var desiredFacing = Util.GetFacing(Target.CenterPosition - self.CenterPosition, 0);
			var facing = self.Trait<IFacing>();
			if (facing.Facing != desiredFacing)
				return Util.SequenceActivities(new Turn(desiredFacing), this);

			attack.DoAttack(self, Target);
			return this;
		}
	}
}
