#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class HarvesterAttackNotifierInfo : ITraitInfo
	{
		public readonly int NotifyInterval = 30;	// seconds

		public object Create(ActorInitializer init) { return new HarvesterAttackNotifier(this); }
	}

	public class HarvesterAttackNotifier : INotifyDamage
	{
		HarvesterAttackNotifierInfo info;

		public int lastAttackTime = -1;
		public CPos lastAttackLocation;

		public HarvesterAttackNotifier(HarvesterAttackNotifierInfo info) { this.info = info; }

		public void Damaged(Actor self, AttackInfo e)
		{
			// only track last hit against our base
			if (!self.HasTrait<Harvester>())
				return;

			// don't track self-damage
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			if (self.World.FrameNumber - lastAttackTime > info.NotifyInterval * 25)
				Sound.PlayNotification(self.Owner, "Speech", "HarvesterAttack", self.Owner.Country.Race);

			lastAttackLocation = self.CenterPosition.ToCPos();
			lastAttackTime = self.World.FrameNumber;
		}
	}
}

