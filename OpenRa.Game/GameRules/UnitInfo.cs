
using System;
namespace OpenRa.Game.GameRules
{
	public enum ArmorType
	{
		none = 0,
		wood = 1,
		light = 2,
		heavy = 3,
		concrete = 4,
	}

	public class UnitInfo : ActorInfo
	{
		public readonly string Name;

		public readonly string Description = "";
		public readonly string[] Traits;

		public readonly int Ammo = -1;
		public readonly ArmorType Armor = ArmorType.none;
		[Obsolete] public readonly bool DoubleOwned = false;
		[Obsolete] public readonly bool Cloakable = false;
		public readonly int Cost = 0;
		public readonly bool Crewed = false;
		public readonly bool Explodes = false;
		public readonly int GuardRange = -1; // -1 = use weapon's range
		public readonly string Image = null; // sprite-set to use when rendering
		public readonly bool Invisible = false;
		public readonly Race[] Owner = { Race.Allies, Race.Soviet };
		public readonly int Points = 0;
		public readonly string[] Prerequisite = { };
		public readonly string Primary = null;
		public readonly string Secondary = null;
		public readonly int ROT = 255;
		public readonly int Reload = 0;
		public readonly bool SelfHealing = false;
		[Obsolete] public readonly bool Sensors = false; // no idea what this does
		public readonly int Sight = 1;
		public readonly int Strength = 1;
		public readonly int TechLevel = -1;
		public readonly bool WaterBound = false;
		public readonly string[] BuiltAt = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly int[] RotorOffset = { 0, 0 };
		public readonly int[] RotorOffset2 = null;
		public readonly int Recoil = 0;
		public readonly bool MuzzleFlash = false;
		public readonly int SelectionPriority = 10;
		public readonly int InitialFacing = 128;
		public readonly bool Selectable = true;
		public readonly int FireDelay = 0;
		public readonly string LongDesc = null;
		public readonly int OrePips = 0;
		public readonly string Icon = null;
		public readonly int[] SelectionSize = null;
		public readonly int Passengers = 0;
		public readonly int UnloadFacing = 0;
		public readonly UnitMovementType[] PassengerTypes = null;

		// weapon origins and firing angles within the turrets. 3 values per position.
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };

		public UnitInfo(string name) { Name = name; }
	}

	public class MobileInfo : UnitInfo
	{
		public readonly int Speed = 0;
		public readonly bool NoMovingFire = false;
		public readonly string Voice = "GenericVoice";

		public MobileInfo(string name) : base(name) { }
	}

	public class InfantryInfo : MobileInfo
	{
		public readonly bool C4 = false;
		public readonly bool FraidyCat = false;
		public readonly bool Infiltrate = false;
		public readonly bool IsCanine = false;
		public readonly int SquadSize = 1;

		public InfantryInfo(string name) : base(name) { }
	}

	public class VehicleInfo : MobileInfo
	{
		public readonly bool Tracked = false;

		public VehicleInfo(string name) : base(name) { }
	}

	public class BuildingInfo : UnitInfo
	{
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly string Footprint = "x";
		public readonly string[] Produces = { };

		public readonly bool BaseNormal = true;
		public readonly int Adjacent = 1;
		public readonly bool Bib = false;
		public readonly bool Capturable = false;
		public readonly int Power = 0;
		public readonly bool Powered = false;
		public readonly bool Repairable = true;
		public readonly int Storage = 0;
		public readonly bool Unsellable = false;
		public readonly int[] RallyPoint = { 1, 3 };
		public readonly float[] SpawnOffset = null;

		public BuildingInfo(string name) : base(name) { }
	}
}
