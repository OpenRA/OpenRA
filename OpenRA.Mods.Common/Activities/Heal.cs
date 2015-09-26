#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Heal : Attack
	{
		public Heal(Actor self, Target target, Armament armament, bool allowMovement)
			: base(self, target, armament, allowMovement) { }

		protected override Activity InnerTick(Actor self, AttackBase attack)
		{
			if (!Target.IsValidFor(self) || !self.Owner.IsAlliedWith(Target.Actor.Owner))
				return NextActivity;

			if (Target.Type == TargetType.Actor && Target.Actor.GetDamageState() == DamageState.Undamaged)
				return NextActivity;

			return base.InnerTick(self, attack);
		}
	}
}
