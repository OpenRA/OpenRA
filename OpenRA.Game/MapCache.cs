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
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA
{
	public class MapCache : IEnumerable<MapPreview>
	{
		public static readonly MapPreview UnknownMap = new MapPreview(null, null);
		readonly Cache<string, MapPreview> previews;
		readonly Manifest manifest;
		readonly SheetBuilder sheetBuilder;
		Thread previewLoaderThread;
		object syncRoot = new object();
		Queue<MapPreview> generateMinimap = new Queue<MapPreview>();

		public MapCache(Manifest m)
		{
			manifest = m;
			previews = new Cache<string, MapPreview>(uid => new MapPreview(uid, this));
			sheetBuilder = new SheetBuilder(SheetType.BGRA);
		}

		public void LoadMaps()
		{
			var paths = manifest.MapFolders.SelectMany(f => FindMapsIn(f));
			foreach (var path in paths)
			{
				try
				{
					var map = new Map(path, manifest.Mod.Id);
					if (manifest.MapCompatibility.Contains(map.RequiresMod))
						previews[map.Uid].UpdateFromMap(map);
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed to load map: {0}", path);
					Console.WriteLine("Details: {0}", e);
				}
			}
		}

		static IEnumerable<string> FindMapsIn(string dir)
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
				var bitmap = Minimap.RenderMapPreview(p.Map, true);
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
