#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		public static Dictionary<string, ActorInfo> Info;
		public static Dictionary<string, WeaponInfo> Weapons;
		public static Dictionary<string, SoundInfo> Voices;
		public static Dictionary<string, SoundInfo> Notifications;
		public static Dictionary<string, MusicInfo> Music;
		public static Dictionary<string, string> Movies;
		public static Dictionary<string, TileSet> TileSets;

		public static void LoadRules(Manifest m, Map map)
		{
			// Added support to extend the list of rules (add it to m.LocalRules)
			Info = LoadYamlRules(m.Rules, map.Rules, (k, y) => new ActorInfo(k.Key.ToLowerInvariant(), k.Value, y));
			Weapons = LoadYamlRules(m.Weapons, map.Weapons, (k, _) => new WeaponInfo(k.Key.ToLowerInvariant(), k.Value));
			Voices = LoadYamlRules(m.Voices, map.Voices, (k, _) => new SoundInfo(k.Value));
			Notifications = LoadYamlRules(m.Notifications, map.Notifications, (k, _) => new SoundInfo(k.Value));
			Music = LoadYamlRules(m.Music, new List<MiniYamlNode>(), (k, _) => new MusicInfo(k.Key, k.Value));
			Movies = LoadYamlRules(m.Movies, new List<MiniYamlNode>(), (k, v) => k.Value.Value);

			TileSets = new Dictionary<string, TileSet>();
			foreach (var file in m.TileSets)
			{
				var t = new TileSet(file);
				TileSets.Add(t.Id,t);
			}
		}

		static Dictionary<string, T> LoadYamlRules<T>(string[] files, List<MiniYamlNode> dict, Func<MiniYamlNode, Dictionary<string, MiniYaml>, T> f)
		{
			var y = files.Select(MiniYaml.FromFile).Aggregate(dict, MiniYaml.MergeLiberal);
			var yy = y.ToDictionary(x => x.Key, x => x.Value);
			return y.ToDictionaryWithConflictLog(kv => kv.Key.ToLowerInvariant(), kv => f(kv, yy), "LoadYamlRules", null, null);
		}

		public static IEnumerable<KeyValuePair<string, MusicInfo>> InstalledMusic { get { return Music.Where(m => m.Value.Exists); } }
	}
}
