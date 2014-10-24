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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class RulesetCache : IDisposable
	{
		readonly ModData modData;

		readonly Dictionary<string, ActorInfo> actorCache = new Dictionary<string, ActorInfo>();
		readonly Dictionary<string, WeaponInfo> weaponCache = new Dictionary<string, WeaponInfo>();
		readonly Dictionary<string, SoundInfo> voiceCache = new Dictionary<string, SoundInfo>();
		readonly Dictionary<string, SoundInfo> notificationCache = new Dictionary<string, SoundInfo>();
		readonly Dictionary<string, MusicInfo> musicCache = new Dictionary<string, MusicInfo>();
		readonly Dictionary<string, string> movieCache = new Dictionary<string, string>();
		readonly Dictionary<string, TileSet> tileSetCache = new Dictionary<string, TileSet>();
		readonly Dictionary<string, SequenceCache> sequenceCaches = new Dictionary<string, SequenceCache>();

		public event EventHandler LoadingProgress;
		void RaiseProgress()
		{
			if (LoadingProgress != null)
				LoadingProgress(this, new EventArgs());
		}

		public RulesetCache(ModData modData)
		{
			this.modData = modData;
		}

		public Ruleset LoadDefaultRules()
		{
			return LoadMapRules(new Map());
		}

		public Ruleset LoadMapRules(Map map)
		{
			var m = modData.Manifest;

			Dictionary<string, ActorInfo> actors;
			Dictionary<string, WeaponInfo> weapons;
			Dictionary<string, SoundInfo> voices;
			Dictionary<string, SoundInfo> notifications;
			Dictionary<string, MusicInfo> music;
			Dictionary<string, string> movies;
			Dictionary<string, TileSet> tileSets;

			using (new PerfTimer("Actors"))
				actors = LoadYamlRules(actorCache, m.Rules, map.RuleDefinitions, (k, y) => new ActorInfo(k.Key.ToLowerInvariant(), k.Value, y));
			using (new PerfTimer("Weapons"))
				weapons = LoadYamlRules(weaponCache, m.Weapons, map.WeaponDefinitions, (k, _) => new WeaponInfo(k.Key.ToLowerInvariant(), k.Value));
			using (new PerfTimer("Voices"))
				voices = LoadYamlRules(voiceCache, m.Voices, map.VoiceDefinitions, (k, _) => new SoundInfo(k.Value));
			using (new PerfTimer("Notifications"))
				notifications = LoadYamlRules(notificationCache, m.Notifications, map.NotificationDefinitions, (k, _) => new SoundInfo(k.Value));
			using (new PerfTimer("Music"))
				music = LoadYamlRules(musicCache, m.Music, new List<MiniYamlNode>(), (k, _) => new MusicInfo(k.Key, k.Value));
			using (new PerfTimer("Movies"))
				movies = LoadYamlRules(movieCache, m.Movies, new List<MiniYamlNode>(), (k, v) => k.Value.Value);
			using (new PerfTimer("TileSets"))
				tileSets = LoadTileSets(tileSetCache, sequenceCaches, m.TileSets);

			var sequences = sequenceCaches.ToDictionary(kvp => kvp.Key, kvp => new SequenceProvider(kvp.Value, map));

			return new Ruleset(actors, weapons, voices, notifications, music, movies, tileSets, sequences);
		}

		Dictionary<string, T> LoadYamlRules<T>(
			Dictionary<string, T> itemCache,
			string[] files, List<MiniYamlNode> nodes,
			Func<MiniYamlNode, Dictionary<string, MiniYaml>, T> f)
		{
			RaiseProgress();

			var inputKey = string.Concat(string.Join("|", files), "|", nodes.WriteToString());

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

				RaiseProgress();
				return t;
			};

			var yy = mergedNodes.ToDictionary(x => x.Key, x => x.Value);
			var itemSet = mergedNodes.ToDictionaryWithConflictLog(kv => kv.Key.ToLowerInvariant(), kv => wrap(kv, yy), "LoadYamlRules", null, null);

			RaiseProgress();
			return itemSet;
		}

		Dictionary<string, TileSet> LoadTileSets(Dictionary<string, TileSet> itemCache, Dictionary<string, SequenceCache> sequenceCaches, string[] files)
		{
			var items = new Dictionary<string, TileSet>();

			foreach (var file in files)
			{
				TileSet t;
				if (itemCache.TryGetValue(file, out t))
					items.Add(t.Id, t);
				else
				{
					t = new TileSet(modData, file);
					itemCache.Add(file, t);

					// every time we load a tile set, we create a sequence cache for it
					var sc = new SequenceCache(modData, t);
					sequenceCaches.Add(t.Id, sc);

					items.Add(t.Id, t);
				}
			}

			return items;
		}

		public void Dispose()
		{
			foreach (var cache in sequenceCaches.Values)
				cache.Dispose();
			sequenceCaches.Clear();
		}
	}
}
