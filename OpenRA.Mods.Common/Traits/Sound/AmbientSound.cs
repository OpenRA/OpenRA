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
	[Desc("Plays a looping audio file at the actor position. Attach this to the `World` actor to cover the whole map.")]
	class AmbientSoundInfo : ITraitInfo
	{
		[FieldLoader.Require]
		public readonly string SoundFile = null;

		public object Create(ActorInitializer init) { return new AmbientSound(init.Self, this); }
	}

	class AmbientSound
	{
		public AmbientSound(Actor self, AmbientSoundInfo info)
		{
			if (self.Info.HasTraitInfo<IOccupySpaceInfo>())
				Sound.PlayLooped(info.SoundFile, self.CenterPosition);
			else
				Sound.PlayLooped(info.SoundFile);
		}
	}
}
