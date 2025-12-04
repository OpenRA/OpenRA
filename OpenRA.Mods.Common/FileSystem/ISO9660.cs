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
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.FileSystem
{
	public class ISO9660Loader : IPackageLoader
	{
		public sealed class ISO9660Package : IReadOnlyPackage
		{
			public readonly record struct Entry(uint Offset, uint Length);

			public string Name { get; }
			public string VolumeName { get; }

			public IEnumerable<string> Contents => index.Keys;

			readonly Dictionary<string, Entry> index = [];
			readonly Stream s;

			readonly record struct DirectoryRecord(string Name, uint Offset, uint Length, bool IsDirectory);
			static bool TryReadDirectoryRecord(Stream s, out DirectoryRecord record, bool isJoliet = false)
			{
				var start = s.Position;
				var recordLength = s.ReadUInt8();
				if (recordLength == 0)
				{
					s.Position = start;
					record = default;
					return false;
				}

				s.Position++;
				var location = s.ReadUInt32();
				s.Position += 4;
				var length = s.ReadUInt32();
				s.Position += 11;
				var flags = s.ReadUInt8();
				s.Position += 6;

				var identifierLength = s.ReadUInt8();
				var buffer = identifierLength < 128 ? stackalloc byte[identifierLength] : new byte[identifierLength];
				s.ReadBytes(buffer);
				var identifier = (isJoliet ? Encoding.BigEndianUnicode : Encoding.ASCII).GetString(buffer);

				s.Position = start + recordLength;

				record = new DirectoryRecord(identifier.Split(';')[0], 2048 * location, length, (flags & 2) != 0);
				return true;
			}

			void EnumerateDirectories(DirectoryRecord parent, string path = null, bool isJoliet = false)
			{
				var pos = s.Position;
				s.Position = parent.Offset;

				// Skip . and .. records
				TryReadDirectoryRecord(s, out _);
				TryReadDirectoryRecord(s, out _);
				while (s.Position < parent.Offset + parent.Length)
				{
					if (!TryReadDirectoryRecord(s, out var child, isJoliet))
						break;

					var childPath = path != null ? $"{path}/{child.Name}" : child.Name;
					if (child.IsDirectory)
						EnumerateDirectories(child, childPath, isJoliet);
					else
						index[childPath] = new Entry(child.Offset, child.Length);
				}

				s.Position = pos;
			}

			public ISO9660Package(Stream s, string filename)
			{
				Name = filename;
				this.s = s;

				try
				{
					var complete = false;

					// Skip system area
					s.Position = 32768;

					// Parse volume descriptors
					while (!complete && s.Position < s.Length)
					{
						var start = s.Position;

						var vdType = s.ReadUInt8();
						var vdIdentifier = s.ReadASCII(5);
						if (vdIdentifier != "CD001")
							throw new InvalidDataException("Invalid volume descriptor");

						switch (vdType)
						{
							// Terminator
							case 0xFF:
								complete = true;
								break;

							// Primary volume descriptor
							case 0x01:
							{
								s.Position = start + 40;
								VolumeName = s.ReadASCII(32).Trim();

								s.Position = start + 156;
								TryReadDirectoryRecord(s, out var root);
								EnumerateDirectories(root);
								break;
							}

							// Supplementary volume descriptor
							case 0x02:
							{
								s.Position = start + 7;
								var volumeFlags = s.ReadUInt8();
								s.Position = start + 88;
								var escape = s.ReadASCII(32).Trim('\x00');

								// Joliet extension
								if (volumeFlags == 0 && escape is "%/@" or "%/C" or "%/E")
								{
									s.Position = start + 156;
									TryReadDirectoryRecord(s, out var root, isJoliet: true);
									EnumerateDirectories(root, isJoliet: true);
								}

								break;
							}
						}

						s.Position = start + 2048;
					}

					index.TrimExcess();
				}
				catch
				{
					Dispose();
					throw;
				}
			}

			public Stream GetStream(string filename)
			{
				if (!index.TryGetValue(filename, out var entry))
					return null;

				return SegmentStream.CreateWithoutOwningStream(s, entry.Offset, (int)entry.Length);
			}

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				var childStream = GetStream(filename);
				if (childStream == null)
					return null;

				if (context.TryParsePackage(childStream, filename, out var package))
					return package;

				childStream.Dispose();
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
			if (s.Length < 34816)
			{
				package = null;
				return false;
			}

			// Check the volume descriptor
			var pos = s.Position;
			s.Position = 32769;
			var identifier = s.ReadASCII(5);
			s.Position = pos;

			if (identifier != "CD001")
			{
				package = null;
				return false;
			}

			package = new ISO9660Package(s, filename);
			return true;
		}
	}
}
