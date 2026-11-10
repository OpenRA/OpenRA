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
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileSystem;

namespace OpenRA
{
	public sealed class MapDirectoryTracker : IDisposable
	{
		readonly Lock syncObject = new();
		readonly FileSystemWatcher watcher;
		readonly IReadOnlyPackage package;
		readonly MapClassification classification;

		enum MapAction { Add, Delete, Update }
		readonly Dictionary<string, MapAction> mapActionQueue = [];

		bool dirty = false;
		Dictionary<string, (int FileCount, long LastWriteTicks)> watchedFoldersSnapshot;

		public MapDirectoryTracker(IReadOnlyPackage package, MapClassification classification)
		{
			this.package = package;
			this.classification = classification;

			watcher = new FileSystemWatcher(package.Name);
			watcher.Changed += (_, e) => AddMapAction(MapAction.Update, e.FullPath);
			watcher.Created += (_, e) => AddMapAction(MapAction.Add, e.FullPath);
			watcher.Deleted += (_, e) => AddMapAction(MapAction.Delete, e.FullPath);
			watcher.Renamed += (_, e) => AddMapAction(MapAction.Add, e.FullPath, e.OldFullPath);

			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;
		}

		public void Dispose()
		{
			watcher.Dispose();
		}

		/// <summary>
		/// Enables or disables raising file system events.
		/// Disabling takes a snapshot of the watched map folders so that changes
		/// made while disabled can be queued when tracking is re-enabled.
		/// </summary>
		public void SetEnabled(bool enabled)
		{
			lock (syncObject)
			{
				if (enabled == watcher.EnableRaisingEvents)
					return;

				watcher.EnableRaisingEvents = enabled;

				if (!Directory.Exists(package.Name))
					return;

				if (!enabled)
				{
					watchedFoldersSnapshot = SnapshotMapFolders();
					return;
				}

				// Queue the folders that appeared, disappeared or changed while
				// disabled so that the next UpdateMaps() call brings the cache
				// back in sync.
				if (watchedFoldersSnapshot != null)
				{
					var current = SnapshotMapFolders();
					foreach (var (folder, fingerprint) in current)
					{
						if (!watchedFoldersSnapshot.TryGetValue(folder, out var old))
							AddMapAction(MapAction.Add, folder);
						else if (old != fingerprint)
							AddMapAction(MapAction.Update, folder);
					}

					foreach (var folder in watchedFoldersSnapshot.Keys)
						if (!current.ContainsKey(folder))
							AddMapAction(MapAction.Delete, folder);
				}

				watchedFoldersSnapshot = null;
			}
		}

		Dictionary<string, (int FileCount, long LastWriteTicks)> SnapshotMapFolders()
		{
			var snapshot = new Dictionary<string, (int, long)>();
			foreach (var folder in Directory.EnumerateDirectories(package.Name))
			{
				var fileCount = 0;
				var lastWriteTicks = 0L;
				foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
				{
					fileCount++;
					lastWriteTicks = Math.Max(lastWriteTicks, File.GetLastWriteTimeUtc(file).Ticks);
				}

				snapshot[folder] = (fileCount, lastWriteTicks);
			}

			return snapshot;
		}

		void AddMapAction(MapAction mapAction, string fullpath, string oldFullPath = null)
		{
			lock (syncObject)
			{
				dirty = true;

				// if path is not root, update map instead
				var path = RemoveSubDirs(fullpath);
				if (fullpath == path)
					mapActionQueue[path] = mapAction;
				else
					mapActionQueue[path] = MapAction.Update;

				// called when file has been renamed / changed location
				if (oldFullPath != null)
				{
					var oldpath = RemoveSubDirs(oldFullPath);
					if (oldpath != null)
						if (oldFullPath == oldpath)
							mapActionQueue[oldpath] = MapAction.Delete;
						else
							mapActionQueue[oldpath] = MapAction.Update;
				}
			}
		}

		public void UpdateMaps(MapCache mapcache)
		{
			lock (syncObject)
			{
				if (!dirty)
					return;

				dirty = false;
				foreach (var mapAction in mapActionQueue)
				{
					var map = mapcache.FirstOrDefault(x => x.Path == mapAction.Key && x.Status == MapStatus.Available);
					if (map != null)
					{
						if (mapAction.Value == MapAction.Delete)
						{
							Console.WriteLine(mapAction.Key + " was deleted");
							map.Invalidate();
						}
						else
						{
							Console.WriteLine(mapAction.Key + " was updated");
							map.Invalidate();
							mapcache.LoadMap(mapAction.Key.Replace(package.Name + Path.DirectorySeparatorChar, ""), package, classification, map.Uid);
						}
					}
					else
					{
						if (mapAction.Value != MapAction.Delete)
						{
							Console.WriteLine(mapAction.Key + " was added");
							mapcache.LoadMap(mapAction.Key.Replace(package?.Name + Path.DirectorySeparatorChar, ""), package, classification, null);
						}
					}
				}

				mapActionQueue.Clear();
			}
		}

		string RemoveSubDirs(string path)
		{
			var endPath = path.Replace(package.Name + Path.DirectorySeparatorChar, "");

			// if file moved from out outside directory, ignore it
			if (path == endPath)
				return null;

			return package.Name + Path.DirectorySeparatorChar + endPath.Split(Path.DirectorySeparatorChar)[0];
		}
	}
}
