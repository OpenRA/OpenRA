#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class RepairBuilding : Activity
	{
		Actor target;

		public RepairBuilding(Actor target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (target == null || !target.IsInWorld || target.IsDead()) return NextActivity;
			if( !target.OccupiesSpace.OccupiedCells().Any( x => x.First == self.Location ) )
				return NextActivity;

			var health = target.Trait<Health>();
			if (health.DamageState == DamageState.Undamaged)
				return NextActivity;

			target.InflictDamage(self, -health.MaxHP, null);
			self.Destroy();

			return this;
		}
	}
}
