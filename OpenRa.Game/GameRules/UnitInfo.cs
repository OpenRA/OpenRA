using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

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

	public class UnitInfo
	{
		public readonly string Name;

		public readonly string Description = "";
		public readonly string[] Traits;

		public readonly int Ammo = -1;
		public readonly ArmorType Armor = ArmorType.none;
		public readonly bool DoubleOwned = false;
		public readonly bool Cloakable = false;
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
		public readonly int ROT = 0;
		public readonly int Reload = 0;
		public readonly bool SelfHealing = false;
		public readonly bool Sensors = false; // no idea what this does
		public readonly int Sight = 1;
		public readonly int Strength = 1;
		public readonly int TechLevel = -1;
		public readonly bool WaterBound = false;
		public readonly string[] BuiltAt = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly int Recoil = 0;

		public UnitInfo(string name) { Name = name; }

		public class MobileInfo : UnitInfo
		{
			public readonly int Passengers = 0;
			public readonly int Speed = 0;

			public MobileInfo(string name) : base(name) { }
		}

		public class InfantryInfo : MobileInfo
		{
			public readonly bool C4 = false;
			public readonly bool FraidyCat = false;
			public readonly bool Infiltrate = false;
			public readonly bool IsCanine = false;

			public InfantryInfo(string name) : base(name) { }
		}

		public class VehicleInfo : MobileInfo
		{
			public readonly bool Crushable = false;
			public readonly bool Tracked = false;
			public readonly bool NoMovingFire = false;

			public VehicleInfo(string name) : base(name) { }
		}

		public class BuildingInfo : UnitInfo
		{
			public readonly int2 Dimensions = new int2( 1, 1 );
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

			public BuildingInfo(string name) : base(name) { }
		}

		/*
		 * Example: HARV
		 *		Unit (can move, etc)
		 *		PlayerOwned
		 *		Selectable
		 *		CanHarvest
		 *		
		 * Example: PROC (refinery)
		 *		Building (can't move)
		 *		AcceptsOre (harvester returns here)
		 *		
		 * Example: 3TNK (soviet heavy tank)
		 *		Unit
		 *		Turret (can aim in different direction to movement)
		 * 
		 * Example: GUN (allied base defense turret)
		 *		Building
		 *		Turret
		 * 
		 * some traits can be determined by fields in rules.ini
		 * and some can't :
		 *		Gap-generator's ability
		 *		Nuke, chrone, curtain, (super-weapons)
		 *		Aircraft-landable
		 *		Selectable (bomber/spyplane can't be selected, for example)
		 *		AppearsFriendly (spy)
		 *		IsInfantry (can be build in TENT/BARR, 5-in-a-square)
		 *		IsVehicle
		 *		Squashable (sandbags, infantry)
		 *		Special rendering for war factory
		 */
	}
}
