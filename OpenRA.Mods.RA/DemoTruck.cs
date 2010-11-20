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

namespace OpenRA.Mods.RA
{
	class DemoTruckInfo : TraitInfo<DemoTruck> { }

	class DemoTruck : Chronoshiftable, INotifyDamage
	{
		// Explode on chronoshift
		public override bool Activate(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			Detonate(self, chronosphere);
			return false;
		}

		// Fire primary on death
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Detonate(self, e.Attacker);
		}

		public void Detonate(Actor self, Actor detonatedBy)
		{
			var move = self.TraitOrDefault<IMove>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();
			var altitude = move != null ? move.Altitude : 0;

			self.World.AddFrameEndTask( w =>
			{
				if (self.Destroyed) return;
				Combat.DoExplosion(self, info.PrimaryWeapon, self.CenterLocation, altitude);
		
				// Remove from world
				self.Kill(self);
				detonatedBy.Owner.Kills++;
				self.Owner.Deaths++;
				self.Destroy();
			} );
		}
	}
}
