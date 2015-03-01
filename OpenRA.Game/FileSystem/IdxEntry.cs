#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileSystem
{
	public class IdxEntry
	{
		public const string DefaultExtension = "wav";

		public readonly uint Hash;
		public readonly string Name;
		public readonly string Extension;
		public readonly uint Offset;
		public readonly uint Length;
		public readonly uint SampleRate;
		public readonly uint Flags;
		public readonly uint ChunkSize;

		public IdxEntry(uint hash, uint offset, uint length, uint sampleRate, uint flags, uint chuckSize)
		{
			Hash = hash;
			Offset = offset;
			Length = length;
			SampleRate = sampleRate;
			Flags = flags;
			ChunkSize = chuckSize;
		}

		public IdxEntry(Stream s)
		{
			var asciiname = s.ReadASCII(16);

			var pos = asciiname.IndexOf('\0');
			if (pos != 0)
				asciiname = asciiname.Substring(0, pos);

			Name = asciiname;
			Extension = DefaultExtension;
			Offset = s.ReadUInt32();
			Length = s.ReadUInt32();
			SampleRate = s.ReadUInt32();
			Flags = s.ReadUInt32();
			ChunkSize = s.ReadUInt32();
			Hash = HashFilename(string.Concat(Name, ".", Extension), PackageHashType.CRC32);
		}

		public void Write(BinaryWriter w)
		{
			w.Write(Name.PadRight(16, '\0'));
			w.Write(Offset);
			w.Write(Length);
			w.Write(SampleRate);
			w.Write(Flags);
			w.Write(ChunkSize);
		}

		public override string ToString()
		{
			string filename;
			if (names.TryGetValue(Hash, out filename))
				return "{0} - offset 0x{1:x8} - length 0x{2:x8}".F(filename, Offset, Length);
			else
				return "0x{0:x8} - offset 0x{1:x8} - length 0x{2:x8}".F(Hash, Offset, Length);
		}

		public static uint HashFilename(string name, PackageHashType type)
		{
			return PackageEntry.HashFilename(name, type);
		}

		static Dictionary<uint, string> names = new Dictionary<uint, string>();

		public static void AddStandardName(string s)
		{
			// RA1 and TD
			var hash = HashFilename(s, PackageHashType.Classic);
			names.Add(hash, s);

			// TS
			var crcHash = HashFilename(s, PackageHashType.CRC32);
			names.Add(crcHash, s);
		}
	}
}
