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
	class ActorLostNotificationInfo : ITraitInfo
	{
		public readonly string Notification = "UnitLost";
		public readonly bool NotifyAll = false;

		public object Create(ActorInitializer init) { return new ActorLostNotification(this); }
	}

	class ActorLostNotification : INotifyKilled
	{
		ActorLostNotificationInfo info;
		public ActorLostNotification(ActorLostNotificationInfo info)
		{
			this.info = info;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var player = info.NotifyAll ? self.World.LocalPlayer : self.Owner;
			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", info.Notification, self.Owner.Faction.InternalName);
		}
	}
}
