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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor has properties that upgrade when a specific criteria is met.")]
	public class GainsStatUpgradesInfo : ITraitInfo
	{
		public readonly string FirepowerUpgrade = "firepower";
		public readonly int[] FirepowerModifier = { 110, 115, 120, 130 };

		public readonly string ArmorUpgrade = "armor";
		public readonly int[] ArmorModifier = { 110, 120, 130, 150 };

		public readonly string SpeedUpgrade = "speed";
		public readonly int[] SpeedModifier = { 110, 115, 120, 150 };

		public readonly string ReloadUpgrade = "reload";
		public readonly int[] ReloadModifier = { 95, 90, 85, 75 };

		public object Create(ActorInitializer init) { return new GainsStatUpgrades(this); }
	}

	public class GainsStatUpgrades : IUpgradable, IFirepowerModifier, IDamageModifier, ISpeedModifier, IReloadModifier
	{
		readonly GainsStatUpgradesInfo info;
		[Sync] int firepowerLevel = 0;
		[Sync] int speedLevel = 0;
		[Sync] int armorLevel = 0;
		[Sync] int reloadLevel = 0;

		public GainsStatUpgrades(GainsStatUpgradesInfo info)
		{
			this.info = info;
		}

		public bool AcceptsUpgrade(string type)
		{
			return (type == info.FirepowerUpgrade && firepowerLevel < info.FirepowerModifier.Length)
				|| (type == info.ArmorUpgrade && armorLevel < info.ArmorModifier.Length)
				|| (type == info.SpeedUpgrade && speedLevel < info.SpeedModifier.Length)
				|| (type == info.ReloadUpgrade && reloadLevel < info.ReloadModifier.Length);
		}

		public void UpgradeAvailable(Actor self, string type, bool available)
		{
			var mod = available ? 1 : -1;
			if (type == info.FirepowerUpgrade)
				firepowerLevel = (firepowerLevel + mod).Clamp(0, info.FirepowerModifier.Length);
			else if (type == info.ArmorUpgrade)
				armorLevel = (armorLevel + mod).Clamp(0, info.ArmorModifier.Length);
			else if (type == info.SpeedUpgrade)
				speedLevel = (speedLevel + mod).Clamp(0, info.SpeedModifier.Length);
			else if (type == info.ReloadUpgrade)
				reloadLevel = (reloadLevel + mod).Clamp(0, info.ReloadModifier.Length);
		}

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead)
		{
			return armorLevel > 0 ? 1 / info.ArmorModifier[armorLevel - 1] : 100;
		}

		public int GetFirepowerModifier()
		{
			return firepowerLevel > 0 ? info.FirepowerModifier[firepowerLevel - 1] : 100;
		}

		public int GetSpeedModifier()
		{
			return speedLevel > 0 ? info.SpeedModifier[speedLevel - 1] : 100;
		}

		public int GetReloadModifier()
		{
			return reloadLevel > 0 ? info.ReloadModifier[reloadLevel - 1] : 100;
		}
	}
}
