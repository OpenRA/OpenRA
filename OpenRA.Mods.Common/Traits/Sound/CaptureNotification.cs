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
	public class CaptureNotificationInfo : ITraitInfo
	{
		public readonly string Notification = "BuildingCaptured";
		public readonly bool NewOwnerVoice = true;

		public object Create(ActorInitializer init) { return new CaptureNotification(this); }
	}

	public class CaptureNotification : INotifyCapture
	{
		CaptureNotificationInfo info;
		public CaptureNotification(CaptureNotificationInfo info)
		{
			this.info = info;
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (captor.World.LocalPlayer != captor.Owner)
				return;

			var faction = info.NewOwnerVoice ? newOwner.Faction.InternalName : oldOwner.Faction.InternalName;
			Game.Sound.PlayNotification(self.World.Map.Rules, captor.World.LocalPlayer, "Speech", info.Notification, faction);
		}
	}
}
