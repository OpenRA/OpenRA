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
	class FallsToEarthInfo : TraitInfo<FallsToEarth> { }

	class FallsToEarth : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead())
			{
				self.Trait<Health>().RemoveOnDeath = false;

				self.CancelActivity();
				self.QueueActivity(new FallToEarth());
			}
		}
	}

	class FallToEarth : CancelableActivity
	{
		int acceleration = 0;
		int spin = 0;

		public override IActivity Tick(Actor self)
		{
			if (acceleration == 0)
				acceleration = self.World.SharedRandom.Next(2) * 2 - 1;

			var aircraft = self.Trait<Aircraft>();
			if (aircraft.Altitude <= 0)
			{
				self.Destroy();
				return null;
			}

			spin += acceleration;
			aircraft.Facing = (aircraft.Facing + spin) % 256;
			aircraft.Altitude--;

			return this;
		}

		protected override bool OnCancel() { return false; }
	}
}
