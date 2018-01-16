#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

	public sealed class ModelSequenceFormat : IGlobalModData
	{
		public readonly string Type;
		public readonly IReadOnlyDictionary<string, MiniYaml> Metadata;
		public ModelSequenceFormat(MiniYaml yaml)
		{
			Type = yaml.Value;
			Metadata = new ReadOnlyDictionary<string, MiniYaml>(yaml.ToDictionary());
		}
	}

	public class ModMetadata
	{
		public string Title;
		public string Version;
		public bool Hidden;
	}

	/// <summary> Describes what is to be loaded in order to run a mod. </summary>
	public class Manifest
	{
		public readonly string Id;
		public readonly IReadOnlyPackage Package;
		public readonly ModMetadata Metadata;
		public readonly string[]
			Rules, ServerTraits,
			Sequences, ModelSequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Notifications, Music, Translations, TileSets,
			ChromeMetrics, MapCompatibility, Missions, Hotkeys;

		public readonly IReadOnlyDictionary<string, string> Packages;
		public readonly IReadOnlyDictionary<string, string> MapFolders;
		public readonly MiniYaml LoadScreen;
		public readonly Dictionary<string, Pair<string, int>> Fonts;

		public readonly string[] SoundFormats = { };
		public readonly string[] SpriteFormats = { };
		public readonly string[] PackageFormats = { };

		readonly string[] reservedModuleNames = { "Metadata", "Folders", "MapFolders", "Packages", "Rules",
			"Sequences", "ModelSequences", "Cursors", "Chrome", "Assemblies", "ChromeLayout", "Weapons",
			"Voices", "Notifications", "Music", "Translations", "TileSets", "ChromeMetrics", "Missions", "Hotkeys",
			"ServerTraits", "LoadScreen", "Fonts", "SupportsMapsFrom", "SoundFormats", "SpriteFormats",
			"RequiresMods", "PackageFormats" };

		readonly TypeDictionary modules = new TypeDictionary();
		readonly Dictionary<string, MiniYaml> yaml;

		bool customDataLoaded;

		public Manifest(string modId, IReadOnlyPackage package)
		{
			Id = modId;
			Package = package;
			yaml = new MiniYaml(null, MiniYaml.FromStream(package.GetStream("mod.yaml"), "mod.yaml")).ToDictionary();

			Metadata = FieldLoader.Load<ModMetadata>(yaml["Metadata"]);

			// TODO: Use fieldloader
			MapFolders = YamlDictionary(yaml, "MapFolders");

			MiniYaml packages;
			if (yaml.TryGetValue("Packages", out packages))
				Packages = packages.ToDictionary(x => x.Value).AsReadOnly();

			Rules = YamlList(yaml, "Rules");
			Sequences = YamlList(yaml, "Sequences");
			ModelSequences = YamlList(yaml, "ModelSequences");
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
			Hotkeys = YamlList(yaml, "Hotkeys");

			ServerTraits = YamlList(yaml, "ServerTraits");

			if (!yaml.TryGetValue("LoadScreen", out LoadScreen))
				throw new InvalidDataException("`LoadScreen` section is not defined.");

			Fonts = yaml["Fonts"].ToDictionary(my =>
			{
				var nd = my.ToDictionary();
				return Pair.New(nd["Font"].Value, Exts.ParseIntegerInvariant(nd["Size"].Value));
			});

			// Allow inherited mods to import parent maps.
			var compat = new List<string> { Id };

			if (yaml.ContainsKey("SupportsMapsFrom"))
				compat.AddRange(yaml["SupportsMapsFrom"].Value.Split(',').Select(c => c.Trim()));

			MapCompatibility = compat.ToArray();

			if (yaml.ContainsKey("PackageFormats"))
				PackageFormats = FieldLoader.GetValue<string[]>("PackageFormats", yaml["PackageFormats"].Value);

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

			customDataLoaded = true;
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

		/// <summary>Load a cached IGlobalModData instance.</summary>
		public T Get<T>() where T : IGlobalModData
		{
			if (!customDataLoaded)
				throw new InvalidOperationException("Attempted to call Manifest.Get() before loading custom data!");

			var module = modules.GetOrDefault<T>();

			// Lazily create the default values if not explicitly defined.
			if (module == null)
			{
				module = (T)Game.ModData.ObjectCreator.CreateBasic(typeof(T));
				modules.Add(module);
			}

			return module;
		}

		/// <summary>
		/// Load an uncached IGlobalModData instance directly from the manifest yaml.
		/// This should only be used by external mods that want to query data from this mod.
		/// </summary>
		public T Get<T>(ObjectCreator oc) where T : IGlobalModData
		{
			MiniYaml data;
			var t = typeof(T);
			if (!yaml.TryGetValue(t.Name, out data))
			{
				// Lazily create the default values if not explicitly defined.
				return (T)oc.CreateBasic(t);
			}

			IGlobalModData module;
			var ctor = t.GetConstructor(new[] { typeof(MiniYaml) });
			if (ctor != null)
			{
				// Class has opted-in to DIY initialization
				module = (IGlobalModData)ctor.Invoke(new object[] { data.Value });
			}
			else
			{
				// Automatically load the child nodes using FieldLoader
				module = oc.CreateObject<IGlobalModData>(t.Name);
				FieldLoader.Load(module, data);
			}

			return (T)module;
		}
	}
}
