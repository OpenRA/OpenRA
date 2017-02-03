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

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Plays a looping audio file at the actor position. Attach this to the `World` actor to cover the whole map.")]
	class AmbientSoundInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string SoundFile = null;

		[Desc("Initial delay (in ticks) before playing the sound for the first time.",
			"Two values indicate a random delay range.")]
		public readonly int[] Delay = { 0 };

		[Desc("Interval between playing the sound (in ticks).",
			"Two values indicate a random delay range.")]
		public readonly int[] Interval = { 0 };

		public override object Create(ActorInitializer init) { return new AmbientSound(init.Self, this); }
	}

	class AmbientSound : ConditionalTrait<AmbientSoundInfo>, ITick, INotifyRemovedFromWorld
	{
		readonly bool loop;
		ISound currentSound;
		WPos cachedPosition;
		int delay;

		public AmbientSound(Actor self, AmbientSoundInfo info)
			: base(info)
		{
			delay = RandomDelay(self.World, info.Delay);
			loop = Info.Interval.Length == 0 || (Info.Interval.Length == 1 && Info.Interval[0] == 0);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var pos = self.CenterPosition;
			if (currentSound != null && pos != cachedPosition)
			{
				currentSound.SetPosition(pos);
				cachedPosition = pos;
			}

			if (delay < 0)
				return;

			if (--delay < 0)
			{
				StartSound(self);
				if (!loop)
					delay = RandomDelay(self.World, Info.Interval);
			}
		}

		void StartSound(Actor self)
		{
			if (self.OccupiesSpace != null)
			{
				cachedPosition = self.CenterPosition;
				currentSound = loop ? Game.Sound.PlayLooped(SoundType.World, Info.SoundFile, cachedPosition) :
					Game.Sound.Play(SoundType.World, Info.SoundFile, self.CenterPosition);
			}
			else
				currentSound = loop ? Game.Sound.PlayLooped(SoundType.World, Info.SoundFile) :
					Game.Sound.Play(SoundType.World, Info.SoundFile);
		}

		void StopSound()
		{
			if (currentSound == null)
				return;

			Game.Sound.StopSound(currentSound);
			currentSound = null;
		}

		static int RandomDelay(World world, int[] range)
		{
			if (range.Length == 0)
				return 0;

			if (range.Length == 1)
				return range[0];

			return world.SharedRandom.Next(range[0], range[1]);
		}

		protected override void TraitEnabled(Actor self) { delay = RandomDelay(self.World, Info.Delay); }
		protected override void TraitDisabled(Actor self) { StopSound(); }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { StopSound(); }
	}
}
