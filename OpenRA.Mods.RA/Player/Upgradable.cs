using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class UpgradableInfo : ITraitInfo
	{
		public readonly string[] Prerequisites = {};

		public readonly float ArmorModifier = 1;
		public readonly float DamageModifier = 1;
		public readonly decimal SpeedModifier = 1;

		public object Create(ActorInitializer init) { return new Upgradable(init.self, this); }
	}

	public class Upgradable : ITechTreeElement, IFirepowerModifier, ISpeedModifier, IDamageModifier
	{
		UpgradableInfo upgradeInfo;
		bool upgraded;

		public bool Upgraded { get { return upgraded; } }

		public Upgradable(Actor actor, UpgradableInfo upgradeInfo)
		{
			this.upgradeInfo = upgradeInfo;
			actor.Owner.PlayerActor.Trait<TechTree>().Add(actor.Info.Name + this.ToString(), upgradeInfo.Prerequisites, 1, this);
		}

		public void PrerequisitesAvailable(string key)
		{
			upgraded = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			upgraded = false;
		}

		public void PrerequisitesItemHidden(string key) { }

		public void PrerequisitesItemVisable(string key) { }

		public float GetDamageModifier(Actor attacker, GameRules.WarheadInfo warhead)
		{
			if (upgradeInfo.ArmorModifier >= 0)
				return upgraded ? upgradeInfo.ArmorModifier : 1;
			return upgraded ? 1 / -upgradeInfo.ArmorModifier : 1;
		}

		public decimal GetSpeedModifier()
		{
			if (upgradeInfo.ArmorModifier >= 0)
				return upgraded ? upgradeInfo.SpeedModifier : 1;
			return upgraded ? 1 / -upgradeInfo.SpeedModifier : 1;
		}

		public float GetFirepowerModifier()
		{
			if (upgradeInfo.ArmorModifier >= 0)
				return upgraded ? upgradeInfo.DamageModifier : 1;
			return upgraded ? 1 / -upgradeInfo.DamageModifier : 1;
		}
	}
}
