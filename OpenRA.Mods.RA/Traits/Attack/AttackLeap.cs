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

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Move onto the target then execute the attack.")]
	public class AttackLeapInfo : AttackFrontalInfo, Requires<MobileInfo>, Requires<UpgradeManagerInfo>
	{
		[Desc("Leap speed (in units/tick).")]
		public readonly WDist Speed = new WDist(426);

		[Desc("Upgrades that last from start of leap till attack.")]
		[UpgradeGrantedReference] public readonly string[] LeapUpgrades = { };

		public override object Create(ActorInitializer init) { return new AttackLeap(init.Self, this); }
	}

	public class AttackLeap : AttackFrontal
	{
		readonly AttackLeapInfo info;
		readonly Mobile mobile;
		readonly UpgradeManager manager;

		public AttackLeap(Actor self, AttackLeapInfo info)
			: base(self, info)
		{
			this.info = info;
			mobile = self.Trait<Mobile>();
			manager = self.Trait<UpgradeManager>();
		}

		public override void DoAttack(Actor self, Target target, IEnumerable<Armament> armaments = null)
		{
			if (IsAttacking || !mobile.CanMoveFreelyInto(target.Actor.Location))
				return;

			var origin = self.World.Map.CenterOfSubCell(self.Location, mobile.FromSubCell);
			var targetMobile = self.TraitOrDefault<Mobile>();
			var targetSubcell = targetMobile != null ? targetMobile.FromSubCell : SubCell.Any;
			var destination = self.World.Map.CenterOfSubCell(target.Actor.Location, targetSubcell);
			var length = Math.Max((origin - destination).Length / info.Speed.Length, 1);
			self.QueueActivity(new Leap(self, origin, destination, length, mobile));
			foreach (var up in info.LeapUpgrades)
				manager.GrantUpgrade(self, up, this);

			Game.RunAfterDelay(length, () => {
				foreach (var up in info.LeapUpgrades)
					manager.RevokeUpgrade(self, up, this);

				if (!self.IsDead)
					base.DoAttack(self, target, armaments);
			});
		}
	}
}
