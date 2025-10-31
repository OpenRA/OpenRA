#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class AnnounceOnKillInfo : TraitInfo
	{
		[Desc("Minimum duration (in milliseconds) between sound events.")]
		public readonly int Interval = 5000;

		[VoiceReference]
		[Desc("Voice to use when killing something.")]
		public readonly string Voice = "Kill";

		public override object Create(ActorInitializer init) { return new AnnounceOnKill(this); }
	}

	public class AnnounceOnKill : INotifyAppliedDamage
	{
		readonly AnnounceOnKillInfo info;

		long lastAnnounce;

		public AnnounceOnKill(AnnounceOnKillInfo info)
		{
			this.info = info;
			lastAnnounce = -info.Interval;
		}

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			// Don't notify suicides
			if (e.DamageState == DamageState.Dead && damaged != e.Attacker)
			{
				if (Game.RunTime > lastAnnounce + info.Interval)
					self.PlayVoice(info.Voice);

				lastAnnounce = Game.RunTime;
			}
		}
	}
}
