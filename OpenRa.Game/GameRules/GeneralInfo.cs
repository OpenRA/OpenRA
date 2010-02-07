
using System;
namespace OpenRa.GameRules
{
	public class GeneralInfo
	{
		/* Special Weapons */
		public readonly float GapRegenInterval =0;
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
		public readonly float BallisticScatter = 0;
		public readonly float C4Delay = 0;
		public readonly float ExpSpread = 0;
		public readonly int FireSupress = 0;
		public readonly float HomingScatter = 0;
		public readonly int MaxDamage = 0;
		public readonly int MinDamage = 0;
		public readonly bool OreExplosive = false;
		public readonly bool PlayerAutoCrush = false;
		public readonly bool PlayerReturnFire = false;
		public readonly bool PlayerScatter = false;
		public readonly bool TreeTargeting = false;
		public readonly int Incoming = 0;

		/* Income & Production */
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

		/* OpenRA-specific */
		public readonly float OreChance = 0;	/* chance of spreading to a particular eligible cell */
		public readonly int LowPowerSlowdown = 3;	/* build time multiplier */
	}
}
