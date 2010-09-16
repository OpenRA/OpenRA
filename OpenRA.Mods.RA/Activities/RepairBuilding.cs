#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class RepairBuilding : IActivity
	{
		Target target;

		public RepairBuilding(Actor target) { this.target = Target.FromActor(target); }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (!target.IsValid) return NextActivity;
			if ((target.Actor.Location - self.Location).Length > 1)
				return NextActivity;

			var health = target.Actor.Trait<Health>();
			if (health.DamageState == DamageState.Undamaged)
				return NextActivity;
			
			target.Actor.InflictDamage(self, -health.MaxHP, null);
			self.Destroy();

			return NextActivity;
		}

		public void Cancel(Actor self) { target = Target.None; NextActivity = null; }
	}
}
