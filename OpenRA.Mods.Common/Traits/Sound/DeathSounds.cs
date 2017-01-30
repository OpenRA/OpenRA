#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Sounds to play when killed.")]
	public class DeathSoundsInfo : ConditionalTraitInfo
	{
		[Desc("Death notification voice.")]
		[VoiceReference] public readonly string Voice = "Die";

		[Desc("Multiply volume with this factor.")]
		public readonly float VolumeMultiplier = 1f;

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"If empty, this will be used as the default sound for all death types.")]
		public readonly HashSet<string> DeathTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new DeathSounds(this); }
	}

	public class DeathSounds : ConditionalTrait<DeathSoundsInfo>, INotifyKilled
	{
		public DeathSounds(DeathSoundsInfo info)
			: base(info) { }

		public void Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (Info.DeathTypes.Count == 0 || e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				self.PlayVoiceLocal(Info.Voice, Info.VolumeMultiplier);
		}
	}
}
