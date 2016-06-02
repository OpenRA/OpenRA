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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using SZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace OpenRA.FileSystem
{
	public sealed class ZipFolder : IReadOnlyPackage
	{
		public string Name { get; private set; }
		public ZipFile Parent { get; private set; }
		readonly string path;

		static ZipFolder()
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
		}

		public ZipFolder(FileSystem context, ZipFile parent, string path, string filename)
		{
			if (filename.EndsWith("/"))
				filename = filename.Substring(0, filename.Length - 1);

			Name = filename;
			Parent = parent;
			if (path.EndsWith("/"))
				path = path.Substring(0, path.Length - 1);

			this.path = path;
		}

		public Stream GetStream(string filename)
		{
			// Zip files use '/' as a path separator
			return Parent.GetStream(path + '/' + filename);
		}

		public IEnumerable<string> Contents
		{
			get
			{
				foreach (var entry in Parent.Contents)
				{
					if (entry.StartsWith(path) && entry != path)
					{
						var filename = entry.Substring(path.Length + 1);
						var dirLevels = filename.Split('/').Count(c => !string.IsNullOrEmpty(c));
						if (dirLevels == 1)
							yield return filename;
					}
				}
			}
		}

		public bool Contains(string filename)
		{
			return Parent.Contains(path + '/' + filename);
		}

		public void Dispose() { /* nothing to do */ }
	}
}
