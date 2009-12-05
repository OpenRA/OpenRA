
namespace OpenRa.Game.GameRules
{
	class GeneralInfo
	{
		/* Crates */
		public readonly int CrateMinimum;
		public readonly int CrateMaximum;
		public readonly float CrateRadius;
		public readonly float CrateRegen;
		public readonly string UnitCrateType;	/* =none, if any */
		public readonly float WaterCrateChance;

		public readonly int SoloCrateMoney;
		public readonly string SilverCrate;		/* solo play crate contents */
		public readonly string WaterCrate;
		public readonly string WoodCrate;

		/* Special Weapons */
		public readonly int ChronoDuration;
		public readonly bool ChronoKillCargo;
		public readonly int ChronoTechLevel;
		public readonly int GPSTechLevel;
		public readonly int GapRadius;
		public readonly float GapRegenInterval;
		public readonly float IronCurtain;		/* minutes */
		public readonly int ParaTech;
		public readonly int ParabombTech;
		public readonly int RadarJamRadius;
		public readonly int SpyPlaneTech;
		public readonly int BadgerBombCount;

		/* Chrono Side Effects */
		public readonly float QuakeChance;
		public readonly float QuakeDamage;		/* percent */
		public readonly float VortexChance;
		public readonly int VortexDamage;
		public readonly int VortexRange;
		public readonly int VortexSpeed;

		/* Repair & Refit */
		public readonly float RefundPercent;
		public readonly float ReloadRate;
		public readonly float RepairPercent;
		public readonly float RepairRate;
		public readonly int RepairStep;
		public readonly float URepairPercent;
		public readonly int URepairStep;

		/* Combat & Damage */
		public readonly float TurboBoost;
		public readonly int APMineDamage;
		public readonly int AVMineDamage;
		public readonly int AtomDamage;
		public readonly float BallisticScatter;
		public readonly int BridgeStrength;
		public readonly float C4Delay;
		public readonly float Crush;
		public readonly float ExpSpread;
		public readonly int FireSupress;
		public readonly float HomingScatter;
		public readonly int MaxDamage;
		public readonly int MinDamage;
		public readonly bool OreExplosive;
		public readonly bool PlayerAutoCrush;
		public readonly bool PlayerReturnFire;
		public readonly bool PlayerScatter;
		public readonly float ProneDamage;
		public readonly bool TreeTargeting;
		public readonly int Incoming;

		/* Income & Production */
		public readonly int BailCount;
		public readonly float BuildSpeed;
		public readonly float BuildupTime;
		public readonly int GemValue;
		public readonly int GoldValue;
		public readonly float GrowthRate;
		public readonly bool OreGrows;
		public readonly bool OreSpreads;
		public readonly float OreTruckRate;
		public readonly bool SeparateAircraft;
		public readonly float SurvivorRate;

		/* Audo/Visual Map Controls */
		public readonly bool AllyReveal;
		public readonly float ConditionRed;
		public readonly float ConditionYellow;
		public readonly int DropZoneRadius;
		public readonly bool EnemyHealth;
		public readonly int Gravity;
		public readonly float IdleActionFrequency;
		public readonly float MessageDelay;
		public readonly float MovieTime;
		public readonly bool NamedCivilians;
		public readonly float SavourDelay;
		public readonly int ShroudRate;
		public readonly int SpeakDelay;
		public readonly int TimerWarning;
		public readonly bool FlashLowPower;

		/* Computer & Movement Controls */
		public readonly bool CurleyShuffle;
		public readonly float BaseBias;
		public readonly float BaseDefenseDelay;
		public readonly float CloseEnough;
		public readonly int DamageDelay;
		public readonly int GameSpeeBias;
		public readonly int LZScanRadius;
		public readonly bool MineAware;
		public readonly float Stray;
		public readonly float SubmergeDelay;
		public readonly float SuspendDelay;
		public readonly int SuspendPriority;
		public readonly float TeamDelay;

		/* Misc */
		public readonly bool FineDiffControl;
		public readonly bool MCVUndeploy;

		/* OpenRA-specific */
		public readonly float OreChance;	/* chance of spreading to a
											 * particular eligible cell */
	}
}
