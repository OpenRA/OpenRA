#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class SoundOnDamageTransitionInfo : ITraitInfo
	{
		[Desc("Play a random sound from this list when damaged.")]
		public readonly string[] DamagedSounds = { };

		[Desc("Play a random sound from this list when destroyed.")]
		public readonly string[] DestroyedSounds = { };

		public object Create(ActorInitializer init) { return new SoundOnDamageTransition(this); }
	}

	public class SoundOnDamageTransition : INotifyDamageStateChanged
	{
		readonly SoundOnDamageTransitionInfo info;

		public SoundOnDamageTransition(SoundOnDamageTransitionInfo info)
		{
			this.info = info;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			var rand = Game.CosmeticRandom;

			if (e.DamageState == DamageState.Dead)
			{
				var sound = info.DestroyedSounds.RandomOrDefault(rand);
				Game.Sound.Play(sound, self.CenterPosition);
			}
			else if (e.DamageState >= DamageState.Heavy && e.PreviousDamageState < DamageState.Heavy)
			{
				var sound = info.DamagedSounds.RandomOrDefault(rand);
				Game.Sound.Play(sound, self.CenterPosition);
			}
		}
	}
}
