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
	public class Heal : Attack
	{
		public Heal(Target target, WRange range, bool allowMovement)
			: base(target, range, allowMovement) {}

		protected override Activity InnerTick(Actor self, AttackBase attack)
		{
			if (Target.IsActor && Target.Actor.GetDamageState() == DamageState.Undamaged)
				return NextActivity;

			return base.InnerTick(self, attack);
		}
	}
}
