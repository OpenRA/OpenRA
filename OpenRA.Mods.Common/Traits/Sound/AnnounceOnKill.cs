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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Play the Kill voice of this actor when eliminating enemies.")]
	public class AnnounceOnKillInfo : ITraitInfo
	{
		[Desc("Minimum duration (in seconds) between sound events.")]
		public readonly int Interval = 5;

		[Desc("Voice to use when killing something.")]
		[VoiceReference] public readonly string Voice = "Kill";

		public object Create(ActorInitializer init) { return new AnnounceOnKill(init.Self, this); }
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
			// Don't notify suicides
			if (e.DamageState == DamageState.Dead && damaged != e.Attacker)
			{
				if (self.World.WorldTick - lastAnnounce > info.Interval * 25)
					self.PlayVoice(info.Voice);

				lastAnnounce = self.World.WorldTick;
			}
		}
	}
}
