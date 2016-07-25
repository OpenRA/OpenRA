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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Trigger upgrades during Attack trait activities.")]
	public class UpgradeOnAttackInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("Lasts until the attack is executed.")]
		[UpgradeGrantedReference] public readonly string[] PreparingAttackUpgrades = { };

		[Desc("Timed upgrade for the duration of the fire delay.")]
		[UpgradeGrantedReference] public readonly string[] AttackingUpgrades = { };

		[Desc("How long the timed attacking upgrade should last.")]
		public readonly int AttackingDelay = 0;

		public object Create(ActorInitializer init) { return new UpgradeOnAttack(init, this); }
	}

	public class UpgradeOnAttack : INotifyAttack
	{
		readonly Actor self;
		readonly UpgradeOnAttackInfo info;
		readonly UpgradeManager manager;

		public UpgradeOnAttack(ActorInitializer init, UpgradeOnAttackInfo info)
		{
			self = init.Self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			foreach (var up in info.PreparingAttackUpgrades)
				manager.GrantUpgrade(self, up, this);
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			foreach (var up in info.PreparingAttackUpgrades)
				manager.RevokeUpgrade(self, up, this);

			foreach (var up in info.AttackingUpgrades)
				manager.GrantTimedUpgrade(self, up, info.AttackingDelay, this);
		}
	}
}
