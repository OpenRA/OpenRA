#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
		static readonly List<MiniYamlNode> NoMapRules = new List<MiniYamlNode>();

		readonly ModData modData;

		readonly Dictionary<string, ActorInfo> actorCache = new Dictionary<string, ActorInfo>();
		readonly Dictionary<string, WeaponInfo> weaponCache = new Dictionary<string, WeaponInfo>();
		readonly Dictionary<string, SoundInfo> voiceCache = new Dictionary<string, SoundInfo>();
		readonly Dictionary<string, SoundInfo> notificationCache = new Dictionary<string, SoundInfo>();
		readonly Dictionary<string, MusicInfo> musicCache = new Dictionary<string, MusicInfo>();
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

		/// <summary>
		/// Cache and return the Ruleset for a given map.
		/// If a map isn't specified then return the default mod Ruleset.
		/// </summary>
		public Ruleset Load(Map map = null)
		{
			var m = modData.Manifest;

			Dictionary<string, ActorInfo> actors;
			Dictionary<string, WeaponInfo> weapons;
			Dictionary<string, SoundInfo> voices;
			Dictionary<string, SoundInfo> notifications;
			Dictionary<string, MusicInfo> music;
			Dictionary<string, TileSet> tileSets;

			using (new PerfTimer("Actors"))
				actors = LoadYamlRules(actorCache, m.Rules,
					map != null ? map.RuleDefinitions : NoMapRules,
					k => new ActorInfo(Game.ModData.ObjectCreator, k.Key.ToLowerInvariant(), k.Value));

			using (new PerfTimer("Weapons"))
				weapons = LoadYamlRules(weaponCache, m.Weapons,
					map != null ? map.WeaponDefinitions : NoMapRules,
					k => new WeaponInfo(k.Key.ToLowerInvariant(), k.Value));

			using (new PerfTimer("Voices"))
				voices = LoadYamlRules(voiceCache, m.Voices,
					map != null ? map.VoiceDefinitions : NoMapRules,
					k => new SoundInfo(k.Value));

			using (new PerfTimer("Notifications"))
				notifications = LoadYamlRules(notificationCache, m.Notifications,
					map != null ? map.NotificationDefinitions : NoMapRules,
					k => new SoundInfo(k.Value));

			using (new PerfTimer("Music"))
				music = LoadYamlRules(musicCache, m.Music,
					map != null ? map.MusicDefinitions : NoMapRules,
					k => new MusicInfo(k.Key, k.Value));

			using (new PerfTimer("TileSets"))
				tileSets = LoadTileSets(tileSetCache, sequenceCaches, m.TileSets);

			var sequences = sequenceCaches.ToDictionary(kvp => kvp.Key, kvp => new SequenceProvider(kvp.Value, map));
			return new Ruleset(actors, weapons, voices, notifications, music, tileSets, sequences);
		}

		Dictionary<string, T> LoadYamlRules<T>(
			Dictionary<string, T> itemCache,
			string[] files, List<MiniYamlNode> nodes,
			Func<MiniYamlNode, T> f)
		{
			RaiseProgress();

			var inputKey = string.Concat(string.Join("|", files), "|", nodes.WriteToString());
			Func<MiniYamlNode, T> wrap = wkv =>
			{
				var key = inputKey + wkv.Value.ToLines(wkv.Key).JoinWith("|");
				T t;
				if (itemCache.TryGetValue(key, out t))
					return t;

				t = f(wkv);
				itemCache.Add(key, t);

				RaiseProgress();
				return t;
			};

			var tree = MiniYaml.Merge(files.Select(MiniYaml.FromFile).Append(nodes))
				.ToDictionaryWithConflictLog(n => n.Key, n => n.Value, "LoadYamlRules", null, null);
			RaiseProgress();

			var itemSet = tree.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => wrap(new MiniYamlNode(kv.Key, kv.Value)));
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
