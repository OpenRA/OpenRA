#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class FallsToEarthInfo : TraitInfo<FallsToEarth>
	{
		[WeaponReference]
		public readonly string Explosion = null;

		public readonly bool Spins = true;
		public readonly bool Moves = false;
	}

	class FallsToEarth : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead())
			{
				self.Trait<Health>().RemoveOnDeath = false;

				self.CancelActivity();
				self.QueueActivity(new FallToEarth(self, self.Info.Traits.Get<FallsToEarthInfo>()));
			}
		}
	}

	class FallToEarth : CancelableActivity
	{
		int acceleration = 0;
		int spin = 0;
		FallsToEarthInfo info;

		public FallToEarth(Actor self, FallsToEarthInfo info)
		{
			this.info = info;
			if (info.Spins)
				acceleration = self.World.SharedRandom.Next(2) * 2 - 1;
		}

		public override IActivity Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			if (aircraft.Altitude <= 0)
			{
				if (info.Explosion != null)
					Combat.DoExplosion(self, info.Explosion, self.CenterLocation, 0);

				self.Destroy();
				return null;
			}

			if (info.Spins)
			{
				spin += acceleration;
				aircraft.Facing = (aircraft.Facing + spin) % 256;
			}

			if (info.Moves)
				FlyUtil.Fly(self, aircraft.Altitude);

			aircraft.Altitude--;

			return this;
		}

		protected override bool OnCancel(Actor self) { return false; }
	}
}
