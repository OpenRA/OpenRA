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
	[Desc("Plays a looping audio file at the actor position. Attach this to the `World` actor to cover the whole map.")]
	class AmbientSoundInfo : UpgradableTraitInfo
	{
		[FieldLoader.Require]
		public readonly string SoundFile = null;

		[Desc("Interval between playing the sound (in ticks).")]
		public readonly int Interval = 0;

		public override object Create(ActorInitializer init) { return new AmbientSound(this); }
	}

	class AmbientSound : UpgradableTrait<AmbientSoundInfo>, ITick, INotifyRemovedFromWorld
	{
		ISound currentSound;
		bool wasDisabled = true;
		int interval;
		WPos cachedPosition;

		public AmbientSound(AmbientSoundInfo info)
			: base(info)
		{
			interval = info.Interval;
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld)
			{
				StopSound();
				return;
			}

			if (Info.Interval <= 0)
			{
				var moved = self.OccupiesSpace != null && cachedPosition != self.CenterPosition;
				if (!wasDisabled && !moved)
					return;

				wasDisabled = false;

				if (moved)
				{
					// Otherwise the sound never gets stopped when the actor is on the move
					Game.Sound.StopSound(currentSound);
					currentSound = null;

					cachedPosition = self.CenterPosition;
					currentSound = Game.Sound.PlayLooped(Info.SoundFile, self.CenterPosition);
				}
				else
					currentSound = Game.Sound.PlayLooped(Info.SoundFile);

				return;
			}

			if (interval-- > 0)
				return;

			interval = Info.Interval;

			if (self.OccupiesSpace != null)
				Game.Sound.Play(Info.SoundFile, self.CenterPosition);
			else
				Game.Sound.Play(Info.SoundFile);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			StopSound();
		}

		void StopSound()
		{
			Game.Sound.StopSound(currentSound);
			currentSound = null;
			wasDisabled = true;
		}
	}
}
