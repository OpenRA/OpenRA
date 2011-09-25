#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class DemoTruckInfo : TraitInfo<DemoTruck> { }

	class DemoTruck : Chronoshiftable, INotifyKilled
	{
		// Explode on chronoshift
		public override bool Teleport(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			Detonate(self, chronosphere);
			return false;
		}

		// Fire primary on death
		public void Killed(Actor self, AttackInfo e)
		{
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
