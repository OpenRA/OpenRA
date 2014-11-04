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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Sounds to play when killed.")]
	public class DeathSoundsInfo : ITraitInfo
	{
		[Desc("Death notification voice.")]
		public readonly string DeathSound = "Die";
		
		[Desc("Multiply volume with this factor.")]
		public readonly float VolumeMultiplier = 1f;

		[Desc("DeathTypes that this should be used for. If empty, this will be used as the default sound.")]
		public readonly string[] DeathTypes = { };

		public object Create(ActorInitializer init) { return new DeathSounds(this); }
	}

	public class DeathSounds : INotifyKilled
	{
		DeathSoundsInfo info;

		public DeathSounds(DeathSoundsInfo info) { this.info = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			// Killed by some non-standard means
			if (e.Warhead == null)
				return;

			var cp = self.CenterPosition;

			if (info.DeathTypes.Contains(e.Warhead.DeathType) || (!info.DeathTypes.Any() && !self.Info.Traits.WithInterface<DeathSoundsInfo>().Any(dsi => dsi.DeathTypes.Contains(e.Warhead.DeathType))))
				Sound.PlayVoiceLocal(info.DeathSound, self, self.Owner.Country.Race, cp, info.VolumeMultiplier);
		}
	}
}