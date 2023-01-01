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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	public class CaptureNotificationInfo : TraitInfo
	{
		[NotificationReference("Speech")]
		[Desc("Speech notification to play to the new owner.")]
		public readonly string Notification = "BuildingCaptured";

		[Desc("Text notification to display to the new owner.")]
		public readonly string TextNotification = null;

		[Desc("Specifies if Notification is played with the voice of the new owners faction.")]
		public readonly bool NewOwnerVoice = true;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play to the old owner.")]
		public readonly string LoseNotification = null;

		[Desc("Text notification to display to the old owner.")]
		public readonly string LoseTextNotification = null;

		[Desc("Specifies if LoseNotification is played with the voice of the new owners faction.")]
		public readonly bool LoseNewOwnerVoice = false;

		public override object Create(ActorInitializer init) { return new CaptureNotification(this); }
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
			TextNotificationsManager.AddTransientLine(info.TextNotification, newOwner);

			var loseFaction = info.LoseNewOwnerVoice ? newOwner.Faction.InternalName : oldOwner.Faction.InternalName;
			Game.Sound.PlayNotification(self.World.Map.Rules, oldOwner, "Speech", info.LoseNotification, loseFaction);
			TextNotificationsManager.AddTransientLine(info.LoseTextNotification, oldOwner);
		}
	}
}
