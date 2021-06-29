#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
	[Desc("Plays an audio notification and shows a radar ping when a harvester is attacked.",
		"Attach this to the player actor.")]
	[TraitLocation(SystemActors.Player)]
	public class HarvesterAttackNotifierInfo : TraitInfo
	{
		[Desc("Minimum duration (in milliseconds) between notification events.")]
		public readonly int NotifyInterval = 30000;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 250;

		[NotificationReference("Speech")]
		[Desc("The audio notification type to play.")]
		public string Notification = "HarvesterAttack";

		public override object Create(ActorInitializer init) { return new HarvesterAttackNotifier(init.Self, this); }
	}

	public class HarvesterAttackNotifier : INotifyDamage
	{
		readonly RadarPings radarPings;
		readonly HarvesterAttackNotifierInfo info;

		long lastAttackTime;

		public HarvesterAttackNotifier(Actor self, HarvesterAttackNotifierInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
			lastAttackTime = -info.NotifyInterval;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			// Don't track self-damage
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			// Only track last hit against our harvesters
			if (!self.Info.HasTraitInfo<HarvesterInfo>())
				return;

			if (Game.RunTime > lastAttackTime + info.NotifyInterval)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);
				radarPings?.Add(() => self.Owner.IsAlliedWith(self.World.RenderPlayer), self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);

				lastAttackTime = Game.RunTime;
			}
		}
	}
}
