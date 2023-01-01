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
using OpenRA.FileSystem;

namespace OpenRA
{
	public sealed class MapDirectoryTracker : IDisposable
	{
		readonly FileSystemWatcher watcher;
		readonly MapGrid mapGrid;
		readonly IReadOnlyPackage package;
		readonly MapClassification classification;

		enum MapAction { Add, Delete, Update }
		readonly Dictionary<string, MapAction> mapActionQueue = new Dictionary<string, MapAction>();

		bool dirty = false;

		public MapDirectoryTracker(MapGrid mapGrid, IReadOnlyPackage package, MapClassification classification)
		{
			this.mapGrid = mapGrid;
			this.package = package;
			this.classification = classification;

			watcher = new FileSystemWatcher(package.Name);
			watcher.Changed += (object sender, FileSystemEventArgs e) => AddMapAction(MapAction.Update, e.FullPath);
			watcher.Created += (object sender, FileSystemEventArgs e) => AddMapAction(MapAction.Add, e.FullPath);
			watcher.Deleted += (object sender, FileSystemEventArgs e) => AddMapAction(MapAction.Delete, e.FullPath);
			watcher.Renamed += (object sender, RenamedEventArgs e) => AddMapAction(MapAction.Add, e.FullPath, e.OldFullPath);

			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;
		}

		public void Dispose()
		{
			watcher.Dispose();
		}

		void AddMapAction(MapAction mapAction, string fullpath, string oldFullPath = null)
		{
			lock (mapActionQueue)
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
			lock (mapActionQueue)
			{
				if (!dirty)
					return;

				dirty = false;
				foreach (var mapAction in mapActionQueue)
				{
					var map = mapcache.FirstOrDefault(x => x.Package?.Name == mapAction.Key && x.Status == MapStatus.Available);
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
							mapcache.LoadMap(mapAction.Key.Replace(package.Name + Path.DirectorySeparatorChar, ""), package, classification, mapGrid, map.Uid);
						}
					}
					else
					{
						if (mapAction.Value != MapAction.Delete)
						{
							Console.WriteLine(mapAction.Key + " was added");
							mapcache.LoadMap(mapAction.Key.Replace(package?.Name + Path.DirectorySeparatorChar, ""), package, classification, mapGrid, null);
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
