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

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackOmniInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackOmni(init.Self, this); }
	}

	public class AttackOmni : AttackBase
	{
		public AttackOmni(Actor self, AttackOmniInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new SetTarget(this, newTarget, allowMove);
		}

		// Some 3rd-party mods rely on this being public
		public class SetTarget : Activity
		{
			readonly AttackOmni attack;
			readonly bool allowMove;
			Target target;

			public SetTarget(AttackOmni attack, Target target, bool allowMove)
			{
				this.target = target;
				this.attack = attack;
				this.allowMove = allowMove;
			}

			public override Activity Tick(Actor self)
			{
				// This activity can't move to reacquire hidden targets, so use the
				// Recalculate overload that invalidates hidden targets.
				target = target.RecalculateInvalidatingHiddenTargets(self.Owner);
				if (IsCanceling || !target.IsValidFor(self) || !attack.IsReachableTarget(target, allowMove))
					return NextActivity;

				attack.DoAttack(self, target);
				return this;
			}
		}
	}
}
