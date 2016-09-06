#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
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
		public readonly ModMetadata Mod;
		public readonly string[]
			Rules, ServerTraits,
			Sequences, VoxelSequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Notifications, Music, Translations, TileSets,
			ChromeMetrics, MapCompatibility, Missions;

		public readonly IReadOnlyDictionary<string, string> Packages;
		public readonly IReadOnlyDictionary<string, string> MapFolders;
		public readonly MiniYaml LoadScreen;

		public readonly Dictionary<string, string> RequiresMods;
		public readonly Dictionary<string, Pair<string, int>> Fonts;

		public readonly string[] SoundFormats = { };
		public readonly string[] SpriteFormats = { };

		readonly string[] reservedModuleNames = { "Metadata", "Folders", "MapFolders", "Packages", "Rules",
			"Sequences", "VoxelSequences", "Cursors", "Chrome", "Assemblies", "ChromeLayout", "Weapons",
			"Voices", "Notifications", "Music", "Translations", "TileSets", "ChromeMetrics", "Missions",
			"ServerTraits", "LoadScreen", "Fonts", "SupportsMapsFrom", "SoundFormats", "SpriteFormats",
			"RequiresMods" };

		readonly TypeDictionary modules = new TypeDictionary();
		readonly Dictionary<string, MiniYaml> yaml;

		public Manifest(string modId)
		{
			var package = ModMetadata.AllMods[modId].Package;

			yaml = new MiniYaml(null, MiniYaml.FromStream(package.GetStream("mod.yaml"), "mod.yaml")).ToDictionary();

			Mod = FieldLoader.Load<ModMetadata>(yaml["Metadata"]);
			Mod.Id = modId;

			// TODO: Use fieldloader
			MapFolders = YamlDictionary(yaml, "MapFolders");

			MiniYaml packages;
			if (yaml.TryGetValue("Packages", out packages))
				Packages = packages.ToDictionary(x => x.Value).AsReadOnly();

			Rules = YamlList(yaml, "Rules");
			Sequences = YamlList(yaml, "Sequences");
			VoxelSequences = YamlList(yaml, "VoxelSequences");
			Cursors = YamlList(yaml, "Cursors");
			Chrome = YamlList(yaml, "Chrome");
			Assemblies = YamlList(yaml, "Assemblies");
			ChromeLayout = YamlList(yaml, "ChromeLayout");
			Weapons = YamlList(yaml, "Weapons");
			Voices = YamlList(yaml, "Voices");
			Notifications = YamlList(yaml, "Notifications");
			Music = YamlList(yaml, "Music");
			Translations = YamlList(yaml, "Translations");
			TileSets = YamlList(yaml, "TileSets");
			ChromeMetrics = YamlList(yaml, "ChromeMetrics");
			Missions = YamlList(yaml, "Missions");

			ServerTraits = YamlList(yaml, "ServerTraits");

			if (!yaml.TryGetValue("LoadScreen", out LoadScreen))
				throw new InvalidDataException("`LoadScreen` section is not defined.");

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

			return yaml[key].ToDictionary().Keys.ToArray();
		}

		static IReadOnlyDictionary<string, string> YamlDictionary(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new ReadOnlyDictionary<string, string>();

			var inner = yaml[key].ToDictionary(my => my.Value);
			return new ReadOnlyDictionary<string, string>(inner);
		}

		public bool Contains<T>() where T : IGlobalModData
		{
			return modules.Contains<T>();
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
	}
}
