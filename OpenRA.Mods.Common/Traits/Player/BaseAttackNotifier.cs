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
	[Desc("Plays an audio notification and shows a radar ping when a building is attacked.",
		"Attach this to the player actor.")]
	public class BaseAttackNotifierInfo : ITraitInfo
	{
		[Desc("Minimum duration (in seconds) between notification events.")]
		public readonly int NotifyInterval = 30;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 10 * 25;

		[Desc("The audio notification type to play.")]
		public string Notification = "BaseAttack";

		[Desc("The audio notification to play to allies when under attack.",
			"Won't play a notification to allies if this is null.")]
		public string AllyNotification = null;

		public object Create(ActorInitializer init) { return new BaseAttackNotifier(init.Self, this); }
	}

	public class BaseAttackNotifier : INotifyDamage
	{
		readonly RadarPings radarPings;
		readonly BaseAttackNotifierInfo info;

		int lastAttackTime;

		public BaseAttackNotifier(Actor self, BaseAttackNotifierInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
			lastAttackTime = -info.NotifyInterval * 25;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Attacker == null)
				return;

			if (e.Attacker.Owner == self.Owner)
				return;

			if (e.Attacker == self.World.WorldActor)
				return;

			// Only track last hit against our base
			if (!self.Info.HasTraitInfo<BuildingInfo>())
				return;

			if (e.Attacker.Owner.IsAlliedWith(self.Owner) && e.Damage.Value <= 0)
				return;

			if (self.World.WorldTick - lastAttackTime > info.NotifyInterval * 25)
			{
				var rules = self.World.Map.Rules;
				Game.Sound.PlayNotification(rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);

				if (info.AllyNotification != null)
					foreach (Player p in self.World.Players)
						if (p != self.Owner && p.IsAlliedWith(self.Owner) && p != e.Attacker.Owner)
							Game.Sound.PlayNotification(rules, p, "Speech", info.AllyNotification, p.Faction.InternalName);

				if (radarPings != null)
					radarPings.Add(() => self.Owner.IsAlliedWith(self.World.RenderPlayer), self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);
			}

			lastAttackTime = self.World.WorldTick;
		}
	}
}
