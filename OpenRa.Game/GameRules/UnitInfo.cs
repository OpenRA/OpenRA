using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.GameRules
{
	public class UnitInfo
	{
		static bool ParseYesNo( string p )
		{
			p = p.ToLowerInvariant();
			if( p == "yes" ) return true;
			if( p == "true" ) return true;
			if( p == "no" ) return false;
			if( p == "false" ) return false;
			throw new InvalidOperationException();
		}

		readonly Dictionary<string, BaseInfo> unitInfos = new Dictionary<string, BaseInfo>();

		public UnitInfo( IniFile rules )
		{
			foreach( var s in Util.ReadAllLines( FileSystem.Open( "buildings.txt" ) ) )
			{
				var unitName = s.Split( ',' )[ 0 ];
				unitInfos.Add( unitName.ToLowerInvariant(), new BuildingInfo( unitName, rules.GetSection( unitName ) ) );
			}
			foreach( var s in Util.ReadAllLines( FileSystem.Open( "infantry.txt" ) ) )
			{
				var unitName = s.Split( ',' )[ 0 ];
				unitInfos.Add( unitName.ToLowerInvariant(), new InfantryInfo( unitName, rules.GetSection( unitName ) ) );
			}
			foreach( var s in Util.ReadAllLines( FileSystem.Open( "vehicles.txt" ) ) )
			{
				var unitName = s.Split( ',' )[ 0 ];
				unitInfos.Add( unitName.ToLowerInvariant(), new VehicleInfo( unitName, rules.GetSection( unitName ) ) );
			}
		}

		public BaseInfo Get( string unitName )
		{
			return unitInfos[ unitName.ToLowerInvariant() ];
		}

		public enum ArmorType
		{
			none = 0,
			wood = 1,
			light = 2,
			heavy = 3,
			concrete = 4,
		}

		public class BaseInfo
		{
			public readonly string Name;

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
			public readonly string Owner = "allies,soviet"; // TODO: make this an enum
			public readonly int Points = 0;
			public readonly string Prerequisite = "";
			public readonly string Primary = null;
			public readonly string Secondary = null;
			public readonly int ROT = 0;
			public readonly int Reload = 0;
			public readonly bool SelfHealing = false;
			public readonly bool Sensors = false; // no idea what this does
			public readonly int Sight = 1;
			public readonly int Strength = 1;
			public readonly int TechLevel = -1;

			public BaseInfo( string name, IniSection ini )
			{
				Name = name.ToLowerInvariant();

				foreach( var x in ini )
				{
					var field = this.GetType().GetField( x.Key );
					if( field.FieldType == typeof( int ) )
						field.SetValue( this, int.Parse( x.Value ) );

					else if( field.FieldType == typeof( float ) )
						field.SetValue( this, float.Parse( x.Value ) );

					else if( field.FieldType == typeof( string ) )
						field.SetValue( this, x.Value.ToLowerInvariant() );

					else if( field.FieldType == typeof( ArmorType ) )
						field.SetValue( this, Enum<ArmorType>.Parse(x.Value) );

					else if( field.FieldType == typeof( bool ) )
						field.SetValue( this, ParseYesNo( x.Value ) );

					else
						do { } while( false );
				}
			}
		}

		public class MobileInfo : BaseInfo
		{
			public readonly int Passengers = 0;
			public readonly int Speed = 0;

			public MobileInfo( string name, IniSection ini )
				: base( name, ini )
			{
			}
		}

		public class InfantryInfo : MobileInfo
		{

			public readonly bool C4 = false;
			public readonly bool FraidyCat = false;
			public readonly bool Infiltrate = false;
			public readonly bool IsCanine = false;

			public InfantryInfo( string name, IniSection ini )
				: base( name, ini )
			{
			}
		}

		public class VehicleInfo : MobileInfo
		{
			public readonly bool Crushable = false;
			public readonly bool Tracked = false;
			public readonly bool NoMovingFire = false;

			public VehicleInfo( string name, IniSection ini )
				: base( name, ini )
			{
			}
		}

		public class BuildingInfo : BaseInfo
		{
			public readonly bool BaseNormal = true;
			public readonly int Adjacent = 1;
			public readonly bool Bib = false;
			public readonly bool Capturable = false;
			public readonly int Power = 0;
			public readonly bool Powered = false;
			public readonly bool Repairable = true;
			public readonly int Storage = 0;
			public readonly bool Unsellable = false;
			public readonly bool WaterBound = false;

			public BuildingInfo( string name, IniSection ini )
				: base( name, ini )
			{
			}
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
