#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
namespace OpenRA.Mods.RA
{
	class ActorLostNotificationInfo : ITraitInfo
	{
		public readonly string Notification = null;
		public readonly bool NotifyAll = false;

		public object Create(ActorInitializer init) { return new ActorLostNotification(this); }
	}

	class ActorLostNotification : INotifyDamage
	{
		ActorLostNotificationInfo Info;
		public ActorLostNotification(ActorLostNotificationInfo info)
		{
			Info = info;
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				var player = (Info.NotifyAll) ? self.World.LocalPlayer : self.Owner;
				Sound.PlayToPlayer(player, Info.Notification);
			}
		}

	}
}

