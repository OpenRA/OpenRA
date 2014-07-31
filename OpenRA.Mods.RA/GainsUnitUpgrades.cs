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
		public readonly int FirepowerMaxLevel = 15;
		public readonly int FirepowerModifier = 20;
		public readonly int ArmorMaxLevel = 8;
		public readonly int ArmorModifier = 10;
		public readonly int SpeedMaxLevel = 15;
		public readonly int SpeedModifier = 20;
		// TODO: weapon range, rate of fire modifiers. potentially a vision modifier.

		public object Create(ActorInitializer init) { return new GainsUnitUpgrades(this); }
	}

	public class GainsUnitUpgrades : IFirepowerModifier, IDamageModifier, ISpeedModifier
	{
		GainsUnitUpgradesInfo info;
		[Sync] public int FirepowerLevel = 0;
		[Sync] public int SpeedLevel = 0;
		[Sync] public int ArmorLevel = 0;

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

		public int GetFirepowerModifier()
		{
			return FirepowerLevel > 0 ? FirepowerLevel * info.FirepowerModifier : 100;
		}

		public int GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return ArmorLevel > 0 ? 100 - ArmorLevel * info.ArmorModifier : 100;
		}

		public int GetSpeedModifier()
		{
			return SpeedLevel > 0 ? (100 + SpeedLevel * info.SpeedModifier) : 100;
		}
	}

	public enum UnitUpgrade
	{
		Firepower = 0,
		Armor = 1,
		Speed = 2
	}
}
