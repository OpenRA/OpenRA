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
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor has properties that upgrade when a specific criteria is met.")]
	public class GainsStatUpgradesInfo : ITraitInfo
	{
		public readonly string FirepowerUpgrade = "firepower";
		public readonly int[] FirepowerModifier = { 110, 115, 120, 130 };

		public readonly string DamageUpgrade = "damage";
		public readonly int[] DamageModifier = { 91, 87, 83, 65 };

		public readonly string SpeedUpgrade = "speed";
		public readonly int[] SpeedModifier = { 110, 115, 120, 150 };

		public readonly string ReloadUpgrade = "reload";
		public readonly int[] ReloadModifier = { 95, 90, 85, 75 };

		public readonly string InaccuracyUpgrade = "inaccuracy";
		public readonly int[] InaccuracyModifier = { 90, 80, 70, 50 };

		public object Create(ActorInitializer init) { return new GainsStatUpgrades(this); }
	}

	public class GainsStatUpgrades : IUpgradable, IFirepowerModifier, IDamageModifier, ISpeedModifier, IReloadModifier, IInaccuracyModifier, IDisabledTrait
	{
		readonly GainsStatUpgradesInfo info;
		[Sync] int firepowerLevel = 0;
		[Sync] int speedLevel = 0;
		[Sync] int damageLevel = 0;
		[Sync] int reloadLevel = 0;
		[Sync] int inaccuracyLevel = 0;
		public bool IsTraitDisabled { get { return firepowerLevel == 0 && speedLevel == 0 && damageLevel == 0 && reloadLevel == 0 && inaccuracyLevel == 0; } }
		public IEnumerable<string> UpgradeTypes
		{
			get
			{
				yield return info.FirepowerUpgrade;
				yield return info.DamageUpgrade;
				yield return info.SpeedUpgrade;
				yield return info.ReloadUpgrade;
				yield return info.InaccuracyUpgrade;
			}
		}

		public GainsStatUpgrades(GainsStatUpgradesInfo info)
		{
			this.info = info;
		}

		public bool AcceptsUpgradeLevel(Actor self, string type, int level)
		{
			if (level < 0)
				return false;

			if (type == info.FirepowerUpgrade)
				return level <= info.FirepowerModifier.Length;

			if (type == info.DamageUpgrade)
				return level <= info.DamageModifier.Length;

			if (type == info.SpeedUpgrade)
				return level <= info.SpeedModifier.Length;

			if (type == info.ReloadUpgrade)
				return level <= info.ReloadModifier.Length;

			if (type == info.InaccuracyUpgrade)
				return level <= info.InaccuracyModifier.Length;

			return false;
		}

		public void UpgradeLevelChanged(Actor self, string type, int oldLevel, int newLevel)
		{
			if (type == info.FirepowerUpgrade)
				firepowerLevel = newLevel.Clamp(0, info.FirepowerModifier.Length);
			else if (type == info.DamageUpgrade)
				damageLevel = newLevel.Clamp(0, info.DamageModifier.Length);
			else if (type == info.SpeedUpgrade)
				speedLevel = newLevel.Clamp(0, info.SpeedModifier.Length);
			else if (type == info.ReloadUpgrade)
				reloadLevel = newLevel.Clamp(0, info.ReloadModifier.Length);
			else if (type == info.InaccuracyUpgrade)
				inaccuracyLevel = newLevel.Clamp(0, info.InaccuracyModifier.Length);
		}

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead)
		{
			return damageLevel > 0 ? info.DamageModifier[damageLevel - 1] : 100;
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

		public int GetInaccuracyModifier()
		{
			return inaccuracyLevel > 0 ? info.InaccuracyModifier[inaccuracyLevel - 1] : 100;
		}
	}
}
