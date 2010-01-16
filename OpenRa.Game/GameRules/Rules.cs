using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Types;
using OpenRa.FileFormats;
using OpenRa.GameRules;

namespace OpenRa
{
	public static class Rules
	{
		public static IniFile AllRules;
		public static Dictionary<string, List<string>> Categories = new Dictionary<string, List<string>>();
		public static InfoLoader<WeaponInfo> WeaponInfo;
		public static InfoLoader<WarheadInfo> WarheadInfo;
		public static InfoLoader<ProjectileInfo> ProjectileInfo;
		public static InfoLoader<VoiceInfo> VoiceInfo;
		public static InfoLoader<SupportPowerInfo> SupportPowerInfo;
		public static GeneralInfo General;
		public static AftermathInfo Aftermath;
		public static TechTree TechTree;
		public static Map Map;
		public static TileSet TileSet;

		public static Dictionary<string, NewUnitInfo> NewUnitInfo;

		public static void LoadRules(string mapFileName, bool useAftermath)
		{
			if (useAftermath)
				AllRules = new IniFile(
					FileSystem.Open(mapFileName),
					FileSystem.Open("aftermathUnits.ini"),
					FileSystem.Open("units.ini"),
					FileSystem.Open("aftrmath.ini"),
					FileSystem.Open("rules.ini"));
			else
				AllRules = new IniFile(
					FileSystem.Open(mapFileName),
					FileSystem.Open("units.ini"),
					FileSystem.Open("rules.ini"));

			General = new GeneralInfo();
			FieldLoader.Load(General, AllRules.GetSection("General"));

			Aftermath = new AftermathInfo();
			if (useAftermath)
				FieldLoader.Load(Aftermath, AllRules.GetSection("Aftermath"));

			LoadCategories(
				"Weapon",
				"Warhead",
				"Projectile",
				"Voice",
				"SupportPower");

			WeaponInfo = new InfoLoader<WeaponInfo>(
				Pair.New<string, Func<string, WeaponInfo>>("Weapon", _ => new WeaponInfo()));
			WarheadInfo = new InfoLoader<WarheadInfo>(
				Pair.New<string, Func<string, WarheadInfo>>("Warhead", _ => new WarheadInfo()));
			ProjectileInfo = new InfoLoader<ProjectileInfo>(
				Pair.New<string, Func<string, ProjectileInfo>>("Projectile", _ => new ProjectileInfo()));
			VoiceInfo = new InfoLoader<VoiceInfo>(
				Pair.New<string, Func<string, VoiceInfo>>("Voice", _ => new VoiceInfo()));
			SupportPowerInfo = new InfoLoader<SupportPowerInfo>(
				Pair.New<string, Func<string, SupportPowerInfo>>("SupportPower", _ => new SupportPowerInfo()));

			var yamlRules = MiniYaml.Merge( MiniYaml.FromFile( "ra.yaml" ), MiniYaml.FromFile( "defaults.yaml" ) );
			if( useAftermath )
				yamlRules = MiniYaml.Merge( MiniYaml.FromFile( "aftermath.yaml" ), yamlRules );

			yamlRules = MiniYaml.Merge( MiniYaml.FromFile( "[mod]Separate buildqueue for defense.yaml" ), yamlRules );

			NewUnitInfo = new Dictionary<string, NewUnitInfo>();
			foreach( var kv in yamlRules )
				NewUnitInfo.Add(kv.Key.ToLowerInvariant(), new NewUnitInfo(kv.Key.ToLowerInvariant(), kv.Value, yamlRules));

			TechTree = new TechTree();
			Map = new Map( AllRules );
			FileSystem.MountTemporary( new Package( Rules.Map.Theater + ".mix" ) );
			TileSet = new TileSet( Map.TileSuffix );
		}

		static void LoadCategories(params string[] types)
		{
			foreach (var t in types)
				Categories[t] = AllRules.GetSection(t + "Types").Select(x => x.Key.ToLowerInvariant()).ToList();
		}
	}
}
