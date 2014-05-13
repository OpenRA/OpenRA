#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
		public readonly string Key;
		public readonly string[] Prerequisites = {};

		public readonly float ArmorModifier = 1;
		public readonly float DamageModifier = 1;
		public readonly decimal SpeedModifier = 1;

		public object Create(ActorInitializer init) { return new Upgradable(init.self, this); }
	}

	public class Upgradable : IFirepowerModifier, ISpeedModifier, IDamageModifier, INotifyUpgrade
	{
		public readonly UpgradableInfo Info;

		bool Upgraded { get; set; }
		public string UpgradeKey { get { return Info.Key; } }
		public string[] UpgradePrerequisites { get { return Info.Prerequisites; } }

		public Upgradable(Actor actor, UpgradableInfo info)
		{
			this.Info = info;
		}

		public float GetDamageModifier(Actor attacker, GameRules.WarheadInfo warhead)
		{
			return Upgraded ? Info.ArmorModifier : 1;
		}

		public decimal GetSpeedModifier()
		{
			return Upgraded ? Info.SpeedModifier : 1;
		}

		public float GetFirepowerModifier()
		{
			return Upgraded ? Info.DamageModifier : 1;
		}

		public void OnUpgrade(string key, bool isUpgrade)
		{
			Upgraded = isUpgrade;
		}
	}
}
