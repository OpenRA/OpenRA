
using System;
namespace OpenRa.Game.GameRules
{
	class GeneralInfo
	{
		/* Crates */
		public readonly int CrateMinimum = 0;
		public readonly int CrateMaximum = 0;
		public readonly float CrateRadius = 0;
		public readonly float CrateRegen = 0;
		public readonly string UnitCrateType = null;	/* =none, if any */
		public readonly float WaterCrateChance = 0;

		public readonly int SoloCrateMoney = 2000;
		public readonly string SilverCrate = null;		/* solo play crate contents */
		public readonly string WaterCrate = null;
		public readonly string WoodCrate = null;

		/* Special Weapons */
		public readonly int ChronoDuration = 0;
		public readonly bool ChronoKillCargo = true;
		[Obsolete] public readonly int ChronoTechLevel = -1;
		[Obsolete] public readonly int GPSTechLevel = -1;
		public readonly int GapRadius = 0;
		public readonly float GapRegenInterval =0;
		public readonly float IronCurtain = 0;		/* minutes */
		[Obsolete] public readonly int ParaTech = -1;
		[Obsolete] public readonly int ParabombTech = -1;
		public readonly int RadarJamRadius = 1;
		[Obsolete] public readonly int SpyPlaneTech = -1;
		public readonly int BadgerBombCount = 1;

		/* Chrono Side Effects */
		public readonly float QuakeChance = 0;
		public readonly float QuakeDamage = 0;		/* percent */
		public readonly float VortexChance = 0;
		public readonly int VortexDamage = 0;
		public readonly int VortexRange = 0;
		public readonly int VortexSpeed = 0;

		/* Repair & Refit */
		public readonly float RefundPercent = 0;
		public readonly float ReloadRate = 0;
		public readonly float RepairPercent = 0;
		public readonly float RepairRate = 0;
		public readonly int RepairStep = 0;
		public readonly float URepairPercent = 0;
		public readonly int URepairStep = 0;

		/* Combat & Damage */
		public readonly float TurboBoost = 1.5f;
		public readonly int APMineDamage = 0;
		public readonly int AVMineDamage = 0;
		public readonly int AtomDamage = 0;
		public readonly float BallisticScatter = 0;
		public readonly int BridgeStrength = 0;
		public readonly float C4Delay = 0;
		public readonly float Crush = 0;
		public readonly float ExpSpread = 0;
		public readonly int FireSupress = 0;
		public readonly float HomingScatter = 0;
		public readonly int MaxDamage = 0;
		public readonly int MinDamage = 0;
		public readonly bool OreExplosive = false;
		public readonly bool PlayerAutoCrush = false;
		public readonly bool PlayerReturnFire = false;
		public readonly bool PlayerScatter = false;
		public readonly float ProneDamage = 0;
		public readonly bool TreeTargeting = false;
		public readonly int Incoming = 0;

		/* Income & Production */
		public readonly int BailCount = 0;
		public readonly float BuildSpeed = 0;
		public readonly float BuildupTime = 0;
		public readonly int GemValue = 0;
		public readonly int GoldValue = 0;
		public readonly float GrowthRate = 0;
		public readonly bool OreGrows = true;
		public readonly bool OreSpreads = true;
		public readonly float OreTruckRate = 0;
		public readonly bool SeparateAircraft = true;
		public readonly float SurvivorRate = 0;

		/* Audo/Visual Map Controls */
		public readonly bool AllyReveal = true;
		public readonly float ConditionRed = 0;
		public readonly float ConditionYellow = 0;
		public readonly int DropZoneRadius = 0;
		public readonly bool EnemyHealth = true;
		public readonly int Gravity = 0;
		public readonly float IdleActionFrequency = 0;
		public readonly float MessageDelay = 0;
		public readonly float MovieTime = 0;
		public readonly bool NamedCivilians = false;
		public readonly float SavourDelay = 0;
		public readonly int ShroudRate = 0;
		public readonly int SpeakDelay = 0;
		public readonly int TimerWarning = 0;
		public readonly bool FlashLowPower = false;

		/* Computer & Movement Controls */
		public readonly bool CurleyShuffle = false;
		public readonly float BaseBias = 0;
		public readonly float BaseDefenseDelay = 0;
		public readonly float CloseEnough = 0;
		public readonly int DamageDelay = 0;
		public readonly int GameSpeeBias = 0;
		public readonly int LZScanRadius = 0;
		public readonly bool MineAware = false;
		public readonly float Stray = 0;
		public readonly float SubmergeDelay = 0;
		public readonly float SuspendDelay = 0;
		public readonly int SuspendPriority = 0;
		public readonly float TeamDelay = 0;

		/* Misc */
		[Obsolete]
		public readonly bool FineDiffControl = false;
		public readonly bool MCVUndeploy = false;

		/* OpenRA-specific */
		public readonly float OreChance = 0;	/* chance of spreading to a particular eligible cell */
		public readonly int LowPowerSlowdown = 3;	/* build time multiplier */
	}
}
