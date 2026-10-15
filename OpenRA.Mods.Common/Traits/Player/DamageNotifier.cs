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

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Plays an audio notification and shows a radar ping when attacked.",
		"Attach this to the player actor.")]
	public class DamageNotifierInfo : TraitInfo
	{
		[Desc("Target types to notify about.",
			"Leave empty to notify about all target types.")]
		public readonly BitSet<TargetableType> ValidTargets = default;
		[Desc("Target types to ignore.",
			"This overrides ValidTargets.",
			"Leave empty to notify about all target types.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Minimum duration (in milliseconds) between notification events.",
			"Set to -1 to make notifications to play only once.")]
		public readonly int NotifyInterval = 30000;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 250;

		[NotificationReference("Speech")]
		[Desc("Speech notification type to play.")]
		public readonly string Notification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display.")]
		public readonly string TextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play to allies when under attack.",
			"Won't play a notification to allies if this is null.")]
		public readonly string AllyNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display to allies when under attack.")]
		public readonly string AllyTextNotification = null;

		public override object Create(ActorInitializer init) { return new DamageNotifier(init.Self, this); }
	}

	public class DamageNotifier : INotifyDamage
	{
		readonly RadarPings radarPings;
		readonly DamageNotifierInfo info;

		long nextActiveTick = 0;

		public DamageNotifier(Actor self, DamageNotifierInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			var localPlayer = self.World.LocalPlayer;

			if (localPlayer == null || localPlayer.Spectating)
				return;

			if (e.Attacker == null)
				return;

			if (e.Attacker.Owner == self.Owner)
				return;

			if (e.Attacker == self.World.WorldActor)
				return;

			if (!info.ValidTargets.IsEmpty && !self.GetEnabledTargetTypes().Overlaps(info.ValidTargets))
				return;

			if (!info.InvalidTargets.IsEmpty && self.GetEnabledTargetTypes().Overlaps(info.InvalidTargets))
				return;

			if (e.Attacker.Owner.IsAlliedWith(self.Owner) && e.Damage.Value <= 0)
				return;

			if (Game.RunTime >= nextActiveTick)
			{
				var visible = self.Owner.IsAlliedWith(self.World.RenderPlayer);
				radarPings?.Add(() => visible, self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);
				if (!visible)
					return;

				var rules = self.World.Map.Rules;
				if (self.Owner == localPlayer)
				{
					Game.Sound.PlayNotification(rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(self.Owner, info.TextNotification);
				}
				else if (localPlayer.IsAlliedWith(self.Owner) && localPlayer != e.Attacker.Owner)
				{
					Game.Sound.PlayNotification(rules, localPlayer, "Speech", info.AllyNotification, localPlayer.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(localPlayer, info.AllyTextNotification);
				}

				nextActiveTick = Game.RunTime + info.NotifyInterval;
				if (info.NotifyInterval < 0 || nextActiveTick < 0) // Notify once if NotifyInterval is negative or correct overflow
					nextActiveTick = long.MaxValue;
			}
		}
	}
}
