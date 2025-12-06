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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA
{
	public sealed class MapCache : IEnumerable<MapPreview>, IDisposable
	{
		public static readonly MapPreview UnknownMap = new(null, null, MapGridType.Rectangular, null);
		public IReadOnlyDictionary<IReadOnlyPackage, MapClassification> MapLocations => mapLocations;
		readonly Dictionary<IReadOnlyPackage, MapClassification> mapLocations = [];
		public bool LoadPreviewImages = true;

		readonly Manifest manifest;
		readonly FS modFiles;
		Cache<string, MapPreview> previews;
		readonly SheetBuilder sheetBuilder;
		Thread previewLoaderThread;
		bool previewLoaderThreadShutDown = true;
		readonly object syncRoot = new();
		readonly Queue<MapPreview> generateMinimap = [];

		public HashSet<string> StringPool { get; } = [];

		readonly List<MapDirectoryTracker> mapDirectoryTrackers = [];

		/// <summary>
		/// The most recently modified or loaded map at runtime.
		/// </summary>
		public string LastModifiedMap { get; private set; } = null;
		readonly Dictionary<string, string> mapUpdates = [];

		string lastLoadedLastModifiedMap;

		/// <summary>
		/// If LastModifiedMap was picked already, returns a null.
		/// </summary>
		public string PickLastModifiedMap(MapVisibility visibility)
		{
			UpdateMaps();
			var map = string.IsNullOrEmpty(LastModifiedMap) ? null : this[LastModifiedMap];
			if (map != null && map.Status == MapStatus.Available && map.Visibility.HasFlag(visibility) && lastLoadedLastModifiedMap != LastModifiedMap)
			{
				lastLoadedLastModifiedMap = LastModifiedMap;
				return lastLoadedLastModifiedMap;
			}

			return null;
		}

		public MapCache(Manifest manifest, FS modFiles)
		{
			this.manifest = manifest;
			this.modFiles = modFiles;
			sheetBuilder = new SheetBuilder(SheetType.BGRA, manifest.RendererConstants.MapPreviewSheetSize);
		}

		public void UpdateMaps()
		{
			foreach (var tracker in mapDirectoryTrackers)
				tracker.UpdateMaps(this);
		}

		public void LoadMaps(ModData modData)
		{
			// Utility mod that does not support maps
			if (manifest.MapFolders.Count == 0)
				return;

			var gridType = modData.GetOrCreate<MapGrid>().Type;
			previews = new Cache<string, MapPreview>(uid => new MapPreview(modData, uid, gridType, this));

			// Enumerate map directories
			foreach (var kv in manifest.MapFolders)
			{
				var name = kv.Key;
				var classification = string.IsNullOrEmpty(kv.Value)
					? MapClassification.Unknown : Enum.Parse<MapClassification>(kv.Value);

				IReadOnlyPackage package;
				var optional = name.StartsWith('~');
				if (optional)
					name = name[1..];

				try
				{
					// HACK: If the path is inside the support directory then we may need to create it
					// Assume that the path is a directory if there is not an existing file with the same name
					var resolved = Platform.ResolvePath(name);
					if (resolved.StartsWith(Platform.SupportDir, StringComparison.Ordinal) && !File.Exists(resolved))
						Directory.CreateDirectory(resolved);

					package = modFiles.OpenPackage(name);
				}
				catch
				{
					if (optional)
						continue;

					throw;
				}

				mapLocations.Add(package, classification);
				mapDirectoryTrackers.Add(new MapDirectoryTracker(package, classification));
			}

			// PERF: Load the mod YAML once outside the loop, and reuse it when resolving each maps custom YAML.
			var modDataRules = modData.GetRulesYaml();
			foreach (var kv in MapLocations)
				foreach (var map in kv.Key.Contents)
					LoadMapInternal(map, kv.Key, kv.Value, null, gridType, modDataRules);

			// We only want to track maps in runtime, not at loadtime
			LastModifiedMap = null;
		}

		public void LoadMap(string map, IReadOnlyPackage package, MapClassification classification, string oldMap)
		{
			LoadMapInternal(map, package, classification, oldMap);
		}

		void LoadMapInternal(string map, IReadOnlyPackage package, MapClassification classification, string oldMap,
			MapGridType? gridType = null, MiniYamlNode[][] modDataRules = null)
		{
			IReadOnlyPackage mapPackage = null;
			try
			{
				using (new PerfTimer(map))
				{
					mapPackage = package.OpenPackage(map, modFiles);
					if (mapPackage != null)
					{
						var uid = Map.ComputeUID(mapPackage);
						previews[uid].UpdateFromMapWithoutOwningPackage(mapPackage, package, classification, gridType, modDataRules);
						mapPackage.Dispose();

						if (oldMap != uid)
						{
							LastModifiedMap = uid;
							if (oldMap != null)
								mapUpdates[oldMap] = uid;
						}
					}
				}
			}
			catch (Exception e)
			{
				mapPackage?.Dispose();
				Console.WriteLine($"Failed to load map: {map}");
				Console.WriteLine("Details:");
				Console.WriteLine(e);
				Log.Write("debug", $"Failed to load map: {map}");
				Log.Write("debug", "Details:");
				Log.Write("debug", e);
			}
		}

		public IEnumerable<IReadWritePackage> EnumerateMapDirPackages(MapClassification classification = MapClassification.System)
		{
			// Enumerate map directories
			foreach (var kv in manifest.MapFolders)
			{
				if (!Enum.TryParse(kv.Value, out MapClassification packageClassification))
					continue;

				if (!classification.HasFlag(packageClassification))
					continue;

				var name = kv.Key;
				var optional = name.StartsWith('~');
				if (optional)
					name = name[1..];

				// Don't try to open the map directory in the support directory if it doesn't exist
				var resolved = Platform.ResolvePath(name);
				if (resolved.StartsWith(Platform.SupportDir, StringComparison.Ordinal) && (!Directory.Exists(resolved) || !File.Exists(resolved)))
					continue;

				using (var package = (IReadWritePackage)modFiles.OpenPackage(name))
					yield return package;
			}
		}

		public IEnumerable<(IReadWritePackage Package, string Map)> EnumerateMapDirPackagesAndNames(MapClassification classification = MapClassification.System)
		{
			var mapDirPackages = EnumerateMapDirPackages(classification);

			foreach (var mapDirPackage in mapDirPackages)
				foreach (var map in mapDirPackage.Contents)
					yield return (mapDirPackage, map);
		}

		public IEnumerable<IReadWritePackage> EnumerateMapPackagesWithoutCaching(MapClassification classification = MapClassification.System)
		{
			var mapDirPackages = EnumerateMapDirPackages(classification);

			foreach (var mapDirPackage in mapDirPackages)
				foreach (var map in mapDirPackage.Contents)
					if (mapDirPackage.OpenPackage(map, modFiles) is IReadWritePackage mapPackage)
						yield return mapPackage;
		}

		public void GenerateMap(ModData modData, MapGenerationArgs args)
		{
			var p = previews[args.Uid];
			if (p.Class == MapClassification.Generated)
				return;

			p.UpdateFromGenerationArgs(args);

			Task.Run(() =>
			{
				try
				{
					var generator = modData.DefaultRules.Actors[SystemActors.EditorWorld]
						.TraitInfos<IMapGeneratorInfo>()
						.FirstOrDefault(info => info.Type == args.Generator);

					if (generator == null)
						throw new Exception($"Unknown map generator type {args.Generator}");

					var map = generator.Generate(modData, args);

					// Uid is generated when the map is saved
					map.Save(new ZipFileLoader.ReadWriteZipFile());

					if (map.Uid != args.Uid)
						throw new InvalidOperationException("Map generation UID mismatch");

					Game.RunAfterTick(() => p.UpdateFromMap(map.Package, MapClassification.Generated));
				}
				catch (Exception e)
				{
					Log.Write("debug", "Map generation failed with error:");
					Log.Write("debug", e);

					p.UpdateFromGenerationArgs(null);
				}
			});
		}

		public void QueryRemoteMapDetails(string repositoryUrl, IEnumerable<string> uids,
			Action<MapPreview> mapDetailsReceived = null, Action<MapPreview> mapQueryFailed = null)
		{
			var queryUids = uids.Distinct()
				.Where(uid => uid != null)
				.Select(uid => previews[uid])
				.Where(p => p.Status == MapStatus.Unavailable)
				.Select(p => p.Uid)
				.ToList();

			foreach (var uid in queryUids)
				previews[uid].BeginRemoteSearch();

			Task.Run(async () =>
			{
				var client = HttpClientFactory.Create();
				var stringPool = new HashSet<string>(); // Reuse common strings in YAML

				// Limit each query to 50 maps at a time to avoid request size limits
				foreach (var batchUids in queryUids.Chunk(50))
				{
					var url = repositoryUrl + "hash/" + string.Join(",", batchUids) + "/yaml";
					using (new PerfTimer("RemoteMapDetails"))
					{
						try
						{
							var result = await client.GetStreamAsync(url);
							foreach (var kv in MiniYaml.FromStream(result, url, stringPool: stringPool))
								previews[kv.Key].CompleteRemoteSearch(kv.Value, mapDetailsReceived);
						}
						catch (Exception e)
						{
							Log.Write("debug", "Remote map query failed with error:");
							Log.Write("debug", e);
							Log.Write("debug", $"URL was: {url}");
						}

						foreach (var uid in batchUids)
						{
							var p = previews[uid];
							if (p.Status == MapStatus.Searching)
								p.CompleteRemoteSearch(null, mapQueryFailed);
						}
					}
				}
			});
		}

		void LoadAsyncInternal()
		{
			Log.Write("debug", "MapCache.LoadAsyncInternal started");

			// Milliseconds to wait on one loop when nothing to do
			const int EmptyDelay = 50;

			// Keep the thread alive for at least 5 seconds after the last minimap generation
			const int MaxKeepAlive = 5000 / EmptyDelay;
			var keepAlive = MaxKeepAlive;

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
					Thread.Sleep(EmptyDelay);
					continue;
				}
				else
					keepAlive = MaxKeepAlive;

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
								Log.Write("debug", "Failed to load minimap with exception:");
								Log.Write("debug", e);
							}
						});
					}

					// Yuck... But this helps the UI Jank when opening the map selector significantly.
					Thread.Sleep(Environment.ProcessorCount == 1 ? 25 : 5);
				}
			}

			// Release the buffer by forcing changes to be written out to the texture, allowing the buffer to be reclaimed by GC.
			if (sheetBuilder.Current != null)
				Game.RunAfterTick(sheetBuilder.Current.ReleaseBuffer);

			Log.Write("debug", "MapCache.LoadAsyncInternal ended");
		}

		public string GetUpdatedMap(string uid)
		{
			if (uid == null)
				return null;

			while (this[uid].Status != MapStatus.Available)
			{
				if (mapUpdates.TryGetValue(uid, out var newUid))
					uid = newUid;
				else
					return null;
			}

			return uid;
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
			UpdateMaps();
			var map = string.IsNullOrEmpty(initialUid) ? null : previews[initialUid];
			if (map == null ||
				map.Status != MapStatus.Available ||
				!map.Visibility.HasFlag(MapVisibility.Lobby) ||
				(map.Class != MapClassification.System && map.Class != MapClassification.User))
			{
				var selected = previews.Values.Where(IsSuitableInitialMap).RandomOrDefault(random) ??
					previews.Values.FirstOrDefault(m =>
					m.Status == MapStatus.Available &&
					m.Visibility.HasFlag(MapVisibility.Lobby) &&
					(m.Class == MapClassification.System || m.Class == MapClassification.User));
				return selected == null ? string.Empty : selected.Uid;
			}

			return initialUid;
		}

		public MapPreview this[string key]
		{
			get
			{
				UpdateMaps();
				return previews[key];
			}
		}

		public IEnumerator<MapPreview> GetEnumerator()
		{
			UpdateMaps();
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

			foreach (var t in mapDirectoryTrackers)
				t.Dispose();

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
