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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Provides the player with an audible warning when their storage is nearing full.")]
	public class ResourceStorageWarningInfo : TraitInfo, Requires<PlayerResourcesInfo>
	{
		[Desc("Interval (in milliseconds) at which to check if more storage is needed.")]
		public readonly int AdviceInterval = 20000;

		[Desc("The percentage threshold above which a warning is played.")]
		public readonly int Threshold = 80;

		[NotificationReference("Speech")]
		[Desc("Speech to play for the warning.")]
		public readonly string Notification = "SilosNeeded";

		[Desc("Text to display for the warning.")]
		public readonly string TextNotification = null;

		public override object Create(ActorInitializer init) { return new ResourceStorageWarning(init.Self, this); }
	}

	public class ResourceStorageWarning : ITick
	{
		readonly ResourceStorageWarningInfo info;
		readonly PlayerResources resources;

		long lastSiloAdviceTime;

		public ResourceStorageWarning(Actor self, ResourceStorageWarningInfo info)
		{
			this.info = info;
			resources = self.Trait<PlayerResources>();
		}

		void ITick.Tick(Actor self)
		{
			if (Game.RunTime > lastSiloAdviceTime + info.AdviceInterval)
			{
				var owner = self.Owner;

				if (resources.Resources > info.Threshold * resources.ResourceCapacity / 100)
				{
					Game.Sound.PlayNotification(self.World.Map.Rules, owner, "Speech", info.Notification, owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(info.TextNotification, owner);
				}

				lastSiloAdviceTime = Game.RunTime;
			}
		}
	}
}
