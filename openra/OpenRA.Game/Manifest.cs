#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA
{
	public class ModMetadata
	{
		// FieldLoader used here, must matching naming in YAML.
#pragma warning disable IDE1006 // Naming Styles
		[FluentReference]
		public readonly string Title;
		public readonly string Version;
		public readonly string Website;
		public readonly string WebIcon32;

		[FluentReference(optional: true)]
		public readonly string WindowTitle;
		public readonly bool Hidden;
#pragma warning restore IDE1006 // Naming Styles

		public string TitleTranslated => FluentProvider.GetMessage(Title);
		public string WindowTitleTranslated => WindowTitle != null ? FluentProvider.GetMessage(WindowTitle) : null;
	}

	public class RendererConstants
	{
		public readonly int FontSheetSize = 512;
		public readonly int CursorSheetSize = 512;
		public readonly int MapPreviewSheetSize = 2048;
		public readonly int SequenceBgraSheetSize = 2048;
		public readonly int SequenceIndexedSheetSize = 2048;
		public readonly int VertexBatchSize = 8192;
	}

	/// <summary>Describes what is to be loaded in order to run a mod.</summary>
	public sealed class Manifest
	{
		public readonly string Id;
		public readonly IReadOnlyPackage Package;
		public readonly ModMetadata Metadata;
		public readonly ImmutableArray<string>
			Rules, ServerTraits,
			Sequences, ModelSequences, Cursors, Chrome, ChromeLayout,
			Weapons, Voices, Notifications, Music, FluentMessages, TileSets,
			ChromeMetrics, MapCompatibility, Missions, Hotkeys;

		public readonly FrozenDictionary<string, string> MapFolders;
		public readonly MiniYaml FileSystem;
		public readonly MiniYaml LoadScreen;
		public readonly string DefaultOrderGenerator;
		public readonly RendererConstants RendererConstants;

		public readonly ImmutableArray<string> Assemblies = [];
		public readonly ImmutableArray<string> SoundFormats = [];
		public readonly ImmutableArray<string> SpriteFormats = [];
		public readonly ImmutableArray<string> PackageFormats = [];
		public readonly ImmutableArray<string> VideoFormats = [];
		public readonly string SpriteSequenceFormat;
		public readonly string TerrainFormat;

		// TODO: This should be controlled by a user-selected translation bundle!
		public readonly string FluentCulture = "en";
		public readonly bool AllowUnusedFluentMessagesInExternalPackages = true;

		static readonly FrozenSet<string> ReservedModuleNames = new HashSet<string>
		{
			"Include", "Metadata", "FileSystem", "MapFolders", "Rules",
			"Sequences", "ModelSequences", "Cursors", "Chrome", "Assemblies", "ChromeLayout", "Weapons",
			"Voices", "Notifications", "Music", "FluentMessages", "TileSets", "ChromeMetrics", "Missions", "Hotkeys",
			"ServerTraits", "LoadScreen", "DefaultOrderGenerator", "SupportsMapsFrom", "SoundFormats", "SpriteFormats", "VideoFormats",
			"SpriteSequenceFormat", "TerrainFormat", "RequiresMods", "PackageFormats", "AllowUnusedFluentMessagesInExternalPackages", "RendererConstants"
		}.ToFrozenSet();

		public readonly FrozenDictionary<string, MiniYaml> GlobalModData;

		public Manifest(string modId, IReadOnlyPackage package)
		{
			Id = modId;
			Package = package;

			var stringPool = new HashSet<string>(); // Reuse common strings in YAML
			var nodes = MiniYaml.FromStream(package.GetStream("mod.yaml"), $"{package.Name}:mod.yaml", stringPool: stringPool).ToList();
			for (var i = nodes.Count - 1; i >= 0; i--)
			{
				if (nodes[i].Key != "Include")
					continue;

				// Replace `Includes: filename.yaml` with the contents of filename.yaml
				var filename = nodes[i].Value.Value;
				var contents = package.GetStream(filename);
				if (contents == null)
					throw new YamlException($"{nodes[i].Location}: File `{filename}` not found.");

				nodes.RemoveAt(i);
				nodes.InsertRange(i, MiniYaml.FromStream(contents, $"{package.Name}:{filename}", stringPool: stringPool));
			}

			// Merge inherited overrides
			var yaml = new MiniYaml(null, MiniYaml.Merge([nodes])).ToDictionary();

			Metadata = FieldLoader.Load<ModMetadata>(yaml["Metadata"]);

			// TODO: Use fieldloader
			MapFolders = YamlDictionary(yaml, "MapFolders");

			if (!yaml.TryGetValue("FileSystem", out FileSystem))
				throw new InvalidDataException("`FileSystem` section is not defined.");

			Rules = YamlList(yaml, "Rules");
			Sequences = YamlList(yaml, "Sequences");
			ModelSequences = YamlList(yaml, "ModelSequences");
			Cursors = YamlList(yaml, "Cursors");
			Chrome = YamlList(yaml, "Chrome");
			ChromeLayout = YamlList(yaml, "ChromeLayout");
			Weapons = YamlList(yaml, "Weapons");
			Voices = YamlList(yaml, "Voices");
			Notifications = YamlList(yaml, "Notifications");
			Music = YamlList(yaml, "Music");
			FluentMessages = YamlList(yaml, "FluentMessages");
			TileSets = YamlList(yaml, "TileSets");
			ChromeMetrics = YamlList(yaml, "ChromeMetrics");
			Missions = YamlList(yaml, "Missions");
			Hotkeys = YamlList(yaml, "Hotkeys");

			ServerTraits = YamlList(yaml, "ServerTraits");

			if (!yaml.TryGetValue("LoadScreen", out LoadScreen))
				throw new InvalidDataException("`LoadScreen` section is not defined.");

			// Allow inherited mods to import parent maps.
			var compat = new List<string> { Id };

			if (yaml.TryGetValue("SupportsMapsFrom", out var entry))
				compat.AddRange(entry.Value.Split(',').Select(c => c.Trim()));

			MapCompatibility = compat.ToImmutableArray();

			if (yaml.TryGetValue("DefaultOrderGenerator", out entry))
				DefaultOrderGenerator = entry.Value;

			if (yaml.TryGetValue("Assemblies", out entry))
				Assemblies = FieldLoader.GetValue<ImmutableArray<string>>("Assemblies", entry.Value);

			if (yaml.TryGetValue("PackageFormats", out entry))
				PackageFormats = FieldLoader.GetValue<ImmutableArray<string>>("PackageFormats", entry.Value);

			if (yaml.TryGetValue("SoundFormats", out entry))
				SoundFormats = FieldLoader.GetValue<ImmutableArray<string>>("SoundFormats", entry.Value);

			if (yaml.TryGetValue("SpriteFormats", out entry))
				SpriteFormats = FieldLoader.GetValue<ImmutableArray<string>>("SpriteFormats", entry.Value);

			if (yaml.TryGetValue("VideoFormats", out entry))
				VideoFormats = FieldLoader.GetValue<ImmutableArray<string>>("VideoFormats", entry.Value);

			if (yaml.TryGetValue("SpriteSequenceFormat", out entry))
				SpriteSequenceFormat = entry.Value;

			if (yaml.TryGetValue("TerrainFormat", out entry))
				TerrainFormat = entry.Value;

			if (yaml.TryGetValue("AllowUnusedFluentMessagesInExternalPackages", out entry))
				AllowUnusedFluentMessagesInExternalPackages =
					FieldLoader.GetValue<bool>("AllowUnusedFluentMessagesInExternalPackages", entry.Value);

			if (yaml.TryGetValue("RendererConstants", out entry))
				RendererConstants = FieldLoader.Load<RendererConstants>(entry);
			else
				RendererConstants = new RendererConstants();

			GlobalModData = yaml.Where(n => !ReservedModuleNames.Contains(n.Key))
				.ToFrozenDictionary(n => n.Key, n => n.Value);
		}

		static ImmutableArray<string> YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.TryGetValue(key, out var value))
				return [];

			return value.Nodes.Select(n => n.Key).ToImmutableArray();
		}

		static FrozenDictionary<string, string> YamlDictionary(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.TryGetValue(key, out var value))
				return FrozenDictionary<string, string>.Empty;

			return value.ToDictionary(my => my.Value).ToFrozenDictionary();
		}
	}
}
