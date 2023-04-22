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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OpenRA.FileSystem;

namespace OpenRA
{
	[Flags]
	public enum MapVisibility
	{
		Lobby = 1,
		Shellmap = 2,
		MissionSelector = 4
	}

	public interface IMapLoader : IGlobalModData
	{
		Map Load(ModData modData, IReadOnlyPackage package);
		Map Create(ModData modData, ITerrainInfo terrainInfo, int width, int height);
		string ComputeUID(ModData modData, IReadOnlyPackage package);
		void UpdatePreview(ModData modData, MapPreview mapPreview, IReadOnlyPackage p, IReadOnlyPackage parent, MapClassification classification, string[] mapCompatibility, MapGridType gridType);
	}

	public abstract class Map : IReadOnlyFileSystem
	{
		// Standard yaml metadata
		public const short InvalidCachedTerrainIndex = -1;
		public string RequiresMod;
		public string Title;
		public string Author;
		public bool LockPreview;
		public MapVisibility Visibility = MapVisibility.Lobby;
		public string[] Categories = { "Conquest" };
		public string[] Translations;
		public readonly MapGrid Grid;

		// Player and actor yaml. Public for access by the map importers and lint checks.
		public List<MiniYamlNode> PlayerDefinitions = new();
		public List<MiniYamlNode> ActorDefinitions = new();

		// Custom map yaml. Public for access by the map importers and lint checks
		public readonly MiniYaml RuleDefinitions;
		public readonly MiniYaml SequenceDefinitions;
		public readonly MiniYaml ModelSequenceDefinitions;
		public readonly MiniYaml WeaponDefinitions;
		public readonly MiniYaml VoiceDefinitions;
		public readonly MiniYaml MusicDefinitions;
		public readonly MiniYaml NotificationDefinitions;

		public readonly Dictionary<CPos, TerrainTile> ReplacedInvalidTerrainTiles = new();

		public IReadOnlyPackage Package { get; protected set; }
		public string Uid { get; protected set; }
		public bool InvalidCustomRules { get; protected set; }
		public Exception InvalidCustomRulesException { get; protected set; }

		// Internal data
#pragma warning disable IDE1006 // Naming Styles
		protected readonly ModData modData;
		protected Translation Translation;
#pragma warning restore IDE1006 // Naming Styles

		public abstract void Save(IReadWritePackage package);

		public static int GetMapFormat(IReadOnlyPackage p)
		{
			using (var yamlStream = p.GetStream("map.yaml"))
			{
				if (yamlStream == null)
					throw new FileNotFoundException("Required file map.yaml not present in this map");

				foreach (var line in yamlStream.ReadAllLines())
				{
					// PERF This is a way to get MapFormat without expensive yaml parsing
					var search = Regex.Match(line, "^MapFormat:\\s*(\\d*)\\s*$");
					if (search.Success && search.Groups.Count > 0)
						return FieldLoader.GetValue<int>("MapFormat", search.Groups[1].Value);
				}
			}

			throw new InvalidDataException("MapFormat is not defined");
		}

		/// <summary>
		/// Initializes a new map created by the editor or importer.
		/// The map will not receive a valid UID until after it has been saved and reloaded.
		/// </summary>
		protected Map(ModData modData)
		{
			this.modData = modData;
			Grid = modData.Manifest.Get<MapGrid>();

			// Empty rules that can be added to by the importers.
			// Will be dropped on save if nothing is added to it
			RuleDefinitions = new MiniYaml("");
		}

		public Stream Open(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains('|') && Package.Contains(filename))
				return Package.GetStream(filename);

			return modData.DefaultFileSystem.Open(filename);
		}

		public bool TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename)
		{
			// Packages aren't supported inside maps
			return modData.DefaultFileSystem.TryGetPackageContaining(path, out package, out filename);
		}

		public bool TryOpen(string filename, out Stream s)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains('|'))
			{
				s = Package.GetStream(filename);
				if (s != null)
					return true;
			}

			return modData.DefaultFileSystem.TryOpen(filename, out s);
		}

		public bool Exists(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains('|') && Package.Contains(filename))
				return true;

			return modData.DefaultFileSystem.Exists(filename);
		}

		public bool IsExternalModFile(string filename)
		{
			// Explicit package paths never refer to a map
			if (filename.Contains('|'))
				return modData.DefaultFileSystem.IsExternalModFile(filename);

			return false;
		}

		public string Translate(string key, IDictionary<string, object> args = null)
		{
			if (Translation.TryGetString(key, out var message, args))
				return message;

			return modData.Translation.GetString(key, args);
		}
	}
}
