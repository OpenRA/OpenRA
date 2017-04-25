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

using OpenRA.GameRules;
using OpenRA.Mods.yupgi_alert.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("Causes aircraft husks that are spawned in the air to crash to the ground.")]
	public class BlimpFallsToEarthInfo : ITraitInfo, IRulesetLoaded, Requires<AircraftInfo>
	{
		[WeaponReference]
		public readonly string Explosion = "UnitExplode";

		public readonly bool Spins = true;
		public readonly int SpinInitial = 10;
		public readonly int SpinAcceleration = 0;
		public readonly bool Moves = false;
		public readonly WDist Velocity = new WDist(43);

		public WeaponInfo ExplosionWeapon { get; private set; }

		public object Create(ActorInitializer init) { return new BlimpFallsToEarth(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			ExplosionWeapon = string.IsNullOrEmpty(Explosion) ? null : rules.Weapons[Explosion.ToLowerInvariant()];
		}
	}

	public class BlimpFallsToEarth
	{
		public BlimpFallsToEarth(Actor self, BlimpFallsToEarthInfo info)
		{
			self.QueueActivity(false, new BlimpFallToEarth(self, info));
		}
	}
}
