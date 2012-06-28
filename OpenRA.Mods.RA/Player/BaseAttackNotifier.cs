#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class BaseAttackNotifierInfo : ITraitInfo
	{
		public readonly int NotifyInterval = 30;	/* seconds */
		public readonly string Audio = "baseatk1.aud";
		public readonly string Race = null;

		public object Create(ActorInitializer init) { return new BaseAttackNotifier(this); }
	}

	public class BaseAttackNotifier : INotifyDamage
	{
		BaseAttackNotifierInfo info;

		public int lastAttackTime = -1;
		public CPos lastAttackLocation;

		public BaseAttackNotifier(BaseAttackNotifierInfo info) { this.info = info; }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (info.Race != null && info.Race != self.Owner.Country.Race) return;
			/* only track last hit against our base */
			if (!self.HasTrait<Building>())
				return;

			/* don't track self-damage */
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			if (self.World.FrameNumber - lastAttackTime > info.NotifyInterval * 25)
				Sound.PlayToPlayer(self.Owner, info.Audio);

			lastAttackLocation = self.CenterLocation.ToCPos();
			lastAttackTime = self.World.FrameNumber;
		}
	}
}

