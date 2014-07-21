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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor has properties that upgrade when a specific criteria is met.")]
	public class GainsUnitUpgradesInfo : ITraitInfo
	{
		[Desc("The maximum upgrade level for firepower.")]
		public readonly int FirepowerMaxLevel = 15;

		[Desc("The boost to firepower per upgrade level.")]
		public readonly float FirepowerModifier = .2f;

		[Desc("The maximum upgrade level for armor.")]
		public readonly int ArmorMaxLevel = 15;

		[Desc("The boost to armor per upgrade level.")]
		public readonly float ArmorModifier = .2f;

		[Desc("The maximum upgrade level for disable armor.")]
		public readonly int DisableArmorMaxLevel = 15;

		[Desc("The boost to disable armor per upgrade level.")]
		public readonly float DisableArmorModifier = .2f;

		[Desc("The maximum upgrade level for speed.")]
		public readonly int SpeedMaxLevel = 15;

		[Desc("The boost to speed per upgrade level.")]
		public readonly decimal SpeedModifier = .2m;
		// TODO: weapon range, rate of fire modifiers. potentially a vision modifier.

		public object Create(ActorInitializer init) { return new GainsUnitUpgrades(this); }
	}

	public class GainsUnitUpgrades : IFirepowerModifier, IDamageModifier, ISpeedModifier, IDisableTicksModifier
	{
		GainsUnitUpgradesInfo info;
		[Sync] public int FirepowerLevel = 0;
		[Sync] public int SpeedLevel = 0;
		[Sync] public int ArmorLevel = 0;
		[Sync] public int DisableArmorLevel = 0;

		public GainsUnitUpgrades(GainsUnitUpgradesInfo info)
		{
			this.info = info;
		}

		public bool CanGainUnitUpgrade(UnitUpgrade? upgrade)
		{
			if (upgrade == UnitUpgrade.Firepower)
				return FirepowerLevel < info.FirepowerMaxLevel;
			if (upgrade == UnitUpgrade.Armor)
				return ArmorLevel < info.ArmorMaxLevel;
			if (upgrade == UnitUpgrade.Speed)
				return SpeedLevel < info.SpeedMaxLevel;

			return false;
		}

		public void GiveUnitUpgrade(UnitUpgrade? upgrade, int numLevels)
		{
			if (upgrade == UnitUpgrade.Firepower)
				FirepowerLevel = Math.Min(FirepowerLevel + numLevels, info.FirepowerMaxLevel);
			else if (upgrade == UnitUpgrade.Armor)
				ArmorLevel = Math.Min(ArmorLevel + numLevels, info.ArmorMaxLevel);
			else if (upgrade == UnitUpgrade.Speed)
				SpeedLevel = Math.Min(SpeedLevel + numLevels, info.SpeedMaxLevel);
		}

		public float GetFirepowerModifier()
		{
			return FirepowerLevel > 0 ? (1 + FirepowerLevel * info.FirepowerModifier) : 1;
		}

		public float GetDamageModifier(Actor attacker, DamagerWarheadInfo warhead)
		{
			return ArmorLevel > 0 ? (1 / (1 + ArmorLevel * info.ArmorModifier)) : 1;
		}

		public float GetDisableTicksModifier(Actor attacker, DisablerWarheadInfo warhead)
		{
			return DisableArmorLevel > 0 ? (1 / (1 + DisableArmorLevel * info.DisableArmorModifier)) : 1;
		}

		public decimal GetSpeedModifier()
		{
			return SpeedLevel > 0 ? (1m + SpeedLevel * info.SpeedModifier) : 1m;
		}
	}

	public enum UnitUpgrade
	{
		Firepower = 0,
		Armor = 1,
		Speed = 2
	}
}
