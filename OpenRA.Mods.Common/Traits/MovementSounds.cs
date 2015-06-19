#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class MovementSoundsInfo : ITraitInfo
	{
		[Desc("Randomly select a sound from this list to play when this actor beings moving.")]
		public readonly string[] Sounds = { };

		[Desc("Should the selected sound loop?")]
		public readonly bool LoopSound = true;

		[Desc("Should allied players hear movement sounds?")]
		public readonly bool AlliesHearSounds = true;

		[Desc("Should enemy players hear movement sounds?")]
		public readonly bool EnemiesHearSounds = true;

		public object Create(ActorInitializer init) { return new MovementSounds(init.Self, this); }
	}

	public class MovementSounds : INotifyMovement
	{
		readonly MovementSoundsInfo info;
		readonly bool alliedWithListener;
		readonly MersenneTwister random;

		ISound sound;

		public MovementSounds(Actor self, MovementSoundsInfo info)
		{
			this.info = info;
			alliedWithListener = self.Owner.IsAlliedWith(self.World.RenderPlayer ?? self.World.LocalPlayer);
			random = Game.CosmeticRandom;
		}

		public void OnMovementStart(Actor self)
		{
			if (alliedWithListener && !info.AlliesHearSounds)
				return;

			if (!alliedWithListener && !info.EnemiesHearSounds)
				return;

			sound = info.LoopSound ? Sound.PlayLooped(info.Sounds.Random(random))
				: Sound.Play(info.Sounds.Random(random));
		}

		public void OnMovementStop(Actor self)
		{
			Sound.StopSound(sound);
		}
	}
}