#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	public class CaptureNotificationInfo : ITraitInfo
	{
		[NotificationReference("Speech")]
		[Desc("The speech notification to play to the new owner.")]
		public readonly string Notification = "BuildingCaptured";

		[Desc("Specifies if Notification is played with the voice of the new owners faction.")]
		public readonly bool NewOwnerVoice = true;

		[NotificationReference("Speech")]
		[Desc("The speech notification to play to the old owner.")]
		public readonly string LoseNotification = null;

		[Desc("Specifies if LoseNotification is played with the voice of the new owners faction.")]
		public readonly bool LoseNewOwnerVoice = false;

		public object Create(ActorInitializer init) { return new CaptureNotification(this); }
	}

	public class CaptureNotification : INotifyCapture
	{
		readonly CaptureNotificationInfo info;
		public CaptureNotification(CaptureNotificationInfo info)
		{
			this.info = info;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			var faction = info.NewOwnerVoice ? newOwner.Faction.InternalName : oldOwner.Faction.InternalName;
			Game.Sound.PlayNotification(self.World.Map.Rules, newOwner, "Speech", info.Notification, faction);

			var loseFaction = info.LoseNewOwnerVoice ? newOwner.Faction.InternalName : oldOwner.Faction.InternalName;
			Game.Sound.PlayNotification(self.World.Map.Rules, oldOwner, "Speech", info.LoseNotification, loseFaction);
		}
	}
}
