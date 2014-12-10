using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants an upgrade to the actor when a certain HP loss threshold is exceeded. (can also have area of effect)")]
	class GrantUpgradeOnDamageStateInfo : ITraitInfo, Requires<HealthInfo>, Requires<UpgradeManagerInfo>
	{
		[Desc("The threshold where the upgrades will be granted. Damage State.")]
		public readonly DamageState Threshold = DamageState.Heavy;
		
		[Desc("The upgrades to apply.")]
		public readonly string[] Upgrades = { };

		[Desc("Only degrant the upgrade when fully healed.")]
		public readonly bool RequiresFullHealing = false;

		[Desc("When this is set to true it grants the upgrade when the HP is over the threshold.", "RequiresFullHealing also changes so it only grants the upgrade again when the actor has been fully healed")]
		public readonly bool InvertLogic = false;

		public object Create(ActorInitializer init) { return new GrantUpgradeOnDamageState(init.self, this); }
	}

	class GrantUpgradeOnDamageState : INotifyDamage, INotifyAddedToWorld
	{
		readonly GrantUpgradeOnDamageStateInfo info;
		readonly Health health;
		readonly UpgradeManager um;

		bool upgraded = false;

		public GrantUpgradeOnDamageState(Actor self, GrantUpgradeOnDamageStateInfo info)
		{
			this.info = info;
			health = self.Trait<Health>();
			um = self.Trait<UpgradeManager>();
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// 
			if ((!upgraded == info.InvertLogic) && info.RequiresFullHealing && health.DamageState != DamageState.Undamaged)
				return;
			// Upgraded and below/equal to the threshold
			if (upgraded && ((health.DamageState >= info.Threshold) == !info.InvertLogic))
				return;
			// Not upgraded, does not require 
			if (!upgraded && ((health.DamageState < info.Threshold) == !info.InvertLogic))
				return;

			foreach (var u in info.Upgrades)
			{
				resolveUpgrade(self, u);
			}
		}

		public void resolveUpgrade(Actor self, string u)
		{
			if ((health.DamageState >= info.Threshold) == !info.InvertLogic)
			{
				um.GrantUpgrade(self, u, this);
				upgraded = true;
			}
			else if ((health.DamageState < info.Threshold) == !info.InvertLogic)
			{
				um.RevokeUpgrade(self, u, this);
				upgraded = false;
			}
		}

		public void AddedToWorld(Actor self)
		{
			if (!info.InvertLogic)
				return;

			foreach (var u in info.Upgrades)
			{
				resolveUpgrade(self, u);
			}
		}
	}
}
