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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Sounds to play when killed.")]
	public class DeathSoundsInfo : ConditionalTraitInfo
	{
		[VoiceReference]
		[Desc("Death notification voice.")]
		public readonly string Voice = "Die";

		[Desc("Multiply volume with this factor.")]
		public readonly float VolumeMultiplier = 1f;

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"If empty, this will be used as the default sound for all death types.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		public override object Create(ActorInitializer init) { return new DeathSounds(this); }
	}

	public class DeathSounds : ConditionalTrait<DeathSoundsInfo>, INotifyKilled
	{
		public DeathSounds(DeathSoundsInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (Info.DeathTypes.IsEmpty || e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				self.PlayVoiceLocal(Info.Voice, Info.VolumeMultiplier);
		}
	}
}
