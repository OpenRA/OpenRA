#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.FileSystem
{
	/// <summary>
	/// This class supports loading unencrypted V3 .meg files using
	/// reference documentation from here https://modtools.petrolution.net/docs/MegFileFormat
	/// </summary>
	public class MegV3Loader : IPackageLoader
	{
		const uint UnencryptedMegID = 0xFFFFFFFF;

		// Float value 0.99, but it is simpler to read and compare as an integer
		const uint MegVersion = 0x3F7D70A4;

		public bool TryParsePackage(Stream s, string filename, OpenRA.FileSystem.FileSystem context, out IReadOnlyPackage package)
		{
			var position = s.Position;

			var id = s.ReadUInt32();
			var version = s.ReadUInt32();

			s.Position = position;

			if (id != UnencryptedMegID || version != MegVersion)
			{
				package = null;
				return false;
			}

			package = new MegFile(s, filename);
			return true;
		}

		public sealed class MegFile : IReadOnlyPackage
		{
			readonly Stream s;

			readonly Dictionary<string, (uint Offset, int Length)> contents = new Dictionary<string, (uint Offset, int Length)>();

			public MegFile(Stream s, string filename)
			{
				Name = filename;
				this.s = s;

				var id = s.ReadUInt32();
				var version = s.ReadUInt32();

				if (id != UnencryptedMegID || version != MegVersion)
					throw new Exception("Invalid file signature for meg file");

				var headerSize = s.ReadUInt32();
				var numStrings = s.ReadUInt32();
				var numFiles = s.ReadUInt32();
				var stringsSize = s.ReadUInt32();
				var stringsStart = s.Position;

				var filenames = new List<string>();

				// The file names are an indexed array of strings
				for (var i = 0; i < numStrings; i++)
				{
					var length = s.ReadUInt16();
					filenames.Add(s.ReadASCII(length));
				}

				// The header indicates where we should be, so verify it
				if (s.Position != stringsSize + stringsStart)
					throw new Exception("File name table in .meg file inconsistent");

				// Now we load each file entry and associated info
				for (var i = 0; i < numFiles; i++)
				{
					// Ignore flags, crc, index
					s.Position += 10;
					var size = s.ReadUInt32();
					var offset = s.ReadUInt32();
					var nameIndex = s.ReadUInt16();
					contents[filenames[nameIndex]] = (offset, (int)size);
				}

				if (s.Position != headerSize)
					throw new Exception("Expected to be at data start offset");
			}

			public string Name { get; }

			public IEnumerable<string> Contents => contents.Keys;

			public bool Contains(string filename)
			{
				return contents.ContainsKey(filename);
			}

			public void Dispose()
			{
				s.Dispose();
			}

			public Stream GetStream(string filename)
			{
				// Look up the index of the filename
				if (!contents.TryGetValue(filename, out var index))
					return null;

				return SegmentStream.CreateWithoutOwningStream(s, index.Offset, index.Length);
			}

			public IReadOnlyPackage OpenPackage(string filename, OpenRA.FileSystem.FileSystem context)
			{
				var childStream = GetStream(filename);
				if (childStream == null)
					return null;

				if (context.TryParsePackage(childStream, filename, out var package))
					return package;

				childStream.Dispose();
				return null;
			}
		}
	}
}
