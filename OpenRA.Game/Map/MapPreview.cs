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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public enum MapStatus { Available, Unavailable, Searching, DownloadAvailable, Downloading, DownloadError }

	// Used for grouping maps in the UI
	public enum MapClassification { Unknown, System, User, Remote }

	// Used for verifying map availability in the lobby
	public enum MapRuleStatus { Unknown, Cached, Invalid }

	[SuppressMessage("StyleCop.CSharp.NamingRules",
		"SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter",
		Justification = "Fields names must match the with the remote API.")]
	[SuppressMessage("StyleCop.CSharp.NamingRules",
		"SA1304:NonPrivateReadonlyFieldsMustBeginWithUpperCaseLetter",
		Justification = "Fields names must match the with the remote API.")]
	[SuppressMessage("StyleCop.CSharp.NamingRules",
		"SA1310:FieldNamesMustNotContainUnderscore",
		Justification = "Fields names must match the with the remote API.")]
	public class RemoteMapData
	{
		public readonly string title;
		public readonly string author;
		public readonly string[] categories;
		public readonly int players;
		public readonly Rectangle bounds;
		public readonly int[] spawnpoints = { };
		public readonly MapGridType map_grid_type;
		public readonly string minimap;
		public readonly bool downloading;
		public readonly string tileset;
		public readonly string rules;
		public readonly string players_block;
	}

	public class MapPreview : IDisposable, IReadOnlyFileSystem
	{
		/// <summary>Wrapper that enables map data to be replaced in an atomic fashion</summary>
		class InnerData
		{
			public string Title;
			public string[] Categories;
			public string Author;
			public string TileSet;
			public MapPlayers Players;
			public int PlayerCount;
			public CPos[] SpawnPoints;
			public MapGridType GridType;
			public Rectangle Bounds;
			public Bitmap Preview;
			public MapStatus Status;
			public MapClassification Class;
			public MapVisibility Visibility;

			Lazy<Ruleset> rules;
			public Ruleset Rules { get { return rules != null ? rules.Value : null; } }
			public bool InvalidCustomRules { get; private set; }
			public bool DefinesUnsafeCustomRules { get; private set; }
			public bool RulesLoaded { get; private set; }

			public void SetRulesetGenerator(ModData modData, Func<Pair<Ruleset, bool>> generator)
			{
				InvalidCustomRules = false;
				RulesLoaded = false;
				DefinesUnsafeCustomRules = false;

				// Note: multiple threads may try to access the value at the same time
				// We rely on the thread-safety guarantees given by Lazy<T> to prevent race conitions.
				// If you're thinking about replacing this, then you must be careful to keep this safe.
				rules = Exts.Lazy(() =>
				{
					if (generator == null)
						return Ruleset.LoadDefaultsForTileSet(modData, TileSet);

					try
					{
						var ret = generator();
						DefinesUnsafeCustomRules = ret.Second;
						return ret.First;
					}
					catch (Exception e)
					{
						Log.Write("debug", "Failed to load rules for `{0}` with error :{1}", Title, e.Message);
						InvalidCustomRules = true;
						return Ruleset.LoadDefaultsForTileSet(modData, TileSet);
					}
					finally
					{
						RulesLoaded = true;
					}
				});
			}

			public InnerData Clone()
			{
				return (InnerData)MemberwiseClone();
			}
		}

		static readonly CPos[] NoSpawns = new CPos[] { };
		MapCache cache;
		ModData modData;

		public readonly string Uid;
		public IReadOnlyPackage Package { get; private set; }
		IReadOnlyPackage parentPackage;

		volatile InnerData innerData;

		public string Title { get { return innerData.Title; } }
		public string[] Categories { get { return innerData.Categories; } }
		public string Author { get { return innerData.Author; } }
		public string TileSet { get { return innerData.TileSet; } }
		public MapPlayers Players { get { return innerData.Players; } }
		public int PlayerCount { get { return innerData.PlayerCount; } }
		public CPos[] SpawnPoints { get { return innerData.SpawnPoints; } }
		public MapGridType GridType { get { return innerData.GridType; } }
		public Rectangle Bounds { get { return innerData.Bounds; } }
		public Bitmap Preview { get { return innerData.Preview; } }
		public MapStatus Status { get { return innerData.Status; } }
		public MapClassification Class { get { return innerData.Class; } }
		public MapVisibility Visibility { get { return innerData.Visibility; } }

		public Ruleset Rules { get { return innerData.Rules; } }
		public bool InvalidCustomRules { get { return innerData.InvalidCustomRules; } }
		public bool RulesLoaded { get { return innerData.RulesLoaded; } }
		public bool DefinesUnsafeCustomRules
		{
			get
			{
				// Force lazy rules to be evaluated
				var force = innerData.Rules;
				return innerData.DefinesUnsafeCustomRules;
			}
		}

		Download download;
		public long DownloadBytes { get; private set; }
		public int DownloadPercentage { get; private set; }

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

		public MapPreview(ModData modData, string uid, MapGridType gridType, MapCache cache)
		{
			this.cache = cache;
			this.modData = modData;

			Uid = uid;
			innerData = new InnerData
			{
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

		public void UpdateFromMap(IReadOnlyPackage p, IReadOnlyPackage parent, MapClassification classification, string[] mapCompatibility, MapGridType gridType)
		{
			Dictionary<string, MiniYaml> yaml;
			using (var yamlStream = p.GetStream("map.yaml"))
			{
				if (yamlStream == null)
					throw new FileNotFoundException("Required file map.yaml not present in this map");

				yaml = new MiniYaml(null, MiniYaml.FromStream(yamlStream, "map.yaml")).ToDictionary();
			}

			Package = p;
			parentPackage = parent;

			var newData = innerData.Clone();
			newData.GridType = gridType;
			newData.Class = classification;

			MiniYaml temp;
			if (yaml.TryGetValue("MapFormat", out temp))
			{
				var format = FieldLoader.GetValue<int>("MapFormat", temp.Value);
				if (format != Map.SupportedMapFormat)
					throw new InvalidDataException("Map format {0} is not supported.".F(format));
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

			string requiresMod = string.Empty;
			if (yaml.TryGetValue("RequiresMod", out temp))
				requiresMod = temp.Value;

			newData.Status = mapCompatibility == null || mapCompatibility.Contains(requiresMod) ?
				MapStatus.Available : MapStatus.Unavailable;

			try
			{
				// Actor definitions may change if the map format changes
				MiniYaml actorDefinitions;
				if (yaml.TryGetValue("Actors", out actorDefinitions))
				{
					var spawns = new List<CPos>();
					foreach (var kv in actorDefinitions.Nodes.Where(d => d.Value.Value == "mpspawn"))
					{
						var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						spawns.Add(s.InitDict.Get<LocationInit>().Value(null));
					}

					newData.SpawnPoints = spawns.ToArray();
				}
				else
					newData.SpawnPoints = new CPos[0];
			}
			catch (Exception)
			{
				newData.SpawnPoints = new CPos[0];
				newData.Status = MapStatus.Unavailable;
			}

			try
			{
				// Player definitions may change if the map format changes
				MiniYaml playerDefinitions;
				if (yaml.TryGetValue("Players", out playerDefinitions))
				{
					newData.Players = new MapPlayers(playerDefinitions.Nodes);
					newData.PlayerCount = newData.Players.Players.Count(x => x.Value.Playable);
				}
			}
			catch (Exception)
			{
				newData.Status = MapStatus.Unavailable;
			}

			newData.SetRulesetGenerator(modData, () =>
			{
				var ruleDefinitions = LoadRuleSection(yaml, "Rules");
				var weaponDefinitions = LoadRuleSection(yaml, "Weapons");
				var voiceDefinitions = LoadRuleSection(yaml, "Voices");
				var musicDefinitions = LoadRuleSection(yaml, "Music");
				var notificationDefinitions = LoadRuleSection(yaml, "Notifications");
				var sequenceDefinitions = LoadRuleSection(yaml, "Sequences");
				var rules = Ruleset.Load(modData, this, TileSet, ruleDefinitions, weaponDefinitions,
					voiceDefinitions, notificationDefinitions, musicDefinitions, sequenceDefinitions);
				var flagged = Ruleset.DefinesUnsafeCustomRules(modData, this, ruleDefinitions,
					weaponDefinitions, voiceDefinitions, notificationDefinitions, sequenceDefinitions);
				return Pair.New(rules, flagged);
			});

			if (p.Contains("map.png"))
				using (var dataStream = p.GetStream("map.png"))
					newData.Preview = new Bitmap(dataStream);

			// Assign the new data atomically
			innerData = newData;
		}

		MiniYaml LoadRuleSection(Dictionary<string, MiniYaml> yaml, string section)
		{
			MiniYaml node;
			if (!yaml.TryGetValue(section, out node))
				return null;

			return node;
		}

		public void PreloadRules()
		{
			var unused = Rules;
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

					var spawns = new CPos[r.spawnpoints.Length / 2];
					for (var j = 0; j < r.spawnpoints.Length; j += 2)
						spawns[j / 2] = new CPos(r.spawnpoints[j], r.spawnpoints[j + 1]);
					newData.SpawnPoints = spawns;
					newData.GridType = r.map_grid_type;
					newData.Preview = new Bitmap(new MemoryStream(Convert.FromBase64String(r.minimap)));

					var playersString = Encoding.UTF8.GetString(Convert.FromBase64String(r.players_block));
					newData.Players = new MapPlayers(MiniYaml.FromString(playersString));

					newData.SetRulesetGenerator(modData, () =>
					{
						var rulesString = Encoding.UTF8.GetString(Convert.FromBase64String(r.rules));
						var rulesYaml = new MiniYaml("", MiniYaml.FromString(rulesString)).ToDictionary();
						var ruleDefinitions = LoadRuleSection(rulesYaml, "Rules");
						var weaponDefinitions = LoadRuleSection(rulesYaml, "Weapons");
						var voiceDefinitions = LoadRuleSection(rulesYaml, "Voices");
						var musicDefinitions = LoadRuleSection(rulesYaml, "Music");
						var notificationDefinitions = LoadRuleSection(rulesYaml, "Notifications");
						var sequenceDefinitions = LoadRuleSection(rulesYaml, "Sequences");
						var rules = Ruleset.Load(modData, this, TileSet, ruleDefinitions, weaponDefinitions,
							voiceDefinitions, notificationDefinitions, musicDefinitions, sequenceDefinitions);
						var flagged = Ruleset.DefinesUnsafeCustomRules(modData, this, ruleDefinitions,
							weaponDefinitions, voiceDefinitions, notificationDefinitions, sequenceDefinitions);
						return Pair.New(rules, flagged);
					});
				}
				catch (Exception) { }

				// Commit updated data before running the callbacks
				innerData = newData;

				if (innerData.Preview != null)
					cache.CacheMinimap(this);

				if (parseMetadata != null)
					parseMetadata(this);
			}

			// Update the status and class unconditionally
			innerData = newData;
		}

		public void Install(Action onSuccess)
		{
			if (Status != MapStatus.DownloadAvailable || !Game.Settings.Game.AllowDownloading)
				return;

			innerData.Status = MapStatus.Downloading;
			var installLocation = cache.MapLocations.FirstOrDefault(p => p.Value == MapClassification.User);
			if (installLocation.Key == null || !(installLocation.Key is IReadWritePackage))
			{
				Log.Write("debug", "Map install directory not found");
				innerData.Status = MapStatus.DownloadError;
				return;
			}

			var mapInstallPackage = installLocation.Key as IReadWritePackage;
			var modData = Game.ModData;
			new Thread(() =>
			{
				// Request the filename from the server
				// Run in a worker thread to avoid network delays
				var mapUrl = Game.Settings.Game.MapRepository + Uid;
				var mapFilename = string.Empty;
				try
				{
					var request = WebRequest.Create(mapUrl);
					request.Method = "HEAD";
					using (var res = request.GetResponse())
					{
						// Map not found
						if (res.Headers["Content-Disposition"] == null)
						{
							innerData.Status = MapStatus.DownloadError;
							return;
						}

						mapFilename = res.Headers["Content-Disposition"].Replace("attachment; filename = ", "");
					}

					Action<DownloadProgressChangedEventArgs> onDownloadProgress = i => { DownloadBytes = i.BytesReceived; DownloadPercentage = i.ProgressPercentage; };
					Action<DownloadDataCompletedEventArgs> onDownloadComplete = i =>
					{
						download = null;

						if (i.Error != null)
						{
							Log.Write("debug", "Remote map download failed with error: {0}", Download.FormatErrorMessage(i.Error));
							Log.Write("debug", "URL was: {0}", mapUrl);

							innerData.Status = MapStatus.DownloadError;
							return;
						}

						mapInstallPackage.Update(mapFilename, i.Result);
						Log.Write("debug", "Downloaded map to '{0}'", mapFilename);
						Game.RunAfterTick(() =>
						{
							var package = modData.ModFiles.OpenPackage(mapFilename, mapInstallPackage);
							if (package == null)
								innerData.Status = MapStatus.DownloadError;
							else
							{
								UpdateFromMap(package, mapInstallPackage, MapClassification.User, null, GridType);
								onSuccess();
							}
						});
					};

					download = new Download(mapUrl, onDownloadProgress, onDownloadComplete);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					innerData.Status = MapStatus.DownloadError;
				}
			}).Start();
		}

		public void CancelInstall()
		{
			if (download == null)
				return;

			download.CancelAsync();
			download = null;
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
			var deleteFromPackage = parentPackage as IReadWritePackage;
			if (deleteFromPackage != null)
				deleteFromPackage.Delete(Package.Name);
		}

		Stream IReadOnlyFileSystem.Open(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains("|") && Package.Contains(filename))
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
			if (!filename.Contains("|"))
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
			if (!filename.Contains("|") && Package.Contains(filename))
				return true;

			return modData.DefaultFileSystem.Exists(filename);
		}
	}
}
