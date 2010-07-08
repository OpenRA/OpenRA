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
		public static TechTree TechTree;

		public static Dictionary<string, ActorInfo> Info;
		public static Dictionary<string, WeaponInfo> Weapons;
		public static Dictionary<string, VoiceInfo> Voices;
		public static Dictionary<string, MusicInfo> Music;
		public static Dictionary<string, TileSet> TileSets;

		public static void LoadRules(Manifest m, Map map)
		{
			Info = LoadYamlRules(m.Rules, map.Rules, (k, y) => new ActorInfo(k.Key.ToLowerInvariant(), k.Value, y));
			Weapons = LoadYamlRules(m.Weapons, map.Weapons, (k, _) => new WeaponInfo(k.Key.ToLowerInvariant(), k.Value));
			Voices = LoadYamlRules(m.Voices, map.Voices, (k, _) => new VoiceInfo(k.Value));
			Music = LoadYamlRules(m.Music, map.Music, (k, _) => new MusicInfo(k.Value));
			
			TileSets = new Dictionary<string, TileSet>();
			foreach (var file in m.TileSets)
			{
				var t = new TileSet(file);
				TileSets.Add(t.Id,t);
			}
			
			TechTree = new TechTree();
		}
		
		static Dictionary<string, T> LoadYamlRules<T>(string[] files, Dictionary<string,MiniYaml>dict, Func<KeyValuePair<string, MiniYaml>, Dictionary<string, MiniYaml>, T> f)
		{
			var y = files.Select(a => MiniYaml.FromFile(a)).Aggregate(dict,MiniYaml.Merge);
			return y.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => f(kv, y));
		}

		public static IEnumerable<string> Categories()
		{
			return Info.Values.Select( x => x.Category ).Distinct().Where( g => g != null ).ToList();
		}
	}
}
