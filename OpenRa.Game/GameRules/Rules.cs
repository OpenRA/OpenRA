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
		public static InfoLoader<UnitInfo> UnitInfo;
		public static InfoLoader<WeaponInfo> WeaponInfo;
		public static InfoLoader<WarheadInfo> WarheadInfo;
		public static InfoLoader<ProjectileInfo> ProjectileInfo;
		public static Footprint Footprint;

		public static void LoadRules( string mapFileName )
		{
			AllRules = new IniFile(
				FileSystem.Open( mapFileName ),
				FileSystem.Open( "rules.ini" ),
				FileSystem.Open( "units.ini" ),
				FileSystem.Open( "campaignUnits.ini" ) );

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

			Footprint = new Footprint(FileSystem.Open("footprint.txt"));
		}
	}
}
