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
        readonly Stance validStances;

        public RepairBuilding(Actor self, Actor target, EnterBehaviour enterBehaviour, Stance validStances)
			: base(self, target, enterBehaviour)
		{
			this.target = target;
            this.validStances = validStances;
			health = target.Trait<Health>();
		}

		protected override bool CanReserve(Actor self)
		{
			return health.DamageState != DamageState.Undamaged;
		}

		protected override bool OnInside(Actor self)
		{
			var stance = self.Owner.Stances[target.Owner];
			if (!stance.HasStance(validStances))
				return false;

            if (health.DamageState == DamageState.Undamaged)
				return false;

			target.InflictDamage(self, new Damage(-health.MaxHP));
			return true;
		}
	}
}
