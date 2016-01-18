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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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

	public class MapPreview
	{
		static readonly CPos[] NoSpawns = new CPos[] { };
		MapCache cache;

		public readonly string Uid;
		public string Title { get; private set; }
		public string Type { get; private set; }
		public string Author { get; private set; }
		public int PlayerCount { get; private set; }
		public CPos[] SpawnPoints { get; private set; }
		public MapGridType GridType { get; private set; }
		public Rectangle Bounds { get; private set; }
		public Bitmap CustomPreview { get; private set; }
		public Map Map { get; private set; }
		public MapStatus Status { get; private set; }
		public MapClassification Class { get; private set; }
		public bool SuitableForInitialMap { get; private set; }

		public MapRuleStatus RuleStatus { get; private set; }

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

		public void SetMinimap(Sprite minimap)
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
		}

		public void UpdateFromMap(Map m, MapClassification classification)
		{
			Map = m;
			Title = m.Title;
			Type = m.Type;
			Type = m.Type;
			Author = m.Author;
			Bounds = m.Bounds;
			SpawnPoints = m.SpawnPoints.Value;
			GridType = m.Grid.Type;
			CustomPreview = m.CustomPreview;
			Status = MapStatus.Available;
			Class = classification;

			var players = new MapPlayers(m.PlayerDefinitions).Players;
			PlayerCount = players.Count(x => x.Value.Playable);

			SuitableForInitialMap = EvaluateUserFriendliness(players);
		}

		bool EvaluateUserFriendliness(Dictionary<string, PlayerReference> players)
		{
			if (Status != MapStatus.Available || !Map.Visibility.HasFlag(MapVisibility.Lobby))
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
							RuleStatus = MapRuleStatus.Invalid;
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
			var baseMapPath = Platform.ResolvePath("^", "maps", Game.ModData.Manifest.Mod.Id);

			// Create the map directory if it doesn't exist
			if (!Directory.Exists(baseMapPath))
				Directory.CreateDirectory(baseMapPath);

			new Thread(() =>
			{
				// Request the filename from the server
				// Run in a worker thread to avoid network delays
				var mapUrl = Game.Settings.Game.MapRepository + Uid;
				var mapPath = string.Empty;
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

						mapPath = Path.Combine(baseMapPath, res.Headers["Content-Disposition"].Replace("attachment; filename = ", ""));
					}

					Action<DownloadProgressChangedEventArgs> onDownloadProgress = i => { DownloadBytes = i.BytesReceived; DownloadPercentage = i.ProgressPercentage; };
					Action<AsyncCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
					{
						download = null;

						if (cancelled || i.Error != null)
						{
							Log.Write("debug", "Remote map download failed with error: {0}", i.Error != null ? i.Error.Message : "cancelled");
							Log.Write("debug", "URL was: {0}", mapUrl);

							Status = MapStatus.DownloadError;
							return;
						}

						Log.Write("debug", "Downloaded map to '{0}'", mapPath);
						Game.RunAfterTick(() =>
						{
							UpdateFromMap(new Map(mapPath), MapClassification.User);
							CacheRules();
						});
					};

					download = new Download(mapUrl, mapPath, onDownloadProgress, onDownloadComplete);
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

		public void CacheRules()
		{
			if (RuleStatus != MapRuleStatus.Unknown)
				return;

			Map.PreloadRules();
			RuleStatus = Map.InvalidCustomRules ? MapRuleStatus.Invalid : MapRuleStatus.Cached;
		}

		public void Invalidate()
		{
			Status = MapStatus.Unavailable;
			RuleStatus = MapRuleStatus.Unknown;
		}
	}
}
