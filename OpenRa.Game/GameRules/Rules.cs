#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA
{
	public static class Rules
	{
		public static IniFile AllRules;
		public static Dictionary<string, List<string>> Categories = new Dictionary<string, List<string>>();
		public static InfoLoader<WeaponInfo> WeaponInfo;
		public static InfoLoader<WarheadInfo> WarheadInfo;
		public static InfoLoader<ProjectileInfo> ProjectileInfo;
		public static InfoLoader<VoiceInfo> VoiceInfo;
		public static GeneralInfo General;
		public static TechTree TechTree;

		public static Dictionary<string, ActorInfo> Info;

		public static void LoadRules(string map, Manifest m)
		{
			var legacyRules = m.LegacyRules.Reverse().ToList();
			legacyRules.Insert(0, map);
			AllRules = new IniFile(legacyRules.Select(a => FileSystem.Open(a)).ToArray());

			General = new GeneralInfo();
			FieldLoader.Load(General, AllRules.GetSection("General"));

			LoadCategories(
				"Weapon",
				"Warhead",
				"Projectile",
				"Voice");

			WeaponInfo = new InfoLoader<WeaponInfo>(
				Pair.New<string, Func<string, WeaponInfo>>("Weapon", _ => new WeaponInfo()));
			WarheadInfo = new InfoLoader<WarheadInfo>(
				Pair.New<string, Func<string, WarheadInfo>>("Warhead", _ => new WarheadInfo()));
			ProjectileInfo = new InfoLoader<ProjectileInfo>(
				Pair.New<string, Func<string, ProjectileInfo>>("Projectile", _ => new ProjectileInfo()));
			VoiceInfo = new InfoLoader<VoiceInfo>(
				Pair.New<string, Func<string, VoiceInfo>>("Voice", _ => new VoiceInfo()));
	
			var yamlRules = m.Rules.Reverse().Select(a => MiniYaml.FromFile(a)).Aggregate(MiniYaml.Merge);

			ActorInfo.LoadModAssemblies(m);
			Info = new Dictionary<string, ActorInfo>();
			foreach( var kv in yamlRules )
				Info.Add(kv.Key.ToLowerInvariant(), new ActorInfo(kv.Key.ToLowerInvariant(), kv.Value, yamlRules));

			TechTree = new TechTree();
		}

		static void LoadCategories(params string[] types)
		{
			foreach (var t in types)
				Categories[t] = AllRules.GetSection(t + "Types").Select(x => x.Key.ToLowerInvariant()).ToList();
		}
	}
}
