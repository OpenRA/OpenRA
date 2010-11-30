#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	/* non-turreted attack */
	public class Heal : Attack
	{
		public Heal(Target target, int range, bool allowMovement)
			: base(target, range, allowMovement) {}

		protected override IActivity InnerTick( Actor self, AttackBase attack )
		{
			if (Target.IsActor && Target.Actor.GetDamageState() == DamageState.Undamaged)
				return NextActivity;
			
			return base.InnerTick(self, attack);
		}
	}
}
