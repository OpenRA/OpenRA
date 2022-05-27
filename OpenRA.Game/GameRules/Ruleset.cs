#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	public class Ruleset
	{
		public readonly ActorInfoDictionary Actors;
		public readonly IReadOnlyDictionary<string, WeaponInfo> Weapons;
		public readonly IReadOnlyDictionary<string, SoundInfo> Voices;
		public readonly IReadOnlyDictionary<string, SoundInfo> Notifications;
		public readonly IReadOnlyDictionary<string, MusicInfo> Music;
		public readonly ITerrainInfo TerrainInfo;
		public readonly SequenceProvider Sequences;
		public readonly IReadOnlyDictionary<string, MiniYamlNode> ModelSequences;

		public Ruleset(
			IReadOnlyDictionary<string, ActorInfo> actors,
			IReadOnlyDictionary<string, WeaponInfo> weapons,
			IReadOnlyDictionary<string, SoundInfo> voices,
			IReadOnlyDictionary<string, SoundInfo> notifications,
			IReadOnlyDictionary<string, MusicInfo> music,
			ITerrainInfo terrainInfo,
			SequenceProvider sequences,
			IReadOnlyDictionary<string, MiniYamlNode> modelSequences)
		{
			Actors = new ActorInfoDictionary(actors);
			Weapons = weapons;
			Voices = voices;
			Notifications = notifications;
			Music = music;
			TerrainInfo = terrainInfo;
			Sequences = sequences;
			ModelSequences = modelSequences;

			foreach (var a in Actors.Values)
			{
				foreach (var t in a.TraitInfos<IRulesetLoaded>())
				{
					try
					{
						t.RulesetLoaded(this, a);
					}
					catch (YamlException e)
					{
						throw new YamlException($"Actor type {a.Name}: {e.Message}");
					}
				}
			}

			foreach (var weapon in Weapons)
			{
				if (weapon.Value.Projectile is IRulesetLoaded<WeaponInfo> projectileLoaded)
				{
					try
					{
						projectileLoaded.RulesetLoaded(this, weapon.Value);
					}
					catch (YamlException e)
					{
						throw new YamlException($"Projectile type {weapon.Key}: {e.Message}");
					}
				}

				foreach (var warhead in weapon.Value.Warheads)
				{
					if (warhead is IRulesetLoaded<WeaponInfo> cacher)
					{
						try
						{
							cacher.RulesetLoaded(this, weapon.Value);
						}
						catch (YamlException e)
						{
							throw new YamlException($"Weapon type {weapon.Key}: {e.Message}");
						}
					}
				}
			}
		}

		public IEnumerable<KeyValuePair<string, MusicInfo>> InstalledMusic { get { return Music.Where(m => m.Value.Exists); } }

		static IReadOnlyDictionary<string, T> MergeOrDefault<T>(string name,
			IReadOnlyFileSystem fileSystem,
			IEnumerable<string> files,
			MiniYaml additional,
			IReadOnlyDictionary<string, T> defaults,
			Func<MiniYamlNode, T> makeObject,
			Func<MiniYamlNode, bool> filterNode = null)
		{
			if (additional == null && defaults != null)
				return defaults;

			IEnumerable<MiniYamlNode> yamlNodes = MiniYaml.Load(fileSystem, files, additional);

			// Optionally, the caller can filter out elements from the loaded set of nodes. Default behavior is unfiltered.
			if (filterNode != null)
				yamlNodes = yamlNodes.Where(k => !filterNode(k));

			return yamlNodes.ToDictionaryWithConflictLog(k => k.Key.ToLowerInvariant(), makeObject, "LoadFromManifest<" + name + ">");
		}

		public static Ruleset LoadDefaults(ModData modData)
		{
			var m = modData.Manifest;
			var fs = modData.DefaultFileSystem;

			Ruleset ruleset = null;
			Action f = () =>
			{
				var actors = MergeOrDefault("Manifest,Rules", fs, m.Rules, null, null,
					k => new ActorInfo(modData.ObjectCreator, k.Key.ToLowerInvariant(), k.Value),
					filterNode: n => n.Key.StartsWith(ActorInfo.AbstractActorPrefix, StringComparison.Ordinal));

				var weapons = MergeOrDefault("Manifest,Weapons", fs, m.Weapons, null, null,
					k => new WeaponInfo(k.Value));

				var voices = MergeOrDefault("Manifest,Voices", fs, m.Voices, null, null,
					k => new SoundInfo(k.Value));

				var notifications = MergeOrDefault("Manifest,Notifications", fs, m.Notifications, null, null,
					k => new SoundInfo(k.Value));

				var music = MergeOrDefault("Manifest,Music", fs, m.Music, null, null,
					k => new MusicInfo(k.Key, k.Value));

				var modelSequences = MergeOrDefault("Manifest,ModelSequences", fs, m.ModelSequences, null, null,
					k => k);

				// The default ruleset does not include a preferred tileset or sequence set
				ruleset = new Ruleset(actors, weapons, voices, notifications, music, null, null, modelSequences);
			};

			if (modData.IsOnMainThread)
			{
				modData.HandleLoadingProgress();

				var loader = new Task(f);
				loader.Start();

				// Animate the loadscreen while we wait
				while (!loader.Wait(40))
					modData.HandleLoadingProgress();
			}
			else
				f();

			return ruleset;
		}

		public static Ruleset LoadDefaultsForTileSet(ModData modData, string tileSet)
		{
			var dr = modData.DefaultRules;
			var terrainInfo = modData.DefaultTerrainInfo[tileSet];
			var sequences = modData.DefaultSequences[tileSet];

			return new Ruleset(dr.Actors, dr.Weapons, dr.Voices, dr.Notifications, dr.Music, terrainInfo, sequences, dr.ModelSequences);
		}

		public static Ruleset Load(ModData modData, IReadOnlyFileSystem fileSystem, string tileSet,
			MiniYaml mapRules, MiniYaml mapWeapons, MiniYaml mapVoices, MiniYaml mapNotifications,
			MiniYaml mapMusic, MiniYaml mapSequences, MiniYaml mapModelSequences)
		{
			var m = modData.Manifest;
			var dr = modData.DefaultRules;

			Ruleset ruleset = null;
			Action f = () =>
			{
				var actors = MergeOrDefault("Rules", fileSystem, m.Rules, mapRules, dr.Actors,
					k => new ActorInfo(modData.ObjectCreator, k.Key.ToLowerInvariant(), k.Value),
					filterNode: n => n.Key.StartsWith(ActorInfo.AbstractActorPrefix, StringComparison.Ordinal));

				var weapons = MergeOrDefault("Weapons", fileSystem, m.Weapons, mapWeapons, dr.Weapons,
					k => new WeaponInfo(k.Value));

				var voices = MergeOrDefault("Voices", fileSystem, m.Voices, mapVoices, dr.Voices,
					k => new SoundInfo(k.Value));

				var notifications = MergeOrDefault("Notifications", fileSystem, m.Notifications, mapNotifications, dr.Notifications,
					k => new SoundInfo(k.Value));

				var music = MergeOrDefault("Music", fileSystem, m.Music, mapMusic, dr.Music,
					k => new MusicInfo(k.Key, k.Value));

				// TODO: Add support for merging custom terrain modifications
				var terrainInfo = modData.DefaultTerrainInfo[tileSet];

				// TODO: Top-level dictionary should be moved into the Ruleset instead of in its own object
				var sequences = mapSequences == null ? modData.DefaultSequences[tileSet] :
					new SequenceProvider(fileSystem, modData, tileSet, mapSequences);

				var modelSequences = dr.ModelSequences;
				if (mapModelSequences != null)
					modelSequences = MergeOrDefault("ModelSequences", fileSystem, m.ModelSequences, mapModelSequences, dr.ModelSequences,
						k => k);

				ruleset = new Ruleset(actors, weapons, voices, notifications, music, terrainInfo, sequences, modelSequences);
			};

			if (modData.IsOnMainThread)
			{
				modData.HandleLoadingProgress();

				var loader = new Task(f);
				loader.Start();

				// Animate the loadscreen while we wait
				while (!loader.Wait(40))
					modData.HandleLoadingProgress();
			}
			else
				f();

			return ruleset;
		}

		static bool AnyCustomYaml(MiniYaml yaml)
		{
			return yaml != null && (yaml.Value != null || yaml.Nodes.Count > 0);
		}

		static bool AnyFlaggedTraits(ModData modData, List<MiniYamlNode> actors)
		{
			foreach (var actorNode in actors)
			{
				foreach (var traitNode in actorNode.Value.Nodes)
				{
					try
					{
						var traitName = traitNode.Key.Split('@')[0];
						var traitType = modData.ObjectCreator.FindType(traitName + "Info");
						if (traitType != null && traitType.GetInterface(nameof(ILobbyCustomRulesIgnore)) == null)
							return true;
					}
					catch (Exception ex)
					{
						Log.Write("debug", "Error in AnyFlaggedTraits\r\n" + ex.ToString());
					}
				}
			}

			return false;
		}

		public static bool DefinesUnsafeCustomRules(ModData modData, IReadOnlyFileSystem fileSystem,
			MiniYaml mapRules, MiniYaml mapWeapons, MiniYaml mapVoices, MiniYaml mapNotifications, MiniYaml mapSequences)
		{
			// Maps that define any weapon, voice, notification, or sequence overrides are always flagged
			if (AnyCustomYaml(mapWeapons) || AnyCustomYaml(mapVoices) || AnyCustomYaml(mapNotifications) || AnyCustomYaml(mapSequences))
				return true;

			// Any trait overrides that aren't explicitly whitelisted are flagged
			if (mapRules != null)
			{
				if (AnyFlaggedTraits(modData, mapRules.Nodes))
					return true;

				if (mapRules.Value != null)
				{
					var mapFiles = FieldLoader.GetValue<string[]>("value", mapRules.Value);
					foreach (var f in mapFiles)
						if (AnyFlaggedTraits(modData, MiniYaml.FromStream(fileSystem.Open(f), f)))
							return true;
				}
			}

			return false;
		}
	}
}
