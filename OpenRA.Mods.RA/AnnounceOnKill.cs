#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Play the Kill voice of this actor when eliminating enemies.")]
	public class AnnounceOnKillInfo : ITraitInfo
	{
		[Desc("Minimum duration (in seconds) between sound events.")]
		public readonly int Interval = 5;

		public object Create(ActorInitializer init) { return new AnnounceOnKill(init.self, this); }
	}

	public class AnnounceOnKill : INotifyAppliedDamage
	{
		readonly AnnounceOnKillInfo info;

		int lastAnnounce;

		public AnnounceOnKill(Actor self, AnnounceOnKillInfo info)
		{
			this.info = info;
			lastAnnounce = -info.Interval * 25;
		}

		public void AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead && damaged != e.Attacker) // don't notify suicides
			{
				if (self.World.WorldTick - lastAnnounce > info.Interval * 25)
					Sound.PlayVoice("Kill", self, self.Owner.Country.Race, self.Owner);

				lastAnnounce = self.World.WorldTick;
			}
		}
	}
}
