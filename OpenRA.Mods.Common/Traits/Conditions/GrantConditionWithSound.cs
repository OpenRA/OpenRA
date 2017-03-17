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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies a condition and plays a sound when enabled and/or disabled.")]
	class GrantConditionWithSoundInfo : GrantConditionInfo
	{
		[Desc("Play a random sound from this list when enabled.")]
		public readonly string[] EnabledSounds = { };

		[Desc("Play a random sound from this list when disabled.")]
		public readonly string[] DisabledSounds = { };

		public override object Create(ActorInitializer init) { return new GrantConditionWithSound(init.Self, this); }
	}

	class GrantConditionWithSound : GrantCondition<GrantConditionWithSoundInfo>
	{
		public GrantConditionWithSound(Actor self, GrantConditionWithSoundInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);
			var sound = Info.EnabledSounds.RandomOrDefault(Game.CosmeticRandom);
			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
		}

		protected override void TraitDisabled(Actor self)
		{
			base.TraitDisabled(self);
			var sound = Info.DisabledSounds.RandomOrDefault(Game.CosmeticRandom);
			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
		}
	}
}
