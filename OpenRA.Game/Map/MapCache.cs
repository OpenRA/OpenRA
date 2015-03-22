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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public sealed class MapCache : IEnumerable<MapPreview>, IDisposable
	{
		public static readonly MapPreview UnknownMap = new MapPreview(null, null);
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
			previews = new Cache<string, MapPreview>(uid => new MapPreview(uid, this));
			sheetBuilder = new SheetBuilder(SheetType.BGRA);
		}

		public void LoadMaps()
		{
			// Expand the dictionary (dir path, dir type) to a dictionary of (map path, dir type)
			var mapPaths = modData.Manifest.MapFolders.SelectMany(kv =>
				FindMapsIn(kv.Key).ToDictionary(p => p, p => string.IsNullOrEmpty(kv.Value) ? MapClassification.Unknown : Enum<MapClassification>.Parse(kv.Value)));

			foreach (var path in mapPaths)
			{
				try
				{
					using (new Support.PerfTimer(path.Key))
					{
						var map = new Map(path.Key);
						if (modData.Manifest.MapCompatibility.Contains(map.RequiresMod))
							previews[map.Uid].UpdateFromMap(map, path.Value);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed to load map: {0}", path);
					Console.WriteLine("Details: {0}", e);
					Log.Write("debug", "Failed to load map: {0}", path);
					Log.Write("debug", "Details: {0}", e);
				}
			}
		}

		public void QueryRemoteMapDetails(IEnumerable<string> uids)
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

			Action<DownloadDataCompletedEventArgs, bool> onInfoComplete = (i, cancelled) =>
			{
				if (cancelled || i.Error != null)
				{
					Log.Write("debug", "Remote map query failed with error: {0}", i.Error != null ? i.Error.Message : "cancelled");
					Log.Write("debug", "URL was: {0}", url);
					foreach (var p in maps.Values)
						p.UpdateRemoteSearch(MapStatus.Unavailable, null);

					return;
				}

				var data = Encoding.UTF8.GetString(i.Result);
				try
				{
					var yaml = MiniYaml.FromString(data);
					foreach (var kv in yaml)
						maps[kv.Key].UpdateRemoteSearch(MapStatus.DownloadAvailable, kv.Value);
				}
				catch
				{
					Log.Write("debug", "Can't parse remote map search data:\n{0}", data);
				}
			};

			new Download(url, _ => { }, onInfoComplete);
		}

		public static IEnumerable<string> FindMapsIn(string dir)
		{
			string[] noMaps = { };

			// Ignore optional flag
			if (dir.StartsWith("~"))
				dir = dir.Substring(1);

			dir = Platform.ResolvePath(dir);

			if (!Directory.Exists(dir))
				return noMaps;

			var dirsWithMaps = Directory.GetDirectories(dir)
				.Where(d => Directory.GetFiles(d, "map.yaml").Any() && Directory.GetFiles(d, "map.bin").Any());

			return dirsWithMaps.Concat(Directory.GetFiles(dir, "*.oramap"));
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
					// The rendering is thread safe because it only reads from the passed instances and writes to a new bitmap
					var createdPreview = false;
					var bitmap = p.CustomPreview;
					var campaignBitmap = p.CustomCampaignPathPreview;
					if (bitmap == null)
					{
						createdPreview = true;
						bitmap = Minimap.RenderMapPreview(modData.DefaultRules.TileSets[p.Map.Tileset], p.Map, modData.DefaultRules, true);
					}

					Game.RunAfterTick(() =>
					{
						try
						{
							p.SetMinimap(sheetBuilder.Add(bitmap));
							if (campaignBitmap != null)
								p.SetCampaignPathPreview(sheetBuilder.Add(campaignBitmap));
						}
						finally
						{
							if (createdPreview)
							{
								bitmap.Dispose();
								if (campaignBitmap != null)
									campaignBitmap.Dispose();
							}
						}
					});

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
