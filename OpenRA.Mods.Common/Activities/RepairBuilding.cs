#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBuilding : Enter
	{
		readonly Actor target;
		readonly IHealth health;
		readonly Stance validStances;

		public RepairBuilding(Actor self, Actor target, EnterBehaviour enterBehaviour, Stance validStances)
			: base(self, target, enterBehaviour)
		{
			this.target = target;
			this.validStances = validStances;
			health = target.Trait<IHealth>();
		}

		protected override bool CanReserve(Actor self)
		{
			return health.DamageState != DamageState.Undamaged;
		}

		protected override void OnInside(Actor self)
		{
			var stance = self.Owner.Stances[target.Owner];
			if (!stance.HasStance(validStances))
				return;

			if (health.DamageState == DamageState.Undamaged)
				return;

			target.InflictDamage(self, new Damage(-health.MaxHP));
		}
	}
}
