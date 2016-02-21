#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class AttackOmniInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackOmni(init.Self, this); }
	}

	class AttackOmni : AttackBase
	{
		public AttackOmni(Actor self, AttackOmniInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new SetTarget(this, newTarget);
		}

		class SetTarget : Activity
		{
			readonly Target target;
			readonly AttackOmni attack;

			public SetTarget(AttackOmni attack, Target target)
			{
				this.target = target;
				this.attack = attack;
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				attack.DoAttack(self, target);
				return this;
			}
		}
	}
}
