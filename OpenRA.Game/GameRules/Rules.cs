#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Support;

namespace OpenRA
{
	public static class Rules
	{
		public static Dictionary<string, ActorInfo> Info { get { return Game.modData.Rules.Actors; } }
		public static Dictionary<string, WeaponInfo> Weapons { get { return Game.modData.Rules.Weapons; } }
		public static Dictionary<string, SoundInfo> Voices { get { return Game.modData.Rules.Voices; } }
		public static Dictionary<string, SoundInfo> Notifications { get { return Game.modData.Rules.Notifications; } }
		public static Dictionary<string, MusicInfo> Music { get { return Game.modData.Rules.Music; } }
		public static Dictionary<string, string> Movies { get { return Game.modData.Rules.Movies; } }
		public static Dictionary<string, TileSet> TileSets { get { return Game.modData.Rules.TileSets; } }

		public static void LoadRules(Manifest m, Map map)
		{
			// HACK: Fallback for code that hasn't been updated yet
			Game.modData.Rules = new ModRules(Game.modData);
			Game.modData.Rules.ActivateMap(map);
		}

		public static IEnumerable<KeyValuePair<string, MusicInfo>> InstalledMusic { get { return Music.Where(m => m.Value.Exists); } }
	}


	public class ModRules
	{
		readonly ModData modData;

		//
		// These contain all unique instances created from each mod/map combination
		//
		readonly Dictionary<string, ActorInfo> actorCache = new Dictionary<string, ActorInfo>();
		readonly Dictionary<string, WeaponInfo> weaponCache = new Dictionary<string, WeaponInfo>();
		readonly Dictionary<string, SoundInfo> voiceCache = new Dictionary<string, SoundInfo>();
		readonly Dictionary<string, SoundInfo> notificationCache = new Dictionary<string, SoundInfo>();
		readonly Dictionary<string, MusicInfo> musicCache = new Dictionary<string, MusicInfo>();
		readonly Dictionary<string, string> movieCache = new Dictionary<string, string>();
		readonly Dictionary<string, TileSet> tileSetCache = new Dictionary<string, TileSet>();

		//
		// These are the instances needed for the current map
		//
		public Dictionary<string, ActorInfo> Actors { get; private set; }
		public Dictionary<string, WeaponInfo> Weapons { get; private set; }
		public Dictionary<string, SoundInfo> Voices { get; private set; }
		public Dictionary<string, SoundInfo> Notifications { get; private set; }
		public Dictionary<string, MusicInfo> Music { get; private set; }
		public Dictionary<string, string> Movies { get; private set; }
		public Dictionary<string, TileSet> TileSets { get; private set; }


		public ModRules(ModData modData)
		{
			this.modData = modData;
		}

		public void ActivateMap(Map map)
		{
			var m = modData.Manifest;
			using (new PerfTimer("Actors"))
				Actors = LoadYamlRules(actorCache, m.Rules, map.Rules, (k, y) => new ActorInfo(k.Key.ToLowerInvariant(), k.Value, y));
			using (new PerfTimer("Weapons"))
				Weapons = LoadYamlRules(weaponCache, m.Weapons, map.Weapons, (k, _) => new WeaponInfo(k.Key.ToLowerInvariant(), k.Value));
			using (new PerfTimer("Voices"))
				Voices = LoadYamlRules(voiceCache, m.Voices, map.Voices, (k, _) => new SoundInfo(k.Value));
			using (new PerfTimer("Notifications"))
				Notifications = LoadYamlRules(notificationCache, m.Notifications, map.Notifications, (k, _) => new SoundInfo(k.Value));
			using (new PerfTimer("Music"))
				Music = LoadYamlRules(musicCache, m.Music, new List<MiniYamlNode>(), (k, _) => new MusicInfo(k.Key, k.Value));
			using (new PerfTimer("Movies"))
				Movies = LoadYamlRules(movieCache, m.Movies, new List<MiniYamlNode>(), (k, v) => k.Value.Value);
			using (new PerfTimer("TileSets"))
				TileSets = LoadTileSets(tileSetCache, m.TileSets);
		}

		Dictionary<string, T> LoadYamlRules<T>(
			Dictionary<string, T> itemCache,
			string[] files, List<MiniYamlNode> nodes,
			Func<MiniYamlNode, Dictionary<string, MiniYaml>, T> f)
		{
			string inputKey = string.Concat(string.Join("|", files), "|", nodes.WriteToString());

			var mergedNodes = files
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(nodes, MiniYaml.MergeLiberal);

			Func<MiniYamlNode, Dictionary<string, MiniYaml>, T> wrap = (wkv, wyy) =>
			{
				var key = inputKey + wkv.Value.ToLines(wkv.Key).JoinWith("|");
				T t;
				if (itemCache.TryGetValue(key, out t))
					return t;

				t = f(wkv, wyy);
				itemCache.Add(key, t);
				return t;
			};

			var yy = mergedNodes.ToDictionary(x => x.Key, x => x.Value);
			var itemSet = mergedNodes.ToDictionaryWithConflictLog(kv => kv.Key.ToLowerInvariant(), kv => wrap(kv, yy), "LoadYamlRules", null, null);

			return itemSet;
		}

		Dictionary<string, TileSet> LoadTileSets(Dictionary<string, TileSet> itemCache, string[] files)
		{
			var items = new Dictionary<string, TileSet>();

			foreach (var file in files)
			{
				TileSet t;
				if (itemCache.TryGetValue(file, out t))
				{
					items.Add(t.Id, t);
				}
				else
				{
					t = new TileSet(file);
					itemCache.Add(file, t);

					items.Add(t.Id, t);
				}
			}

			return items;
		}
	}
}
