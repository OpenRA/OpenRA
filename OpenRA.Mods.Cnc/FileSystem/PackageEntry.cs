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
using System.Runtime.InteropServices;
using System.Text;
using OpenRA.Mods.Cnc.FileFormats;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public enum PackageHashType { Classic, CRC32 }

	public class PackageEntry
	{
		public const int Size = 12;
		public readonly uint Hash;
		public readonly uint Offset;
		public readonly uint Length;

		public PackageEntry(uint hash, uint offset, uint length)
		{
			Hash = hash;
			Offset = offset;
			Length = length;
		}

		public PackageEntry(Stream s)
		{
			Hash = s.ReadUInt32();
			Offset = s.ReadUInt32();
			Length = s.ReadUInt32();
		}

		public void Write(BinaryWriter w)
		{
			w.Write(Hash);
			w.Write(Offset);
			w.Write(Length);
		}

		public override string ToString()
		{
			if (Names.TryGetValue(Hash, out var filename))
				return $"{filename} - offset 0x{Offset:x8} - length 0x{Length:x8}";
			else
				return $"0x{Hash:x8} - offset 0x{Offset:x8} - length 0x{Length:x8}";
		}

		public static uint HashFilename(string name, PackageHashType type)
		{
			var padding = name.Length % 4 != 0 ? 4 - name.Length % 4 : 0;
			var paddedLength = name.Length + padding;

			// Avoid stack overflows by only allocating small buffers on the stack, and larger ones on the heap.
			// 64 chars covers most real filenames.
			var upperPaddedName = paddedLength < 64 ? stackalloc char[paddedLength] : new char[paddedLength];
			name.AsSpan().ToUpperInvariant(upperPaddedName);

			switch (type)
			{
				case PackageHashType.Classic:
					{
						for (var p = 0; p < padding; p++)
							upperPaddedName[paddedLength - 1 - p] = '\0';

						var asciiBytes = paddedLength < 64 ? stackalloc byte[paddedLength] : new byte[paddedLength];
						Encoding.ASCII.GetBytes(upperPaddedName, asciiBytes);

						var data = MemoryMarshal.Cast<byte, uint>(asciiBytes);
						var result = 0u;
						foreach (var next in data)
							result = ((result << 1) | (result >> 31)) + next;

						return result;
					}

				case PackageHashType.CRC32:
					{
						var length = name.Length;
						var lengthRoundedDownToFour = length / 4 * 4;
						if (length != lengthRoundedDownToFour)
						{
							upperPaddedName[length] = (char)(length - lengthRoundedDownToFour);
							for (var p = 1; p < padding; p++)
								upperPaddedName[length + p] = upperPaddedName[lengthRoundedDownToFour];
						}

						var asciiBytes = paddedLength < 64 ? stackalloc byte[paddedLength] : new byte[paddedLength];
						Encoding.ASCII.GetBytes(upperPaddedName, asciiBytes);

						return CRC32.Calculate(asciiBytes);
					}

				default: throw new NotImplementedException($"Unknown hash type `{type}`");
			}
		}

		static readonly Dictionary<uint, string> Names = new Dictionary<uint, string>();

		public static void AddStandardName(string s)
		{
			var hash = HashFilename(s, PackageHashType.Classic); // RA1 and TD
			Names.Add(hash, s);
			var crcHash = HashFilename(s, PackageHashType.CRC32); // TS
			Names.Add(crcHash, s);
		}
	}
}
