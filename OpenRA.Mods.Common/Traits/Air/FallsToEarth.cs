#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Causes aircraft husks that are spawned in the air to crash to the ground.")]
	public class FallsToEarthInfo : TraitInfo, IRulesetLoaded, Requires<AircraftInfo>
	{
		[WeaponReference]
		[Desc("Explosion weapon that triggers when hitting ground.")]
		public readonly string Explosion = "UnitExplode";

		[Desc("Limit the maximum spin (in angle units per tick) that can be achieved while crashing.",
			"0 disables spinning. Leave undefined for no limit.")]
		public readonly WAngle? MaximumSpinSpeed = null;

		[Desc("Does the aircraft (husk) move forward at aircraft speed?")]
		public readonly bool Moves = false;

		[Desc("Velocity (per tick) at which aircraft falls to ground.")]
		public readonly WDist Velocity = new WDist(43);

		public WeaponInfo ExplosionWeapon { get; private set; }

		public override object Create(ActorInitializer init) { return new FallsToEarth(init, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (string.IsNullOrEmpty(Explosion))
				return;

			var weaponToLower = Explosion.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			ExplosionWeapon = weapon;
		}
	}

	public class FallsToEarth : IEffectiveOwner, INotifyCreated
	{
		readonly FallsToEarthInfo info;
		readonly Player effectiveOwner;

		public FallsToEarth(ActorInitializer init, FallsToEarthInfo info)
		{
			this.info = info;
			effectiveOwner = init.GetValue<EffectiveOwnerInit, Player>(info, init.Self.Owner);
		}

		// We return init.Self.Owner if there's no effective owner
		bool IEffectiveOwner.Disguised => true;
		Player IEffectiveOwner.Owner => effectiveOwner;

		void INotifyCreated.Created(Actor self)
		{
			self.QueueActivity(false, new FallToEarth(self, info));
		}
	}
}
