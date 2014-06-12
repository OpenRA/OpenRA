#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Air
{
	class FallsToEarthInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string Explosion = "UnitExplode";

		public readonly bool Spins = true;
		public readonly bool Moves = false;
		public readonly WRange Velocity = new WRange(43);

		public object Create(ActorInitializer init) { return new FallsToEarth(init.self, this); }
	}

	class FallsToEarth
	{
		public FallsToEarth(Actor self, FallsToEarthInfo info)
		{
			self.QueueActivity(false, new FallToEarth(self, info));
		}
	}

	class FallToEarth : Activity
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

		public override Activity Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			if (self.CenterPosition.Z <= 0)
			{
				if (info.Explosion != null)
					Combat.DoExplosion(self, info.Explosion, self.CenterPosition);

				self.Destroy();
				return null;
			}

			if (info.Spins)
			{
				spin += acceleration;
				aircraft.Facing = (aircraft.Facing + spin) % 256;
			}

			var move = info.Moves ? aircraft.FlyStep(aircraft.Facing) : WVec.Zero;
			move -= new WVec(WRange.Zero, WRange.Zero, info.Velocity);
			aircraft.SetPosition(self, aircraft.CenterPosition + move);

			return this;
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
