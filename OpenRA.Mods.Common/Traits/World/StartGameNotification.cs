#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	class StartGameNotificationInfo : TraitInfo
	{
		[NotificationReference("Speech")]
		public readonly string Notification = "StartGame";

		[NotificationReference("Speech")]
		public readonly string LoadedNotification = "GameLoaded";

		[NotificationReference("Speech")]
		public readonly string SavedNotification = "GameSaved";

		public override object Create(ActorInitializer init) { return new StartGameNotification(this); }
	}

	class StartGameNotification : IWorldLoaded, INotifyGameLoaded, INotifyGameSaved
	{
		StartGameNotificationInfo info;
		public StartGameNotification(StartGameNotificationInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			if (!world.IsLoadingGameSave)
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", info.Notification, world.RenderPlayer == null ? null : world.RenderPlayer.Faction.InternalName);
		}

		void INotifyGameLoaded.GameLoaded(World world)
		{
			if (!world.IsReplay)
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", info.LoadedNotification, world.RenderPlayer == null ? null : world.RenderPlayer.Faction.InternalName);
		}

		void INotifyGameSaved.GameSaved(World world)
		{
			if (!world.IsReplay)
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", info.SavedNotification, world.RenderPlayer == null ? null : world.RenderPlayer.Faction.InternalName);
		}
	}
}
