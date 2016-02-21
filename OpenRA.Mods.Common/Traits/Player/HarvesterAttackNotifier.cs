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

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Plays an audio notification and shows a radar ping when a harvester is attacked.",
		"Attach this to the player actor.")]
	public class HarvesterAttackNotifierInfo : ITraitInfo
	{
		[Desc("Minimum duration (in seconds) between notification events.")]
		public readonly int NotifyInterval = 30;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 10 * 25;

		[Desc("The audio notification type to play.")]
		public string Notification = "HarvesterAttack";

		public object Create(ActorInitializer init) { return new HarvesterAttackNotifier(init.Self, this); }
	}

	public class HarvesterAttackNotifier : INotifyDamage
	{
		readonly RadarPings radarPings;
		readonly HarvesterAttackNotifierInfo info;

		int lastAttackTime;

		public HarvesterAttackNotifier(Actor self, HarvesterAttackNotifierInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
			lastAttackTime = -info.NotifyInterval * 25;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// Don't track self-damage
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			// Only track last hit against our harvesters
			if (!self.Info.HasTraitInfo<HarvesterInfo>())
				return;

			if (self.World.WorldTick - lastAttackTime > info.NotifyInterval * 25)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);

				if (radarPings != null)
					radarPings.Add(() => self.Owner.IsAlliedWith(self.World.RenderPlayer), self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);
			}

			lastAttackTime = self.World.WorldTick;
		}
	}
}