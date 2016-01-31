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
using System.IO;
using System.Linq;
using OpenRA.Markup;
using OpenRA.Primitives;

namespace OpenRA
{
	public interface IGlobalModData { }

	public sealed class SpriteSequenceFormat : IGlobalModData
	{
		public readonly string Type;
		public readonly IReadOnlyDictionary<string, MiniYaml> Metadata;
		public SpriteSequenceFormat(MiniYaml yaml)
		{
			Type = yaml.Value;
			Metadata = new ReadOnlyDictionary<string, MiniYaml>(yaml.ToDictionary());
		}
	}

	/// <summary> Describes what is to be loaded in order to run a mod. </summary>
	public class Manifest
	{
		public static readonly Dictionary<string, Manifest> AllMods = LoadMods();

		public readonly ModMetadata Mod;
		public readonly string[]
			Packages, Folders, Rules, ServerTraits,
			Sequences, VoxelSequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Notifications, Music, Translations, TileSets,
			ChromeMetrics, MapCompatibility, Missions;

		public readonly IReadOnlyDictionary<string, string> MapFolders;
		public readonly MiniYaml LoadScreen;
		public readonly MiniYaml LobbyDefaults;

		public readonly Dictionary<string, string> RequiresMods;
		public readonly Dictionary<string, Pair<string, int>> Fonts;

		public readonly string[] SoundFormats = { };
		public readonly string[] SpriteFormats = { };

		readonly string[] reservedModuleNames = { "Metadata", "Folders", "MapFolders", "Packages", "Rules",
			"Sequences", "VoxelSequences", "Cursors", "Chrome", "Assemblies", "ChromeLayout", "Weapons",
			"Voices", "Notifications", "Music", "Translations", "TileSets", "ChromeMetrics", "Missions",
			"ServerTraits", "LoadScreen", "LobbyDefaults", "Fonts", "SupportsMapsFrom", "SoundFormats", "SpriteFormats",
			"RequiresMods" };

		readonly TypeDictionary modules = new TypeDictionary();
		readonly Dictionary<string, MiniYaml> yaml;

		public Manifest(string modId, string modPath = null)
		{
			if (modPath == null)
				modPath = ModMetadata.CandidateModPaths[modId];

			var path = Path.Combine(modPath, "mod.yaml");
			yaml = new MiniYaml(null, MiniYaml.FromFile(path)).ToDictionary();

			Mod = FieldLoader.Load<ModMetadata>(yaml["Metadata"]);
			Mod.Id = modId;

			// TODO: Use fieldloader
			Folders = YamlList(yaml, "Folders", true);
			MapFolders = YamlDictionary(yaml, "MapFolders", true);
			Packages = YamlList(yaml, "Packages", true);
			Rules = YamlList(yaml, "Rules", true);
			Sequences = YamlList(yaml, "Sequences", true);
			VoxelSequences = YamlList(yaml, "VoxelSequences", true);
			Cursors = YamlList(yaml, "Cursors", true);
			Chrome = YamlList(yaml, "Chrome", true);
			Assemblies = YamlList(yaml, "Assemblies", true);
			ChromeLayout = YamlList(yaml, "ChromeLayout", true);
			Weapons = YamlList(yaml, "Weapons", true);
			Voices = YamlList(yaml, "Voices", true);
			Notifications = YamlList(yaml, "Notifications", true);
			Music = YamlList(yaml, "Music", true);
			Translations = YamlList(yaml, "Translations", true);
			TileSets = YamlList(yaml, "TileSets", true);
			ChromeMetrics = YamlList(yaml, "ChromeMetrics", true);
			Missions = YamlList(yaml, "Missions", true);

			ServerTraits = YamlList(yaml, "ServerTraits");

			if (!yaml.TryGetValue("LoadScreen", out LoadScreen))
				throw new InvalidDataException("`LoadScreen` section is not defined.");

			if (!yaml.TryGetValue("LobbyDefaults", out LobbyDefaults))
				throw new InvalidDataException("`LobbyDefaults` section is not defined.");

			Fonts = yaml["Fonts"].ToDictionary(my =>
				{
					var nd = my.ToDictionary();
					return Pair.New(nd["Font"].Value, Exts.ParseIntegerInvariant(nd["Size"].Value));
				});

			RequiresMods = yaml["RequiresMods"].ToDictionary(my => my.Value);

			// Allow inherited mods to import parent maps.
			var compat = new List<string> { Mod.Id };

			if (yaml.ContainsKey("SupportsMapsFrom"))
				compat.AddRange(yaml["SupportsMapsFrom"].Value.Split(',').Select(c => c.Trim()));

			MapCompatibility = compat.ToArray();

			if (yaml.ContainsKey("SoundFormats"))
				SoundFormats = FieldLoader.GetValue<string[]>("SoundFormats", yaml["SoundFormats"].Value);

			if (yaml.ContainsKey("SpriteFormats"))
				SpriteFormats = FieldLoader.GetValue<string[]>("SpriteFormats", yaml["SpriteFormats"].Value);
		}

