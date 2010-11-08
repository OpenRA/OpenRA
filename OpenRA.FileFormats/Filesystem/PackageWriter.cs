#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public static class PackageWriter
	{
		public static void CreateMix(string filename, List<string> contents)
		{
			// Construct a list of entries for the file header
			uint dataSize = 0;
			var items = new List<PackageEntry>();
			foreach (var file in contents)
			{
				uint length = (uint)new FileInfo(file).Length;
				uint hash = PackageEntry.HashFilename(Path.GetFileName(file));
				items.Add(new PackageEntry(hash, dataSize, length));
				dataSize += length;
			}

			using (var s = File.Create(filename))
			using (var writer = new BinaryWriter(s))
			{
				// Write file header
				writer.Write((ushort)items.Count);
				writer.Write(dataSize);
				foreach (var item in items)
					item.Write(writer);

				writer.Flush();

				// Copy file data
				foreach (var file in contents)
					s.Write(File.ReadAllBytes(file));
			}
		}
	}
}
