#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Heal : Attack
	{
		public Heal(Actor self, Target target, WRange minRange, WRange maxRange, bool allowMovement)
			: base(self, target, minRange, maxRange, allowMovement) { }

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
