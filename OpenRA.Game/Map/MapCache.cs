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

		readonly Cache<string, MapPreview> previews;
		readonly ModData modData;
		readonly SheetBuilder sheetBuilder;
		Thread previewLoaderThread;
		bool previewLoaderThreadShutDown = true;
		object syncRoot = new object();
		Queue<MapPreview> generateMinimap = new Queue<MapPreview>();

		public MapCache(ModData modData)
		{
			this.modData = modData;

			var gridType = Exts.Lazy(() => modData.Manifest.Get<MapGrid>().Type);
			previews = new Cache<string, MapPreview>(uid => new MapPreview(modData, uid, gridType.Value, this));
			sheetBuilder = new SheetBuilder(SheetType.BGRA);

			// Enumerate map directories
			var mapLocations = new Dictionary<IReadOnlyPackage, MapClassification>();
			foreach (var kv in modData.Manifest.MapFolders)
			{
				var name = kv.Key;
				var classification = string.IsNullOrEmpty(kv.Value)
					? MapClassification.Unknown : Enum<MapClassification>.Parse(kv.Value);

				IReadOnlyPackage package;
				var optional = name.StartsWith("~");
				if (optional)
					name = name.Substring(1);

				try
				{
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

			MapLocations = new ReadOnlyDictionary<IReadOnlyPackage, MapClassification>(mapLocations);
		}

		public void LoadMaps()
		{
			// Utility mod that does not support maps
			if (!modData.Manifest.Contains<MapGrid>())
				return;

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
							mapPackage = modData.ModFiles.OpenPackage(map, kv.Key);
							if (mapPackage == null)
								continue;

							var uid = Map.ComputeUID(mapPackage);
							previews[uid].UpdateFromMap(mapPackage, kv.Key, kv.Value, modData.Manifest.MapCompatibility, mapGrid.Type);
						}
					}
					catch (Exception e)
					{
						if (mapPackage != null)
							mapPackage.Dispose();
						Console.WriteLine("Failed to load map: {0}", map);
						Console.WriteLine("Details: {0}", e);
						Log.Write("debug", "Failed to load map: {0}", map);
						Log.Write("debug", "Details: {0}", e);
					}
				}
			}
		}

		public void QueryRemoteMapDetails(IEnumerable<string> uids, Action<MapPreview> mapDetailsReceived = null, Action queryFailed = null)
		{
			var maps = uids.Distinct()
				.Select(uid => previews[uid])
				.Where(p => p.Status == MapStatus.Unavailable)
				.ToDictionary(p => p.Uid, p => p);

			if (!maps.Any())
				return;

			foreach (var p in maps.Values)
				p.UpdateRemoteSearch(MapStatus.Searching, null);

			var url = Game.Settings.Game.MapRepository + "hash/" + string.Join(",", maps.Keys) + "/yaml";

			Action<DownloadDataCompletedEventArgs> onInfoComplete = i =>
			{
				if (i.Error != null)
				{
					Log.Write("debug", "Remote map query failed with error: {0}", Download.FormatErrorMessage(i.Error));
					Log.Write("debug", "URL was: {0}", url);
					foreach (var p in maps.Values)
						p.UpdateRemoteSearch(MapStatus.Unavailable, null);

					if (queryFailed != null)
						queryFailed();

					return;
				}

				var data = Encoding.UTF8.GetString(i.Result);
				try
				{
					var yaml = MiniYaml.FromString(data);
					foreach (var kv in yaml)
						maps[kv.Key].UpdateRemoteSearch(MapStatus.DownloadAvailable, kv.Value, mapDetailsReceived);
				}
				catch
				{
					Log.Write("debug", "Can't parse remote map search data:\n{0}", data);
					if (queryFailed != null)
						queryFailed();
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

			for (;;)
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
						Game.RunAfterTick(() => p.SetMinimap(sheetBuilder.Add(p.Preview)));

					// Yuck... But this helps the UI Jank when opening the map selector significantly.
					Thread.Sleep(Environment.ProcessorCount == 1 ? 25 : 5);
				}
			}

			// The buffer is not fully reclaimed until changes are written out to the texture.
			// We will access the texture in order to force changes to be written out, allowing the buffer to be freed.
			Game.RunAfterTick(() =>
			{
				sheetBuilder.Current.ReleaseBuffer();
				sheetBuilder.Current.GetTexture();
			});
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
					if (previewLoaderThread != null)
						previewLoaderThread.Join();

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
					previews.Values.First(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Lobby));
				return selected.Uid;
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
