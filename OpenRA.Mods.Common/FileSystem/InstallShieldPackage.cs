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
using System.Collections.ObjectModel;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.FileFormats;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.FileSystem
{
	public class InstallShieldLoader : IPackageLoader
	{
		public sealed class InstallShieldPackage : IReadOnlyPackage
		{
			public readonly struct Entry
			{
				public readonly uint Offset;
				public readonly uint Length;

				public Entry(uint offset, uint length)
				{
					Offset = offset;
					Length = length;
				}
			}

			public string Name { get; }
			public IEnumerable<string> Contents => index.Keys;

			readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
			readonly Stream s;
			readonly long dataStart = 255;

			public InstallShieldPackage(Stream s, string filename)
			{
				Name = filename;
				this.s = s;

				try
				{
					// Parse package header
					/*var signature = */s.ReadUInt32();
					s.Position += 8;
					/*var FileCount = */s.ReadUInt16();
					s.Position += 4;
					/*var ArchiveSize = */s.ReadUInt32();
					s.Position += 19;
					var tocAddress = s.ReadInt32();
					s.Position += 4;
					var dirCount = s.ReadUInt16();

					// Parse the directory list
					s.Position = tocAddress;

					// Parse directories
					var directories = new Dictionary<string, uint>();
					for (var i = 0; i < dirCount; i++)
					{
						// Parse directory header
						var fileCount = s.ReadUInt16();
						var chunkSize = s.ReadUInt16();
						var nameLength = s.ReadUInt16();
						var dirName = s.ReadASCII(nameLength);

						// Skip to the end of the chunk
						s.ReadBytes(chunkSize - nameLength - 6);
						directories.Add(dirName, fileCount);
					}

					// Parse files
					foreach (var dir in directories)
						for (var i = 0; i < dir.Value; i++)
							ParseFile(dir.Key);
				}
				catch
				{
					Dispose();
					throw;
				}
			}

			uint accumulatedData = 0;
			void ParseFile(string dirName)
			{
				s.Position += 7;
				var compressedSize = s.ReadUInt32();
				s.Position += 12;
				var chunkSize = s.ReadUInt16();
				s.Position += 4;
				var nameLength = s.ReadByte();
				var fileName = dirName + "\\" + s.ReadASCII(nameLength);

				// Use index syntax to overwrite any duplicate entries with the last value
				index[fileName] = new Entry(accumulatedData, compressedSize);
				accumulatedData += compressedSize;

				// Skip to the end of the chunk
				s.Position += chunkSize - nameLength - 30;
			}

			public Stream GetStream(string filename)
			{
				if (!index.TryGetValue(filename, out var e))
					return null;

				s.Seek(dataStart + e.Offset, SeekOrigin.Begin);

				var ret = new MemoryStream();
				Blast.Decompress(s, ret);
				ret.Seek(0, SeekOrigin.Begin);

				return ret;
			}

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				// Not implemented
				return null;
			}

			public bool Contains(string filename)
			{
				return index.ContainsKey(filename);
			}

			public IReadOnlyDictionary<string, Entry> Index => new ReadOnlyDictionary<string, Entry>(index);

			public void Dispose()
			{
				s.Dispose();
			}
		}

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			// Take a peek at the file signature
			var signature = s.ReadUInt32();
			s.Position -= 4;

			if (signature != 0x8C655D13)
			{
				package = null;
				return false;
			}

			package = new InstallShieldPackage(s, filename);
			return true;
		}
	}
}
