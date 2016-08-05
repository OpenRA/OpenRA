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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA
{
	public class InstalledMods : IReadOnlyDictionary<string, Manifest>
	{
		readonly Dictionary<string, Manifest> mods;

		public InstalledMods(string customModPath)
		{
			mods = GetInstalledMods(customModPath);
		}

		static IEnumerable<Pair<string, string>> GetCandidateMods()
		{
			// Get mods that are in the game folder.
			var basePath = Platform.ResolvePath(Path.Combine(".", "mods"));
			var mods = Directory.GetDirectories(basePath)
				.Select(x => Pair.New(x.Substring(basePath.Length + 1), x))
				.ToList();

			foreach (var m in Directory.GetFiles(basePath, "*.oramod"))
				mods.Add(Pair.New(Path.GetFileNameWithoutExtension(m), m));

			// Get mods that are in the support folder.
			var supportPath = Platform.ResolvePath(Path.Combine("^", "mods"));
			if (!Directory.Exists(supportPath))
				return mods;

			foreach (var pair in Directory.GetDirectories(supportPath).ToDictionary(x => x.Substring(supportPath.Length + 1)))
				mods.Add(Pair.New(pair.Key, pair.Value));

			foreach (var m in Directory.GetFiles(supportPath, "*.oramod"))
				mods.Add(Pair.New(Path.GetFileNameWithoutExtension(m), m));

			return mods;
		}

		static Manifest LoadMod(string id, string path)
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

		static Dictionary<string, Manifest> GetInstalledMods(string customModPath)
		{
			var ret = new Dictionary<string, Manifest>();
			var candidates = GetCandidateMods();
			if (customModPath != null)
				candidates = candidates.Append(Pair.New(Path.GetFileNameWithoutExtension(customModPath), customModPath));

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