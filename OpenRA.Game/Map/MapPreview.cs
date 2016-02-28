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
using System.Threading;
using OpenRA.FileSystem;
using OpenRA.Graphics;

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
		public readonly string map_type;
		public readonly int players;
		public readonly Rectangle bounds;
		public readonly int[] spawnpoints = { };
		public readonly MapGridType map_grid_type;
		public readonly string minimap;
		public readonly bool downloading;
	}

	public class MapPreview : IDisposable
	{
		static readonly CPos[] NoSpawns = new CPos[] { };
		MapCache cache;

		public readonly string Uid;
		public IReadOnlyPackage Package { get; private set; }
		IReadOnlyPackage parentPackage;

		public string Title { get; private set; }
		public string Type { get; private set; }
		public string Author { get; private set; }
		public int PlayerCount { get; private set; }
		public CPos[] SpawnPoints { get; private set; }
		public MapGridType GridType { get; private set; }
		public Rectangle Bounds { get; private set; }
		public Bitmap CustomPreview { get; private set; }
		public MapStatus Status { get; private set; }
		public MapClassification Class { get; private set; }
		public MapVisibility Visibility { get; private set; }
		public bool SuitableForInitialMap { get; private set; }

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

		public MapPreview(string uid, MapGridType gridType, MapCache cache)
		{
			this.cache = cache;
			Uid = uid;
			Title = "Unknown Map";
			Type = "Unknown";
			Author = "Unknown Author";
			PlayerCount = 0;
			Bounds = Rectangle.Empty;
			SpawnPoints = NoSpawns;
			GridType = gridType;
			Status = MapStatus.Unavailable;
			Class = MapClassification.Unknown;
			Visibility = MapVisibility.Lobby;
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
			GridType = gridType;
			Class = classification;

			MiniYaml temp;
			if (yaml.TryGetValue("MapFormat", out temp))
			{
				var format = FieldLoader.GetValue<int>("MapFormat", temp.Value);
				if (format != Map.SupportedMapFormat)
					throw new InvalidDataException("Map format {0} is not supported.".F(format));
			}

			if (yaml.TryGetValue("Title", out temp))
				Title = temp.Value;
			if (yaml.TryGetValue("Type", out temp))
				Type = temp.Value;
			if (yaml.TryGetValue("Author", out temp))
				Author = temp.Value;
			if (yaml.TryGetValue("Bounds", out temp))
				Bounds = FieldLoader.GetValue<Rectangle>("Bounds", temp.Value);
			if (yaml.TryGetValue("Visibility", out temp))
				Visibility = FieldLoader.GetValue<MapVisibility>("Visibility", temp.Value);

			string requiresMod = string.Empty;
			if (yaml.TryGetValue("RequiresMod", out temp))
				requiresMod = temp.Value;

			Status = mapCompatibility == null || mapCompatibility.Contains(requiresMod) ? MapStatus.Available : MapStatus.Unavailable;

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

					SpawnPoints = spawns.ToArray();
				}
				else
					SpawnPoints = new CPos[0];
			}
			catch (Exception)
			{
				SpawnPoints = new CPos[0];
				Status = MapStatus.Unavailable;
			}

			try
			{
				// Player definitions may change if the map format changes
				MiniYaml playerDefinitions;
				if (yaml.TryGetValue("Players", out playerDefinitions))
				{
					var players = new MapPlayers(playerDefinitions.Nodes).Players;
					PlayerCount = players.Count(x => x.Value.Playable);
					SuitableForInitialMap = EvaluateUserFriendliness(players);
				}
			}
			catch (Exception)
			{
				Status = MapStatus.Unavailable;
			}

			if (p.Contains("map.png"))
				using (var dataStream = p.GetStream("map.png"))
					CustomPreview = new Bitmap(dataStream);
		}

		bool EvaluateUserFriendliness(Dictionary<string, PlayerReference> players)
		{
			if (Status != MapStatus.Available || !Visibility.HasFlag(MapVisibility.Lobby))
				return false;

			// Other map types may have confusing settings or gameplay
			if (Type != "Conquest")
				return false;

			// Maps with bots disabled confuse new players
			if (players.Any(x => !x.Value.AllowBots))
				return false;

			// Large maps expose unfortunate performance problems
			if (Bounds.Width > 128 || Bounds.Height > 128)
				return false;

			return true;
		}

		public void UpdateRemoteSearch(MapStatus status, MiniYaml yaml)
		{
			// Update on the main thread to ensure consistency
			Game.RunAfterTick(() =>
			{
				if (status == MapStatus.DownloadAvailable)
				{
					try
					{
						var r = FieldLoader.Load<RemoteMapData>(yaml);

						// Map download has been disabled server side
						if (!r.downloading)
						{
							Status = MapStatus.Unavailable;
							return;
						}

						Title = r.title;
						Type = r.map_type;
						Author = r.author;
						PlayerCount = r.players;
						Bounds = r.bounds;

						var spawns = new CPos[r.spawnpoints.Length / 2];
						for (var j = 0; j < r.spawnpoints.Length; j += 2)
							spawns[j / 2] = new CPos(r.spawnpoints[j], r.spawnpoints[j + 1]);
						SpawnPoints = spawns;
						GridType = r.map_grid_type;

						CustomPreview = new Bitmap(new MemoryStream(Convert.FromBase64String(r.minimap)));
					}
					catch (Exception) { }

					if (CustomPreview != null)
						cache.CacheMinimap(this);
				}

				Status = status;
				Class = MapClassification.Remote;
			});
		}

		public void Install()
		{
			if (Status != MapStatus.DownloadAvailable || !Game.Settings.Game.AllowDownloading)
				return;

			Status = MapStatus.Downloading;
			var installLocation = cache.MapLocations.FirstOrDefault(p => p.Value == MapClassification.User);
			if (installLocation.Key == null || !(installLocation.Key is IReadWritePackage))
			{
				Log.Write("debug", "Map install directory not found");
				Status = MapStatus.DownloadError;
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
							Status = MapStatus.DownloadError;
							return;
						}

						mapFilename = res.Headers["Content-Disposition"].Replace("attachment; filename = ", "");
					}

					Action<DownloadProgressChangedEventArgs> onDownloadProgress = i => { DownloadBytes = i.BytesReceived; DownloadPercentage = i.ProgressPercentage; };
					Action<DownloadDataCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
					{
						download = null;

						if (cancelled || i.Error != null)
						{
							Log.Write("debug", "Remote map download failed with error: {0}", i.Error != null ? i.Error.Message : "cancelled");
							Log.Write("debug", "URL was: {0}", mapUrl);

							Status = MapStatus.DownloadError;
							return;
						}

						mapInstallPackage.Update(mapFilename, i.Result);
						Log.Write("debug", "Downloaded map to '{0}'", mapFilename);
						Game.RunAfterTick(() =>
						{
							var package = modData.ModFiles.OpenPackage(mapFilename, mapInstallPackage);
							UpdateFromMap(package, mapInstallPackage, MapClassification.User, null, GridType);
						});
					};

					download = new Download(mapUrl, onDownloadProgress, onDownloadComplete);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Status = MapStatus.DownloadError;
				}
			}).Start();
		}

		public void CancelInstall()
		{
			if (download == null)
				return;

			download.Cancel();
			download = null;
		}

		public void Invalidate()
		{
			Status = MapStatus.Unavailable;
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
	}
}
