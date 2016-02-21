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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides the player with an audible warning when their storage is nearing full.")]
	public class ResourceStorageWarningInfo : ITraitInfo, Requires<PlayerResourcesInfo>
	{
		[Desc("Interval, in seconds, at which to check if more storage is needed.")]
		public readonly int AdviceInterval = 10;

		[Desc("The percentage threshold above which a warning is played.")]
		public readonly int Threshold = 80;

		[Desc("The speech to play for the warning.")]
		public readonly string Notification = "SilosNeeded";

		public object Create(ActorInitializer init) { return new ResourceStorageWarning(init.Self, this); }
	}

	public class ResourceStorageWarning : ITick
	{
		readonly ResourceStorageWarningInfo info;
		readonly PlayerResources resources;

		int nextSiloAdviceTime = 0;

		public ResourceStorageWarning(Actor self, ResourceStorageWarningInfo info)
		{
			this.info = info;
			resources = self.Trait<PlayerResources>();
		}

		public void Tick(Actor self)
		{
			if (--nextSiloAdviceTime <= 0)
			{
				var owner = self.Owner;

				if (resources.Resources > info.Threshold * resources.ResourceCapacity / 100)
					Game.Sound.PlayNotification(self.World.Map.Rules, owner, "Speech", info.Notification, owner.Faction.InternalName);

				nextSiloAdviceTime = info.AdviceInterval * 1000 / self.World.Timestep;
			}
		}
	}
}
