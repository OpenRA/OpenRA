#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	/* non-turreted attack */
	public class Attack : Activity
	{
		protected Target Target;
		ITargetable targetable;
		WRange Range;
		bool AllowMovement;

		int nextPathTime;

		const int delayBetweenPathingAttempts = 20;
		const int delaySpread = 5;

		public Attack(Target target, WRange range)
			: this(target, range, true) {}

		public Attack(Target target, WRange range, bool allowMovement)
		{
			Target = target;
			if (target.IsActor)
				targetable = target.Actor.TraitOrDefault<ITargetable>();

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

			if (!Target.IsValid)
				return NextActivity;
				
			if (!self.Owner.HasFogVisibility() && Target.Actor != null && Target.Actor.HasTrait<Mobile>() && !self.Owner.Shroud.IsTargetable(Target.Actor))
				return NextActivity;

			if (targetable != null && !targetable.TargetableBy(Target.Actor, self))
				return NextActivity;

			if (!Target.IsInRange(self.CenterPosition, Range))
			{
				if (--nextPathTime > 0)
					return this;

				nextPathTime = self.World.SharedRandom.Next(delayBetweenPathingAttempts - delaySpread,
					delayBetweenPathingAttempts + delaySpread);

				return (AllowMovement) ? Util.SequenceActivities(self.Trait<Mobile>().MoveWithinRange(Target, Range.Range / 1024), this) : NextActivity;
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
