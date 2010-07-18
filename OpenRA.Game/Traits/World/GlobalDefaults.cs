#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

// THIS STUFF NEEDS TO GO DIE IN A FIRE.

namespace OpenRA.Traits
{
	public class GlobalDefaultsInfo : TraitInfo<GlobalDefaults>
	{
		/* Special Weapons */
		public readonly float GapRegenInterval = 0.1f;
		public readonly int BadgerBombCount = 1;

		/* Chrono Side Effects */
		public readonly float QuakeChance = 0.2f;
		public readonly float QuakeDamage = 0.33f;		/* percent */
		public readonly float VortexChance = 0.2f;
		public readonly int VortexDamage = 200;
		public readonly int VortexRange = 10;
		public readonly int VortexSpeed = 10;
		
		/* Repair & Refit */
		public readonly float RefundPercent = 0.5f;
		public readonly float ReloadRate = 0.04f;
		public readonly float RepairPercent = 0.2f;
		public readonly float RepairRate = 0.016f;
		public readonly int RepairStep = 7;

		
		/* Combat & Damage */
		public readonly float TurboBoost = 1.5f;
		public readonly float BallisticScatter = 1.0f;
		public readonly float ExpSpread = 0.3f;
		public readonly int FireSupress = 1;
		public readonly float HomingScatter = 2.0f;
		public readonly int MaxDamage = 1000;
		public readonly int MinDamage = 1;
		public readonly bool OreExplosive = false;
		public readonly bool PlayerAutoCrush = false;
		public readonly bool PlayerReturnFire = false;
		public readonly bool PlayerScatter = false;
		public readonly bool TreeTargeting = false;
		public readonly int Incoming = 10;
		
		/* Income & Production */
		public readonly float BuildupTime = 0.06f;
		public readonly float OreTruckRate = 1;
		public readonly bool SeparateAircraft = false;
		public readonly float SurvivorRate = 0.4f;
		
		/* Audo/Visual Map Controls */
		public readonly bool AllyReveal = true;
		public readonly float ConditionRed = 0.25f;
		public readonly float ConditionYellow = 0.5f;
		public readonly int DropZoneRadius = 4;
		public readonly bool EnemyHealth = true;
		public readonly int Gravity = 3;
		public readonly float IdleActionFrequency = 0.1f;
		public readonly float MessageDelay = 0.6f;
		public readonly float MovieTime = 0.06f;
		public readonly bool NamedCivilians = false;
		public readonly float SavourDelay = 0.03f;
		
		public readonly int SpeakDelay = 2;
		public readonly int TimerWarning = 2;
		public readonly bool FlashLowPower = true;

		/* Computer & Movement Controls */
		public readonly bool CurleyShuffle = false;
		public readonly float BaseBias = 2.0f;
		public readonly float BaseDefenseDelay = 0.25f;
		public readonly float CloseEnough = 2.75f;
		public readonly int DamageDelay = 1;
		public readonly int GameSpeeBias = 1;
		public readonly int LZScanRadius = 16;
		public readonly bool MineAware = true;
		public readonly float Stray = 2.0f;
		public readonly float SuspendDelay = 2.0f;
		public readonly int SuspendPriority = 20;
		public readonly float TeamDelay = 0.6f;
	}

	public class GlobalDefaults {}
}
