#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class MapCache : IEnumerable<MapPreview>, IDisposable
	{
		public static readonly MapPreview UnknownMap = new MapPreview(null, null, MapGridType.Rectangular, null);
		public readonly IReadOnlyDictionary<IReadOnlyPackage, MapClassification> MapLocations;
		readonly Dictionary<IReadOnlyPackage, MapClassification> mapLocations = new Dictionary<IReadOnlyPackage, MapClassification>();

		readonly Cache<string, MapPreview> previews;
		readonly ModData modData;
		readonly SheetBuilder sheetBuilder;
		Thread previewLoaderThread;
		bool previewLoaderThreadShutDown = true;
		object syncRoot = new object();
		Queue<MapPreview> generateMinimap = new Queue<MapPreview>();

		public Dictionary<string, string> StringPool { get; } = new Dictionary<string, string>();

		public MapCache(ModData modData)
		{
			this.modData = modData;

			var gridType = Exts.Lazy(() => modData.Manifest.Get<MapGrid>().Type);
			previews = new Cache<string, MapPreview>(uid => new MapPreview(modData, uid, gridType.Value, this));
			sheetBuilder = new SheetBuilder(SheetType.BGRA);

			MapLocations = new ReadOnlyDictionary<IReadOnlyPackage, MapClassification>(mapLocations);
		}

		public void LoadMaps()
		{
			// Utility mod that does not support maps
			if (!modData.Manifest.Contains<MapGrid>())
				return;

			// Enumerate map directories
			foreach (var kv in modData.Manifest.MapFolders)
			{
				var name = kv.Key;
				var classification = string.IsNullOrEmpty(kv.Value)
					? MapClassification.Unknown : Enum<MapClassification>.Parse(kv.Value);

				IReadOnlyPackage package;
				var optional = name.StartsWith("~", StringComparison.Ordinal);
				if (optional)
					name = name.Substring(1);

				try
				{
					// HACK: If the path is inside the the support directory then we may need to create it
					// Assume that the path is a directory if there is not an existing file with the same name
					var resolved = Platform.ResolvePath(name);
					if (resolved.StartsWith(Platform.SupportDir) && !File.Exists(resolved))
						Directory.CreateDirectory(resolved);

					package = modData.ModFiles.OpenPackage(name);
				}
				catch
				{
					if (optional)
						continue;

					throw;
				}

				mapLocations.Add(package, classification);
			}

			var mapGrid = modData.Manifest.Get<MapGrid>();
			foreach (var kv in MapLocations)
			{
				foreach (var map in kv.Key.Contents)
				{
					IReadOnlyPackage mapPackage = null;
					try
					{
						using (new Support.PerfTimer(map))
						{
							mapPackage = kv.Key.OpenPackage(map, modData.ModFiles);
							if (mapPackage == null)
								continue;

							var uid = Map.ComputeUID(mapPackage);
							previews[uid].UpdateFromMap(mapPackage, kv.Key, kv.Value, modData.Manifest.MapCompatibility, mapGrid.Type);
						}
					}
					catch (Exception e)
					{
						mapPackage?.Dispose();
						Console.WriteLine("Failed to load map: {0}", map);
						Console.WriteLine("Details: {0}", e);
						Log.Write("debug", "Failed to load map: {0}", map);
						Log.Write("debug", "Details: {0}", e);
					}
				}
			}
		}

		public IEnumerable<IReadWritePackage> EnumerateMapPackagesWithoutCaching(MapClassification classification = MapClassification.System)
		{
			// Utility mod that does not support maps
			if (!modData.Manifest.Contains<MapGrid>())
				yield break;

			// Enumerate map directories
			foreach (var kv in modData.Manifest.MapFolders)
			{
				if (!Enum.TryParse(kv.Value, out MapClassification packageClassification))
					continue;

				if (!classification.HasFlag(packageClassification))
					continue;

				var name = kv.Key;
				var optional = name.StartsWith("~", StringComparison.Ordinal);
				if (optional)
					name = name.Substring(1);

				// Don't try to open the map directory in the support directory if it doesn't exist
				var resolved = Platform.ResolvePath(name);
				if (resolved.StartsWith(Platform.SupportDir) && (!Directory.Exists(resolved) || !File.Exists(resolved)))
					continue;

				using (var package = (IReadWritePackage)modData.ModFiles.OpenPackage(name))
				{
					foreach (var map in package.Contents)
					{
						var mapPackage = package.OpenPackage(map, modData.ModFiles) as IReadWritePackage;
						if (mapPackage != null)
							yield return mapPackage;
					}
				}
			}
		}

		public IEnumerable<Map> EnumerateMapsWithoutCaching(MapClassification classification = MapClassification.System)
		{
			foreach (var mapPackage in EnumerateMapPackagesWithoutCaching(classification))
				yield return new Map(modData, mapPackage);
		}

		public void QueryRemoteMapDetails(string repositoryUrl, IEnumerable<string> uids, Action<MapPreview> mapDetailsReceived = null, Action queryFailed = null)
		{
			var maps = uids.Distinct()
				.Select(uid => previews[uid])
				.Where(p => p.Status == MapStatus.Unavailable)
				.ToDictionary(p => p.Uid, p => p);

			if (!maps.Any())
				return;

			foreach (var p in maps.Values)
				p.UpdateRemoteSearch(MapStatus.Searching, null);

			var url = repositoryUrl + "hash/" + string.Join(",", maps.Keys) + "/yaml";

			Action<DownloadDataCompletedEventArgs> onInfoComplete = i =>
			{
				if (i.Error != null)
				{
					Log.Write("debug", "Remote map query failed with error: {0}", Download.FormatErrorMessage(i.Error));
					Log.Write("debug", "URL was: {0}", url);
					foreach (var p in maps.Values)
						p.UpdateRemoteSearch(MapStatus.Unavailable, null);

					queryFailed?.Invoke();

					return;
				}

				var data = Encoding.UTF8.GetString(i.Result);
				try
				{
					var yaml = MiniYaml.FromString(data);
					foreach (var kv in yaml)
						maps[kv.Key].UpdateRemoteSearch(MapStatus.DownloadAvailable, kv.Value, mapDetailsReceived);

					foreach (var map in maps)
						if (map.Value.Status != MapStatus.DownloadAvailable)
							map.Value.UpdateRemoteSearch(MapStatus.Unavailable, null);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Can't parse remote map search data:\n{0}", data);
					Log.Write("debug", "Exception: {0}", e);
					queryFailed?.Invoke();
				}
			};

			new Download(url, _ => { }, onInfoComplete);
		}

		void LoadAsyncInternal()
		{
			Log.Write("debug", "MapCache.LoadAsyncInternal started");

			// Milliseconds to wait on one loop when nothing to do
			var emptyDelay = 50;

			// Keep the thread alive for at least 5 seconds after the last minimap generation
			var maxKeepAlive = 5000 / emptyDelay;
			var keepAlive = maxKeepAlive;

			while (true)
			{
				List<MapPreview> todo;
				lock (syncRoot)
				{
					todo = generateMinimap.Where(p => p.GetMinimap() == null).ToList();
					generateMinimap.Clear();
					if (keepAlive > 0)
						keepAlive--;
					if (keepAlive == 0 && todo.Count == 0)
					{
						previewLoaderThreadShutDown = true;
						break;
					}
				}

				if (todo.Count == 0)
				{
					Thread.Sleep(emptyDelay);
					continue;
				}
				else
					keepAlive = maxKeepAlive;

				// Render the minimap into the shared sheet
				foreach (var p in todo)
				{
					if (p.Preview != null)
					{
						Game.RunAfterTick(() =>
						{
							try
							{
								p.SetMinimap(sheetBuilder.Add(p.Preview));
							}
							catch (Exception e)
							{
								Log.Write("debug", "Failed to load minimap with exception: {0}", e);
							}
						});
					}

					// Yuck... But this helps the UI Jank when opening the map selector significantly.
					Thread.Sleep(Environment.ProcessorCount == 1 ? 25 : 5);
				}
			}

			// Release the buffer by forcing changes to be written out to the texture, allowing the buffer to be reclaimed by GC.
			Game.RunAfterTick(sheetBuilder.Current.ReleaseBuffer);
			Log.Write("debug", "MapCache.LoadAsyncInternal ended");
		}

		public void CacheMinimap(MapPreview preview)
		{
			bool launchPreviewLoaderThread;
			lock (syncRoot)
			{
				generateMinimap.Enqueue(preview);
				launchPreviewLoaderThread = previewLoaderThreadShutDown;
				previewLoaderThreadShutDown = false;
			}

			if (launchPreviewLoaderThread)
				Game.RunAfterTick(() =>
				{
					// Wait for any existing thread to exit before starting a new one.
					previewLoaderThread?.Join();

					previewLoaderThread = new Thread(LoadAsyncInternal)
					{
						Name = "Map Preview Loader",
						IsBackground = true
					};
					previewLoaderThread.Start();
				});
		}

		bool IsSuitableInitialMap(MapPreview map)
		{
			if (map.Status != MapStatus.Available || !map.Visibility.HasFlag(MapVisibility.Lobby))
				return false;

			// Other map types may have confusing settings or gameplay
			if (!map.Categories.Contains("Conquest"))
				return false;

			// Maps with bots disabled confuse new players
			if (map.Players.Players.Any(x => !x.Value.AllowBots))
				return false;

			// Large maps expose unfortunate performance problems
			if (map.Bounds.Width > 128 || map.Bounds.Height > 128)
				return false;

			return true;
		}

		public string ChooseInitialMap(string initialUid, MersenneTwister random)
		{
			if (string.IsNullOrEmpty(initialUid) || previews[initialUid].Status != MapStatus.Available)
			{
				var selected = previews.Values.Where(IsSuitableInitialMap).RandomOrDefault(random) ??
					previews.Values.FirstOrDefault(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Lobby));
				return selected == null ? string.Empty : selected.Uid;
			}

			return initialUid;
		}

		public MapPreview this[string key]
		{
			get { return previews[key]; }
		}

		public IEnumerator<MapPreview> GetEnumerator()
		{
			return previews.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Dispose()
		{
			if (previewLoaderThread == null)
			{
				sheetBuilder.Dispose();
				return;
			}

			foreach (var p in previews.Values)
				p.Dispose();

			// We need to let the loader thread exit before we can dispose our sheet builder.
			// Ideally we should dispose our resources before returning, but we don't to block waiting on the loader thread to exit.
			// Instead, we'll queue disposal to be run once it has exited.
			ThreadPool.QueueUserWorkItem(_ =>
			{
				previewLoaderThread.Join();
				Game.RunAfterTick(sheetBuilder.Dispose);
			});
		}
	}
}