		public void LoadCustomData(ObjectCreator oc)
		{
			foreach (var kv in yaml)
			{
				if (reservedModuleNames.Contains(kv.Key))
					continue;

				var t = oc.FindType(kv.Key);
				if (t == null || !typeof(IGlobalModData).IsAssignableFrom(t))
					throw new InvalidDataException("`{0}` is not a valid mod manifest entry.".F(kv.Key));

				IGlobalModData module;
				var ctor = t.GetConstructor(new[] { typeof(MiniYaml) });
				if (ctor != null)
				{
					// Class has opted-in to DIY initialization
					module = (IGlobalModData)ctor.Invoke(new object[] { kv.Value });
				}
				else
				{
					// Automatically load the child nodes using FieldLoader
					module = oc.CreateObject<IGlobalModData>(kv.Key);
					FieldLoader.Load(module, kv.Value);
				}

				modules.Add(module);
			}
		}

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key, bool parsePaths = false)
		{
			if (!yaml.ContainsKey(key))
				return new string[] { };

			if (parsePaths)
				return yaml[key].Nodes.Select(node => Platform.ResolvePath(node.Key, node.Value.Value ?? string.Empty)).ToArray();

			return yaml[key].ToDictionary().Keys.ToArray();
		}

		static IReadOnlyDictionary<string, string> YamlDictionary(Dictionary<string, MiniYaml> yaml, string key, bool parsePaths = false)
		{
			if (!yaml.ContainsKey(key))
				return new ReadOnlyDictionary<string, string>();

			var inner = new Dictionary<string, string>();
			foreach (var node in yaml[key].Nodes)
			{
				var line = node.Key;
				if (node.Value.Value != null)
					line += ":" + node.Value.Value;

				// '@' may be used in mod.yaml to indicate extra information (similar to trait @ tags).
				// Applies to MapFolders (to indicate System and User directories) and Packages (to indicate package annotation).
				if (line.Contains('@'))
				{
					var split = line.Split('@');
					inner.Add(parsePaths ? Platform.ResolvePath(split[0]) : split[0], split[1]);
				}
				else
					inner.Add(line, null);
			}

			return new ReadOnlyDictionary<string, string>(inner);
		}

		public T Get<T>() where T : IGlobalModData
		{
			var module = modules.GetOrDefault<T>();

			// Lazily create the default values if not explicitly defined.
			if (module == null)
			{
				module = (T)Game.ModData.ObjectCreator.CreateBasic(typeof(T));
				modules.Add(module);
			}

			return module;
		}

		static Dictionary<string, Manifest> LoadMods()
		{
			var ret = new Dictionary<string, Manifest>();
			foreach (var mod in ModMetadata.CandidateModPaths)
			{
				if (!File.Exists(Path.Combine(mod.Value, "mod.yaml")))
					continue;

				try
				{
					var manifest = new Manifest(mod.Key, mod.Value);
					ret.Add(mod.Key, manifest);
				}
				catch (Exception ex)
				{
					Log.Write("debug", "An exception occurred while trying to load mod {0}:", mod);
					Log.Write("debug", ex.ToString());
				}
			}

			return ret;
		}
	}
}
