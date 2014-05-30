#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
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
	public class MapCache : IEnumerable<MapPreview>
	{
		public static readonly MapPreview UnknownMap = new MapPreview(null, null);
		readonly Cache<string, MapPreview> previews;
		readonly ModData modData;
		readonly SheetBuilder sheetBuilder;
		Thread previewLoaderThread;
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
						var map = new Map(path.Key, modData.Manifest.Mod.Id);
						if (modData.Manifest.MapCompatibility.Contains(map.RequiresMod))
							previews[map.Uid].UpdateFromMap(map, path.Value);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed to load map: {0}", path);
					Console.WriteLine("Details: {0}", e);
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

			var url = Game.Settings.Game.MapRepository + "hash/" + string.Join(",", maps.Keys.ToArray()) + "/yaml";

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

			// Paths starting with ^ are relative to the user directory
			if (dir.StartsWith("^"))
				dir = Platform.SupportDir + dir.Substring(1);

			if (!Directory.Exists(dir))
				return noMaps;

			var dirsWithMaps = Directory.GetDirectories(dir)
				.Where(d => Directory.GetFiles(d, "map.yaml").Any() && Directory.GetFiles(d, "map.bin").Any());

			return dirsWithMaps.Concat(Directory.GetFiles(dir, "*.oramap"));
		}

		void LoadAsyncInternal()
		{
			for (;;)
			{
				MapPreview p;
				lock (syncRoot)
				{
					if (generateMinimap.Count == 0)
						break;

					p = generateMinimap.Peek();

					// Preview already exists
					if (p.Minimap != null)
					{
						generateMinimap.Dequeue();
						continue;
					}
				}

				// Render the minimap into the shared sheet
				// Note: this is not generally thread-safe, but it works here because:
				//   (a) This worker is the only thread writing to this sheet
				//   (b) The main thread is the only thread reading this sheet
				//   (c) The sheet is marked dirty after the write is completed,
				//       which causes the main thread to copy this to the texture during
				//       the next render cycle.
				//   (d) Any partially written bytes from the next minimap is in an
				//       unallocated area, and will be committed in the next cycle.
				var bitmap = p.CustomPreview ?? Minimap.RenderMapPreview(modData.DefaultRules.TileSets[p.Map.Tileset], p.Map, modData.DefaultRules, true);
				p.Minimap = sheetBuilder.Add(bitmap);

				lock (syncRoot)
					generateMinimap.Dequeue();

				// Yuck... But this helps the UI Jank when opening the map selector significantly.
				Thread.Sleep(50);
			}
		}

		public void CacheMinimap(MapPreview preview)
		{
			lock (syncRoot)
				generateMinimap.Enqueue(preview);

			if (previewLoaderThread == null || !previewLoaderThread.IsAlive)
			{
				previewLoaderThread = new Thread(LoadAsyncInternal);
				previewLoaderThread.Start();
			}
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
	}
}
