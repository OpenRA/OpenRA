#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ActorLostNotificationInfo : ITraitInfo
	{
		public readonly string Race = null;
		public readonly string Notification = null;
		public readonly bool NotifyAll = false;

		public object Create(ActorInitializer init) { return new ActorLostNotification(this); }
	}

	class ActorLostNotification : INotifyKilled
	{
		ActorLostNotificationInfo Info;
		public ActorLostNotification(ActorLostNotificationInfo info)
		{
			Info = info;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var player = (Info.NotifyAll) ? self.World.LocalPlayer : self.Owner;
			if (Info.Race != null && Info.Race != self.Owner.Country.Race)
				return;
			Sound.PlayToPlayer(player, Info.Notification);
		}
	}
}

