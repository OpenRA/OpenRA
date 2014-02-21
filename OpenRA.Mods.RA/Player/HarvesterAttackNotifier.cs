#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class HarvesterAttackNotifierInfo : ITraitInfo
	{
		public readonly int NotifyInterval = 30;	// seconds
		public readonly Color RadarPingColor = Color.Red;
		public readonly int RadarPingDuration = 10 * 25;

		public object Create(ActorInitializer init) { return new HarvesterAttackNotifier(init.self, this); }
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
			// only track last hit against our base
			if (!self.HasTrait<Harvester>())
				return;

			// don't track self-damage
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			if (self.World.FrameNumber - lastAttackTime > info.NotifyInterval * 25)
			{
				Sound.PlayNotification(self.Owner, "Speech", "HarvesterAttack", self.Owner.Country.Race);

				if (radarPings != null)
					radarPings.Add(() => self.Owner == self.World.LocalPlayer, self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);
			}

			lastAttackTime = self.World.FrameNumber;
		}
	}
}

