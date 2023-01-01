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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	class ActorLostNotificationInfo : ConditionalTraitInfo
	{
		[NotificationReference("Speech")]
		[Desc("Speech notification to play.")]
		public readonly string Notification = "UnitLost";

		[Desc("Text notification to display.")]
		public readonly string TextNotification = null;

		public readonly bool NotifyAll = false;

		public override object Create(ActorInitializer init) { return new ActorLostNotification(this); }
	}

	class ActorLostNotification : ConditionalTrait<ActorLostNotificationInfo>, INotifyKilled
	{
		public ActorLostNotification(ActorLostNotificationInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			var localPlayer = self.World.LocalPlayer;

			if (localPlayer == null || localPlayer.Spectating)
				return;

			var player = Info.NotifyAll ? localPlayer : self.Owner;

			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", Info.Notification, self.Owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(Info.TextNotification, player);
		}
	}
}
