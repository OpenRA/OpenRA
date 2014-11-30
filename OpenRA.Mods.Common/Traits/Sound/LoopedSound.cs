#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Play a sound repeatedly on the actor position.")]
	public class LoopedSoundInfo : ITraitInfo
	{
		public readonly string Filename;

		public int TickDelay = 25;

		public object Create(ActorInitializer init) { return new LoopedSound(this); }
	}

	public class LoopedSound : ITick
	{
		LoopedSoundInfo info;
		int remainingTicks;

		public LoopedSound(LoopedSoundInfo info)
		{
			this.info = info;
			remainingTicks = info.TickDelay;
		}

		public void Tick(Actor self)
		{
			if (--remainingTicks <= 0)
			{
				Sound.Play(info.Filename, self.CenterPosition);
				remainingTicks = info.TickDelay;
			}
		}
	}
}
