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
	class CaptureNotificationInfo : ITraitInfo
	{
		public readonly string Race = null;
		public readonly string Notification = null;

		public object Create(ActorInitializer init) { return new CaptureNotification(this); }
	}

	class CaptureNotification : INotifyCapture
	{
		CaptureNotificationInfo Info;
		public CaptureNotification(CaptureNotificationInfo info)
		{
			Info = info;
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (captor.World.LocalPlayer != captor.Owner)
				return;
			
			if (Info.Race != null && Info.Race != oldOwner.Country.Race)
				return;
			
			Sound.PlayToPlayer(captor.World.LocalPlayer, Info.Notification);
		}
	}
}

