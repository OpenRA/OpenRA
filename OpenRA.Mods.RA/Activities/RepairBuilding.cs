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
	class RepairBuilding : Enter
	{
		readonly Actor target;
		readonly Health health;

		public RepairBuilding(Actor self, Actor target)
			: base(self, target)
		{
			this.target = target;
			health = target.Trait<Health>();
		}

		protected override bool CanReserve(Actor self)
		{
			return health.DamageState != DamageState.Undamaged;
		}

		protected override void OnInside(Actor self)
		{
			target.InflictDamage(self, -health.MaxHP, null);
			self.Destroy();
		}
	}
}
