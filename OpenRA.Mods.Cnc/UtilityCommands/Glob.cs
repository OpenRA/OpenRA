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

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	public static class Glob
	{
		public static bool Enabled = true;

		static readonly char[] GlobChars = new char[] { '*', '?' };
		static readonly char[] DirectorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

		static bool NeedsExpansion(string filePath)
		{
			if (!Enabled)
				return false;

			return filePath.IndexOfAny(GlobChars) >= 0;
		}

		public static IEnumerable<string> Expand(string filePath)
		{
			if (!NeedsExpansion(filePath))
			{
				yield return filePath;
				yield break;
			}

			// Split using DirectorySeparators but keep the separators
			var parts = new List<string>();

			for (var startIndex = 0; startIndex < filePath.Length;)
			{
				var index = filePath.IndexOfAny(DirectorySeparators, startIndex);
				if (index == -1)
				{
					parts.Add(filePath.Substring(startIndex));
					break;
				}

				parts.Add(filePath.Substring(startIndex, index - startIndex + 1));
				startIndex = index + 1;
			}

			if (parts.Count > 0 && (parts[0] == "." || parts[0] == ".."))
				parts[0] += Path.DirectorySeparatorChar;

			// If it's empty
			// or if
			//    it's not rooted
			//    and it doesn't start with "./" or "../"
			// prepend "./"
			if (parts.Count == 0
				|| (parts[0][0] != Path.DirectorySeparatorChar
				&& parts[0][0] != Path.AltDirectorySeparatorChar
				&& parts[0].Contains(':') == false
				&& parts[0] != "." + Path.DirectorySeparatorChar
				&& parts[0] != "." + Path.AltDirectorySeparatorChar
				&& parts[0] != ".." + Path.DirectorySeparatorChar
				&& parts[0] != ".." + Path.AltDirectorySeparatorChar))
				parts.Insert(0, "." + Path.DirectorySeparatorChar);

			// If the last entry ends with a directory separator, append a '*'
			if (parts[parts.Count - 1][parts[parts.Count - 1].Length - 1] == Path.DirectorySeparatorChar
				|| parts[parts.Count - 1][parts[parts.Count - 1].Length - 1] == Path.AltDirectorySeparatorChar)
				parts.Add("*");

			var root = parts[0];
			var dirs = parts.Skip(1).Take(parts.Count - 2).ToList();
			var file = parts[parts.Count - 1];

			foreach (var path in Expand(root, dirs, 0, file))
				yield return path;
		}

		static IEnumerable<string> Expand(string basePath, IList<string> dirs, int dirIndex, string file)
		{
			if (dirIndex < dirs.Count)
			{
				var dir = dirs[dirIndex];

				if (!NeedsExpansion(dir))
				{
					var path = Path.Combine(basePath, dir);

					if (!Directory.Exists(path))
						yield break;
					else
					{
						foreach (var s in Expand(path, dirs, dirIndex + 1, file))
							yield return s;
					}
				}
				else
				{
					if (dir[dir.Length - 1] == Path.DirectorySeparatorChar || dir[dir.Length - 1] == Path.AltDirectorySeparatorChar)
						dir = dir.Substring(0, dir.Length - 1);
					foreach (var subDir in Directory.EnumerateDirectories(basePath, dir, SearchOption.TopDirectoryOnly))
						foreach (var s in Expand(subDir, dirs, dirIndex + 1, file))
							yield return s;
				}
			}
			else
			{
				if (!NeedsExpansion(file))
					yield return Path.Combine(basePath, file);
				else
				{
					foreach (var s in Directory.GetFiles(basePath, file, SearchOption.TopDirectoryOnly))
						yield return s;
				}
			}
		}
	}
}
