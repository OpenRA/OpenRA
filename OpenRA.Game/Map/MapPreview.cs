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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public enum MapStatus { Available, Unavailable, Searching, DownloadAvailable, Downloading, DownloadError }

	// Used for grouping maps in the UI
	[Flags]
	public enum MapClassification
	{
		Unknown = 0,
		System = 1,
		User = 2,
		Remote = 4
	}

	[SuppressMessage("StyleCop.CSharp.NamingRules",
		"SA1310:FieldNamesMustNotContainUnderscore",
		Justification = "Fields names must match the with the remote API.")]
	[SuppressMessage("Style",
		"IDE1006:Naming Styles",
		Justification = "Fields names must match the with the remote API.")]
	public class RemoteMapData
	{
		public readonly string title;
		public readonly string author;
		public readonly string[] categories;
		public readonly int players;
		public readonly Rectangle bounds;
		public readonly short[] spawnpoints = Array.Empty<short>();
		public readonly MapGridType map_grid_type;
		public readonly string minimap;
		public readonly bool downloading;
		public readonly string tileset;
		public readonly string rules;
		public readonly string players_block;
		public readonly int mapformat;
	}

	public sealed class MapPreview : IDisposable, IReadOnlyFileSystem
	{
		/// <summary>Wrapper that enables map data to be replaced in an atomic fashion.</summary>
		sealed class InnerData
		{
			public int MapFormat;
			public string Title;
			public string[] Categories;
			public string Author;
			public string TileSet;
			public MapPlayers Players;
			public int PlayerCount;
			public CPos[] SpawnPoints;
			public MapGridType GridType;
			public Rectangle Bounds;
			public Png Preview;
			public MapStatus Status;
			public MapClassification Class;
			public MapVisibility Visibility;
			public DateTime ModifiedDate;

			public MiniYaml RuleDefinitions;
			public MiniYaml WeaponDefinitions;
			public MiniYaml VoiceDefinitions;
			public MiniYaml MusicDefinitions;
			public MiniYaml NotificationDefinitions;
			public MiniYaml SequenceDefinitions;
			public MiniYaml ModelSequenceDefinitions;

			public Translation Translation { get; private set; }
			public ActorInfo WorldActorInfo { get; private set; }
			public ActorInfo PlayerActorInfo { get; private set; }

			static MiniYaml LoadRuleSection(Dictionary<string, MiniYaml> yaml, string section)
			{
				if (!yaml.TryGetValue(section, out var node))
					return null;

				return node;
			}

			static bool IsLoadableRuleDefinition(MiniYamlNode n)
			{
				if (n.Key[0] == '^')
					return true;

				var key = n.Key.ToLowerInvariant();
				return key == "world" || key == "player";
			}

			public void SetCustomRules(ModData modData, IReadOnlyFileSystem fileSystem, Dictionary<string, MiniYaml> yaml, IEnumerable<List<MiniYamlNode>> modDataRules)
			{
				RuleDefinitions = LoadRuleSection(yaml, "Rules");
				WeaponDefinitions = LoadRuleSection(yaml, "Weapons");
				VoiceDefinitions = LoadRuleSection(yaml, "Voices");
				MusicDefinitions = LoadRuleSection(yaml, "Music");
				NotificationDefinitions = LoadRuleSection(yaml, "Notifications");
				SequenceDefinitions = LoadRuleSection(yaml, "Sequences");
				ModelSequenceDefinitions = LoadRuleSection(yaml, "ModelSequences");

				Translation = yaml.TryGetValue("Translations", out var node) && node != null
					? new Translation(Game.Settings.Player.Language, FieldLoader.GetValue<string[]>("value", node.Value), fileSystem)
					: null;

				try
				{
					// PERF: Implement a minimal custom loader for custom world and player actors to minimize loading time
					// This assumes/enforces that these actor types can only inherit abstract definitions (starting with ^)
					if (RuleDefinitions != null)
					{
						modDataRules ??= modData.GetRulesYaml();
						var files = Enumerable.Empty<string>();
						if (RuleDefinitions.Value != null)
						{
							var mapFiles = FieldLoader.GetValue<string[]>("value", RuleDefinitions.Value);
							files = files.Append(mapFiles);
						}

						var sources =
							modDataRules.Select(x => x.Where(IsLoadableRuleDefinition).ToList())
							.Concat(files.Select(s => MiniYaml.FromStream(fileSystem.Open(s), s).Where(IsLoadableRuleDefinition).ToList()));
						if (RuleDefinitions.Nodes.Count > 0)
							sources = sources.Append(RuleDefinitions.Nodes.Where(IsLoadableRuleDefinition).ToList());

						var yamlNodes = MiniYaml.Merge(sources);
						WorldActorInfo = new ActorInfo(modData.ObjectCreator, "world", yamlNodes.First(n => string.Equals(n.Key, "world", StringComparison.InvariantCultureIgnoreCase)).Value);
						PlayerActorInfo = new ActorInfo(modData.ObjectCreator, "player", yamlNodes.First(n => string.Equals(n.Key, "player", StringComparison.InvariantCultureIgnoreCase)).Value);
						return;
					}
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Failed to load rules for `{Title}` with error:");
					Log.Write("debug", e);
				}

				WorldActorInfo = modData.DefaultRules.Actors[SystemActors.World];
				PlayerActorInfo = modData.DefaultRules.Actors[SystemActors.Player];
			}

			public InnerData Clone()
			{
				return (InnerData)MemberwiseClone();
			}
		}

		static readonly CPos[] NoSpawns = Array.Empty<CPos>();
		readonly MapCache cache;
		readonly ModData modData;

		public readonly string Uid;
		public IReadOnlyPackage Package { get; private set; }
		IReadOnlyPackage parentPackage;

		volatile InnerData innerData;

		public int MapFormat => innerData.MapFormat;
		public string Title => innerData.Title;
		public string[] Categories => innerData.Categories;
		public string Author => innerData.Author;
		public string TileSet => innerData.TileSet;
		public MapPlayers Players => innerData.Players;
		public int PlayerCount => innerData.PlayerCount;
		public CPos[] SpawnPoints => innerData.SpawnPoints;
		public MapGridType GridType => innerData.GridType;
		public Rectangle Bounds => innerData.Bounds;
		public Png Preview => innerData.Preview;
		public MapStatus Status => innerData.Status;
		public MapClassification Class => innerData.Class;
		public MapVisibility Visibility => innerData.Visibility;

		public MiniYaml RuleDefinitions => innerData.RuleDefinitions;
		public MiniYaml WeaponDefinitions => innerData.WeaponDefinitions;
		public MiniYaml SequenceDefinitions => innerData.SequenceDefinitions;

		public ActorInfo WorldActorInfo => innerData.WorldActorInfo;
		public ActorInfo PlayerActorInfo => innerData.PlayerActorInfo;
		public DateTime ModifiedDate => innerData.ModifiedDate;

		public long DownloadBytes { get; private set; }
		public int DownloadPercentage { get; private set; }

		/// <summary>
		/// Functionality mirrors <see cref="TranslationProvider.GetString"/>, except instead of using
		/// loaded <see cref="Map"/>'s translations as backup, we use this <see cref="MapPreview"/>'s.
		/// </summary>
		public string GetLocalisedString(string key, IDictionary<string, object> args = null)
		{
			// PERF: instead of loading mod level Translation per each MapPreview, reuse the already loaded one in TranslationProvider.
			if (TranslationProvider.TryGetModString(key, out var message, args))
				return message;

			return innerData.Translation?.GetString(key, args) ?? key;
		}

		Sprite minimap;
		bool generatingMinimap;
		public Sprite GetMinimap()
		{
			if (minimap != null)
				return minimap;

			if (!generatingMinimap && Status == MapStatus.Available)
			{
				generatingMinimap = true;
				cache.CacheMinimap(this);
			}

			return null;
		}

		internal void SetMinimap(Sprite minimap)
		{
			this.minimap = minimap;
			generatingMinimap = false;
		}

		public bool DefinesUnsafeCustomRules()
		{
			return Ruleset.DefinesUnsafeCustomRules(modData, this, innerData.RuleDefinitions,
				innerData.WeaponDefinitions, innerData.VoiceDefinitions,
				innerData.NotificationDefinitions, innerData.SequenceDefinitions);
		}

		public Ruleset LoadRuleset()
		{
			return Ruleset.Load(modData, this, TileSet, innerData.RuleDefinitions,
				innerData.WeaponDefinitions, innerData.VoiceDefinitions, innerData.NotificationDefinitions,
				innerData.MusicDefinitions, innerData.ModelSequenceDefinitions);
		}

		public MapPreview(ModData modData, string uid, MapGridType gridType, MapCache cache)
		{
			this.cache = cache;
			this.modData = modData;

			Uid = uid;
			innerData = new InnerData
			{
				MapFormat = 0,
				Title = "Unknown Map",
				Categories = new[] { "Unknown" },
				Author = "Unknown Author",
				TileSet = "unknown",
				Players = null,
				PlayerCount = 0,
				SpawnPoints = NoSpawns,
				GridType = gridType,
				Bounds = Rectangle.Empty,
				Preview = null,
				Status = MapStatus.Unavailable,
				Class = MapClassification.Unknown,
				Visibility = MapVisibility.Lobby,
			};
		}

		// For linting purposes only!
		public MapPreview(Map map, ModData modData)
		{
			this.modData = modData;
			cache = modData.MapCache;

			Uid = map.Uid;
			Package = map.Package;

			var mapPlayers = new MapPlayers(map.PlayerDefinitions);
			var spawns = new List<CPos>();
			foreach (var kv in map.ActorDefinitions.Where(d => d.Value.Value == "mpspawn"))
			{
				var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				spawns.Add(s.Get<LocationInit>().Value);
			}

			innerData = new InnerData
			{
				MapFormat = map.MapFormat,
				Title = map.Title,
				Categories = map.Categories,
				Author = map.Author,
				TileSet = map.Tileset,
				Players = mapPlayers,
				PlayerCount = mapPlayers.Players.Count(x => x.Value.Playable),
				SpawnPoints = spawns.ToArray(),
				GridType = map.Grid.Type,
				Bounds = map.Bounds,
				Preview = null,
				Status = MapStatus.Available,
				Class = MapClassification.Unknown,
				Visibility = map.Visibility,
			};

			innerData.SetCustomRules(modData, this, new Dictionary<string, MiniYaml>()
			{
				{ "Rules", map.RuleDefinitions },
				{ "Translations", map.TranslationDefinitions },
				{ "Weapons", map.WeaponDefinitions },
				{ "Voices", map.VoiceDefinitions },
				{ "Music", map.MusicDefinitions },
				{ "Notifications", map.NotificationDefinitions },
				{ "Sequences", map.SequenceDefinitions },
				{ "ModelSequences", map.ModelSequenceDefinitions }
			}, null);
		}

		public void UpdateFromMap(IReadOnlyPackage p, IReadOnlyPackage parent, MapClassification classification, string[] mapCompatibility, MapGridType gridType, IEnumerable<List<MiniYamlNode>> modDataRules)
		{
			Dictionary<string, MiniYaml> yaml;
			using (var yamlStream = p.GetStream("map.yaml"))
			{
				if (yamlStream == null)
					throw new FileNotFoundException("Required file map.yaml not present in this map");

				yaml = new MiniYaml(null, MiniYaml.FromStream(yamlStream, "map.yaml", stringPool: cache.StringPool)).ToDictionary();
			}

			Package = p;
			parentPackage = parent;

			var newData = innerData.Clone();
			newData.GridType = gridType;
			newData.Class = classification;

			if (yaml.TryGetValue("MapFormat", out var temp))
			{
				var format = FieldLoader.GetValue<int>("MapFormat", temp.Value);
				if (format < Map.SupportedMapFormat)
					throw new InvalidDataException($"Map format {format} is not supported.");
			}

			if (yaml.TryGetValue("Title", out temp))
				newData.Title = temp.Value;

			if (yaml.TryGetValue("Categories", out temp))
				newData.Categories = FieldLoader.GetValue<string[]>("Categories", temp.Value);

			if (yaml.TryGetValue("Tileset", out temp))
				newData.TileSet = temp.Value;

			if (yaml.TryGetValue("Author", out temp))
				newData.Author = temp.Value;

			if (yaml.TryGetValue("Bounds", out temp))
				newData.Bounds = FieldLoader.GetValue<Rectangle>("Bounds", temp.Value);

			if (yaml.TryGetValue("Visibility", out temp))
				newData.Visibility = FieldLoader.GetValue<MapVisibility>("Visibility", temp.Value);

			var requiresMod = string.Empty;
			if (yaml.TryGetValue("RequiresMod", out temp))
				requiresMod = temp.Value;

			if (yaml.TryGetValue("MapFormat", out temp))
				newData.MapFormat = FieldLoader.GetValue<int>("MapFormat", temp.Value);

			newData.Status = mapCompatibility == null || mapCompatibility.Contains(requiresMod) ?
				MapStatus.Available : MapStatus.Unavailable;

			try
			{
				// Actor definitions may change if the map format changes
				if (yaml.TryGetValue("Actors", out var actorDefinitions))
				{
					var spawns = new List<CPos>();
					foreach (var kv in actorDefinitions.Nodes.Where(d => d.Value.Value == "mpspawn"))
					{
						var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						spawns.Add(s.Get<LocationInit>().Value);
					}

					newData.SpawnPoints = spawns.ToArray();
				}
				else
					newData.SpawnPoints = Array.Empty<CPos>();
			}
			catch (Exception)
			{
				newData.SpawnPoints = Array.Empty<CPos>();
				newData.Status = MapStatus.Unavailable;
			}

			try
			{
				// Player definitions may change if the map format changes
				if (yaml.TryGetValue("Players", out var playerDefinitions))
				{
					newData.Players = new MapPlayers(playerDefinitions.Nodes);
					newData.PlayerCount = newData.Players.Players.Count(x => x.Value.Playable);
				}
			}
			catch (Exception)
			{
				newData.Status = MapStatus.Unavailable;
			}

			newData.SetCustomRules(modData, this, yaml, modDataRules);

			if (cache.LoadPreviewImages && p.Contains("map.png"))
				using (var dataStream = p.GetStream("map.png"))
					newData.Preview = new Png(dataStream);

			newData.ModifiedDate = File.GetLastWriteTime(p.Name);

			// Assign the new data atomically
			innerData = newData;
		}

		public void UpdateRemoteSearch(MapStatus status, MiniYaml yaml, Action<MapPreview> parseMetadata = null)
		{
			var newData = innerData.Clone();
			newData.Status = status;
			newData.Class = MapClassification.Remote;

			if (status == MapStatus.DownloadAvailable)
			{
				try
				{
					var r = FieldLoader.Load<RemoteMapData>(yaml);

					// Map download has been disabled server side
					if (!r.downloading)
					{
						newData.Status = MapStatus.Unavailable;
						return;
					}

					newData.Title = r.title;
					newData.Categories = r.categories;
					newData.Author = r.author;
					newData.PlayerCount = r.players;
					newData.Bounds = r.bounds;
					newData.TileSet = r.tileset;
					newData.MapFormat = r.mapformat;

					var spawns = new CPos[r.spawnpoints.Length / 2];
					for (var j = 0; j < r.spawnpoints.Length; j += 2)
						spawns[j / 2] = new CPos(r.spawnpoints[j], r.spawnpoints[j + 1]);
					newData.SpawnPoints = spawns;
					newData.GridType = r.map_grid_type;
					if (cache.LoadPreviewImages)
					{
						try
						{
							newData.Preview = new Png(new MemoryStream(Convert.FromBase64String(r.minimap)));
						}
						catch (Exception e)
						{
							Log.Write("debug", "Failed parsing mapserver minimap response:");
							Log.Write("debug", e);
							newData.Preview = null;
						}
					}

					var playersString = Encoding.UTF8.GetString(Convert.FromBase64String(r.players_block));
					newData.Players = new MapPlayers(MiniYaml.FromString(playersString));

					var rulesString = Encoding.UTF8.GetString(Convert.FromBase64String(r.rules));
					var rulesYaml = new MiniYaml("", MiniYaml.FromString(rulesString)).ToDictionary();
					newData.SetCustomRules(modData, this, rulesYaml, null);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed parsing mapserver response:");
					Log.Write("debug", e);
				}

				// Commit updated data before running the callbacks
				innerData = newData;

				if (innerData.Preview != null)
					cache.CacheMinimap(this);

				parseMetadata?.Invoke(this);
			}

			// Update the status and class unconditionally
			innerData = newData;
		}

		public void Install(string mapRepositoryUrl, Action onSuccess)
		{
			if ((Status != MapStatus.DownloadError && Status != MapStatus.DownloadAvailable) || !Game.Settings.Game.AllowDownloading)
				return;

			innerData.Status = MapStatus.Downloading;
			var installLocation = cache.MapLocations.FirstOrDefault(p => p.Value == MapClassification.User);
			if (installLocation.Key is not IReadWritePackage mapInstallPackage)
			{
				Log.Write("debug", "Map install directory not found");
				innerData.Status = MapStatus.DownloadError;
				return;
			}

			Task.Run(async () =>
			{
				// Request the filename from the server
				// Run in a worker thread to avoid network delays
				var mapUrl = mapRepositoryUrl + Uid;
				try
				{
					void OnDownloadProgress(long total, long received, int percentage)
					{
						DownloadBytes = total;
						DownloadPercentage = percentage;
					}

					var client = HttpClientFactory.Create();

					var response = await client.GetAsync(mapUrl, HttpCompletionOption.ResponseHeadersRead);

					if (!response.IsSuccessStatusCode)
					{
						innerData.Status = MapStatus.DownloadError;
						return;
					}

					var mapFilename = response.Content.Headers.ContentDisposition?.FileName;

					// Map not found
					if (string.IsNullOrEmpty(mapFilename))
					{
						innerData.Status = MapStatus.DownloadError;
						return;
					}

					var fileStream = new MemoryStream();

					await response.ReadAsStreamWithProgress(fileStream, OnDownloadProgress, CancellationToken.None);

					mapInstallPackage.Update(mapFilename, fileStream.ToArray());
					Log.Write("debug", $"Downloaded map to '{mapFilename}'");

					var package = mapInstallPackage.OpenPackage(mapFilename, modData.ModFiles);
					if (package == null)
						innerData.Status = MapStatus.DownloadError;
					else
					{
						UpdateFromMap(package, mapInstallPackage, MapClassification.User, null, GridType, null);
						Game.RunAfterTick(onSuccess);
					}
				}
				catch (Exception e)
				{
					Log.Write("debug", "Map installation failed with error:");
					Log.Write("debug", e);
					innerData.Status = MapStatus.DownloadError;
				}
			});
		}

		public void Invalidate()
		{
			innerData.Status = MapStatus.Unavailable;
		}

		public void Dispose()
		{
			if (Package != null)
			{
				Package.Dispose();
				Package = null;
			}
		}

		public void Delete()
		{
			Invalidate();
			(parentPackage as IReadWritePackage)?.Delete(Package.Name);
		}

		Stream IReadOnlyFileSystem.Open(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains('|') && Package.Contains(filename))
				return Package.GetStream(filename);

			return modData.DefaultFileSystem.Open(filename);
		}

		bool IReadOnlyFileSystem.TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename)
		{
			// Packages aren't supported inside maps
			return modData.DefaultFileSystem.TryGetPackageContaining(path, out package, out filename);
		}

		bool IReadOnlyFileSystem.TryOpen(string filename, out Stream s)
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

		bool IReadOnlyFileSystem.Exists(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains('|') && Package.Contains(filename))
				return true;

			return modData.DefaultFileSystem.Exists(filename);
		}

		bool IReadOnlyFileSystem.IsExternalModFile(string filename)
		{
			// Explicit package paths never refer to a map
			if (filename.Contains('|'))
				return modData.DefaultFileSystem.IsExternalModFile(filename);

			return false;
		}
	}
}
