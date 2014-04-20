#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class StartGameNotificationInfo : ITraitInfo
	{
		public readonly string Notification = "StartGame";

		public object Create(ActorInitializer init) { return new StartGameNotification(this); }
	}

	class StartGameNotification : IWorldLoaded
	{
		StartGameNotificationInfo info;
		public StartGameNotification(StartGameNotificationInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			Sound.PlayNotification(null, "Speech", info.Notification, null);
		}
	}
}
