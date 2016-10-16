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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBuilding : Enter
	{
		readonly Actor target;
		readonly Health health;

		public RepairBuilding(Actor self, Actor target, EnterBehaviour enterBehaviour)
			: base(self, target, enterBehaviour)
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
			if (health.DamageState == DamageState.Undamaged)
				return;

			target.InflictDamage(self, new Damage(-health.MaxHP));
		}
	}
}
