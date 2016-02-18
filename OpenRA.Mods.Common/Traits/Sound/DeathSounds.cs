#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Sounds to play when killed.")]
	public class DeathSoundsInfo : ITraitInfo
	{
		[Desc("Death notification voice.")]
		[VoiceReference] public readonly string Voice = "Die";

		[Desc("Multiply volume with this factor.")]
		public readonly float VolumeMultiplier = 1f;

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"If empty, this will be used as the default sound for all death types.")]
		public readonly HashSet<string> DeathTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new DeathSounds(this); }
	}

	public class DeathSounds : INotifyKilled
	{
		readonly DeathSoundsInfo info;

		public DeathSounds(DeathSoundsInfo info) { this.info = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			var warhead = e.Warhead as DamageWarhead;

			// If the warhead is null, the actor was killed by some non-standard means
			if (info.DeathTypes.Count == 0 || (warhead != null && warhead.DamageTypes.Overlaps(info.DeathTypes)))
				self.PlayVoiceLocal(info.Voice, info.VolumeMultiplier);
		}
	}
}