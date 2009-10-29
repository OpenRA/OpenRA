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
		public static Dictionary<string, List<String>> Categories;
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

			Categories = LoadCategories(
				"BuildingTypes",
				"InfantryTypes",
				"VehicleTypes",
				"ShipTypes",
				"PlaneTypes",
				"WeaponTypes",
				"WarheadTypes",
				"ProjectileTypes" );

			UnitInfo = new InfoLoader<UnitInfo>(
				Pair.New<string,Func<string,UnitInfo>>( "BuildingTypes", s => new UnitInfo.BuildingInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "InfantryTypes", s => new UnitInfo.InfantryInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "VehicleTypes", s => new UnitInfo.VehicleInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "ShipTypes", s => new UnitInfo.VehicleInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "PlaneTypes", s => new UnitInfo.VehicleInfo(s)));

			WeaponInfo = new InfoLoader<WeaponInfo>(
				Pair.New<string,Func<string,WeaponInfo>>("WeaponTypes", _ => new WeaponInfo()));
			WarheadInfo = new InfoLoader<WarheadInfo>(
				Pair.New<string,Func<string,WarheadInfo>>("WarheadTypes", _ => new WarheadInfo()));

			ProjectileInfo = new InfoLoader<ProjectileInfo>(
				Pair.New<string, Func<string, ProjectileInfo>>("ProjectileTypes", _ => new ProjectileInfo()));
		}

		static Dictionary<string, List<string>> LoadCategories( params string[] types )
		{
			var ret = new Dictionary<string, List<string>>();
			foreach( var t in types )
				ret[ t ] = AllRules.GetSection( t ).Select( x => x.Key.ToLowerInvariant() ).ToList();

			return ret;
		}
	}
}
