using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using IjwFramework.Types;

namespace OpenRa.Game
{
	static class Rules
	{
		public static IniFile AllRules;
		public static Dictionary<string, List<string>> Categories = new Dictionary<string,List<string>>();
		public static Dictionary<string, string> UnitCategory;
		public static InfoLoader<UnitInfo> UnitInfo;
		public static InfoLoader<WeaponInfo> WeaponInfo;
		public static InfoLoader<WarheadInfo> WarheadInfo;
		public static InfoLoader<ProjectileInfo> ProjectileInfo;

		public static void LoadRules( string mapFileName )
		{
			AllRules = new IniFile(
				FileSystem.Open( mapFileName ),
				FileSystem.Open( "rules.ini" ),
				FileSystem.Open( "units.ini" ),
				FileSystem.Open( "campaignUnits.ini" ) );

			LoadCategories(
				"Building",
				"Infantry",
				"Vehicle",
				"Ship",
				"Plane" );
			UnitCategory = Categories.SelectMany( x => x.Value.Select( y => new KeyValuePair<string, string>( y, x.Key ) ) ).ToDictionary( x => x.Key, x => x.Value );

			UnitInfo = new InfoLoader<UnitInfo>(
				Pair.New<string,Func<string,UnitInfo>>( "Building", s => new UnitInfo.BuildingInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "Infantry", s => new UnitInfo.InfantryInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "Vehicle", s => new UnitInfo.VehicleInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "Ship", s => new UnitInfo.VehicleInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "Plane", s => new UnitInfo.VehicleInfo(s)));

			LoadCategories(
				"Weapon",
				"Warhead",
				"Projectile" );

			WeaponInfo = new InfoLoader<WeaponInfo>(
				Pair.New<string,Func<string,WeaponInfo>>("Weapon", _ => new WeaponInfo()));
			WarheadInfo = new InfoLoader<WarheadInfo>(
				Pair.New<string,Func<string,WarheadInfo>>("Warhead", _ => new WarheadInfo()));

			ProjectileInfo = new InfoLoader<ProjectileInfo>(
				Pair.New<string, Func<string, ProjectileInfo>>("Projectile", _ => new ProjectileInfo()));
		}

		static void LoadCategories( params string[] types )
		{
			foreach( var t in types )
				Categories[ t ] = AllRules.GetSection( t + "Types" ).Select( x => x.Key.ToLowerInvariant() ).ToList();
		}
	}
}
