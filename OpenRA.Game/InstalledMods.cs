#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public class InstalledMods : IReadOnlyDictionary<string, Manifest>
	{
		readonly Dictionary<string, Manifest> mods;
		readonly SheetBuilder sheetBuilder;

		readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
		public readonly IReadOnlyDictionary<string, Sprite> Icons;

		/// <summary>Initializes the collection of locally installed mods.</summary>
		/// <param name="searchPaths">Filesystem paths to search for mod packages.</param>
		/// <param name="explicitPaths">Filesystem paths to additional mod packages.</param>
		public InstalledMods(IEnumerable<string> searchPaths, IEnumerable<string> explicitPaths)
		{
			sheetBuilder = new SheetBuilder(SheetType.BGRA, 256);
			Icons = new ReadOnlyDictionary<string, Sprite>(icons);
			mods = GetInstalledMods(searchPaths, explicitPaths);
		}

		static IEnumerable<Pair<string, string>> GetCandidateMods(IEnumerable<string> searchPaths)
		{
			var mods = new List<Pair<string, string>>();
			foreach (var path in searchPaths)
			{
				try
				{
					var directory = new DirectoryInfo(Platform.ResolvePath(path));
					foreach (var subdir in directory.EnumerateDirectories())
						mods.Add(Pair.New(subdir.Name, subdir.FullName));

					foreach (var file in directory.EnumerateFiles("*.oramod"))
						mods.Add(Pair.New(Path.GetFileNameWithoutExtension(file.Name), file.FullName));
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed to enumerate mod search path {0}: {1}", path, e.Message);
				}
			}

			return mods;
		}

		Manifest LoadMod(string id, string path)
		{
			IReadOnlyPackage package = null;
			try
			{
				if (Directory.Exists(path))
					package = new Folder(path);
				else
				{
					try
					{
						using (var fileStream = File.OpenRead(path))
							package = new ZipFile(fileStream, path);
					}
					catch
					{
						throw new InvalidDataException(path + " is not a valid mod package");
					}
				}

				if (!package.Contains("mod.yaml"))
					throw new InvalidDataException(path + " is not a valid mod package");

				using (var stream = package.GetStream("icon.png"))
					if (stream != null)
						using (var bitmap = new Bitmap(stream))
							icons[id] = sheetBuilder.Add(bitmap);

				// Mods in the support directory and oramod packages (which are listed later
				// in the CandidateMods list) override mods in the main install.
				return new Manifest(id, package);
			}
			catch (Exception)
			{
				if (package != null)
					package.Dispose();

				return null;
			}
		}

		Dictionary<string, Manifest> GetInstalledMods(IEnumerable<string> searchPaths, IEnumerable<string> explicitPaths)
		{
			var ret = new Dictionary<string, Manifest>();
			var candidates = GetCandidateMods(searchPaths)
				.Concat(explicitPaths.Select(p => Pair.New(Path.GetFileNameWithoutExtension(p), p)));

			foreach (var pair in candidates)
			{
				var mod = LoadMod(pair.First, pair.Second);

				// Mods in the support directory and oramod packages (which are listed later
				// in the CandidateMods list) override mods in the main install.
				if (mod != null)
					ret[pair.First] = mod;
			}

			return ret;
		}

		public Manifest this[string key] { get { return mods[key]; } }
		public int Count { get { return mods.Count; } }
		public ICollection<string> Keys { get { return mods.Keys; } }
		public ICollection<Manifest> Values { get { return mods.Values; } }
		public bool ContainsKey(string key) { return mods.ContainsKey(key); }
		public IEnumerator<KeyValuePair<string, Manifest>> GetEnumerator() { return mods.GetEnumerator(); }
		public bool TryGetValue(string key, out Manifest value) { return mods.TryGetValue(key, out value); }
		IEnumerator IEnumerable.GetEnumerator() { return mods.GetEnumerator(); }
	}
}