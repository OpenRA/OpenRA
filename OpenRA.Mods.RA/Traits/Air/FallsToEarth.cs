#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class FallsToEarthInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string Explosion = "UnitExplode";

		public readonly bool Spins = true;
		public readonly bool Moves = false;
		public readonly WRange Velocity = new WRange(43);

		public object Create(ActorInitializer init) { return new FallsToEarth(init.self, this); }
	}

	public class FallsToEarth
	{
		public FallsToEarth(Actor self, FallsToEarthInfo info)
		{
			self.QueueActivity(false, new FallToEarth(self, info));
		}
	}
}
