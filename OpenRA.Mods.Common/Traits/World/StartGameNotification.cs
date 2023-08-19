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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	sealed class StartGameNotificationInfo : TraitInfo
	{
		[NotificationReference("Speech")]
		public readonly string Notification = "StartGame";

		[TranslationReference(optional: true)]
		public readonly string TextNotification = null;

		[NotificationReference("Speech")]
		public readonly string LoadedNotification = "GameLoaded";

		[TranslationReference(optional: true)]
		public readonly string LoadedTextNotification = null;

		[NotificationReference("Speech")]
		public readonly string SavedNotification = "GameSaved";

		[TranslationReference(optional: true)]
		public readonly string SavedTextNotification = null;

		public override object Create(ActorInitializer init) { return new StartGameNotification(this); }
	}

	sealed class StartGameNotification : IWorldLoaded, INotifyGameLoaded, INotifyGameSaved
	{
		readonly StartGameNotificationInfo info;
		public StartGameNotification(StartGameNotificationInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			if (!world.IsLoadingGameSave)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", info.Notification, world.RenderPlayer?.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(null, info.TextNotification);
			}
		}

		void INotifyGameLoaded.GameLoaded(World world)
		{
			if (!world.IsReplay)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", info.LoadedNotification, world.RenderPlayer?.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(null, info.LoadedTextNotification);
			}
		}

		void INotifyGameSaved.GameSaved(World world)
		{
			if (!world.IsReplay)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", info.SavedNotification, world.RenderPlayer?.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(null, info.SavedTextNotification);
			}
		}
	}
}
