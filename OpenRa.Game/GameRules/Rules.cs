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
		public static InfoLoader<UnitInfo> UnitInfo;
		public static InfoLoader<WeaponInfo> WeaponInfo;
		public static InfoLoader<WarheadInfo> WarheadInfo;
		public static InfoLoader<ProjectileInfo> ProjectileInfo;
		public static Footprint Footprint;

		// TODO: load rules from the map, where appropriate.
		public static void LoadRules()
		{
			var rulesIni = new IniFile(FileSystem.Open("rules.ini"));

			UnitInfo = new InfoLoader<UnitInfo>(rulesIni, 
				Pair.New<string,Func<string,UnitInfo>>( "buildings.txt", s => new UnitInfo.BuildingInfo(s)),
				Pair.New<string, Func<string,UnitInfo>>("infantry.txt", s => new UnitInfo.InfantryInfo(s)),
				Pair.New<string,Func<string,UnitInfo>>( "vehicles.txt", s => new UnitInfo.VehicleInfo(s)));

			WeaponInfo = new InfoLoader<WeaponInfo>(rulesIni, 
				Pair.New<string,Func<string,WeaponInfo>>("weapons.txt", _ => new WeaponInfo()));
			WarheadInfo = new InfoLoader<WarheadInfo>(rulesIni, 
				Pair.New<string,Func<string,WarheadInfo>>("warheads.txt", _ => new WarheadInfo()));

			ProjectileInfo = new InfoLoader<ProjectileInfo>(rulesIni,
				Pair.New<string, Func<string, ProjectileInfo>>("projectiles.txt", _ => new ProjectileInfo()));

			Footprint = new Footprint(FileSystem.Open("footprint.txt"));
		}
	}
}
