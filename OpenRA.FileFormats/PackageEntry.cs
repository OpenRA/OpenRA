#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenRA.FileFormats
{
	public class PackageEntry
	{
		public readonly uint Hash;
		public readonly uint Offset;
		public readonly uint Length;


		public PackageEntry(uint hash, uint offset, uint length)
		{
			Hash = hash;
			Offset = offset;
			Length = length;
		}

		public PackageEntry(BinaryReader r)
		{
			Hash = r.ReadUInt32();
			Offset = r.ReadUInt32();
			Length = r.ReadUInt32();
		}

		public void Write(BinaryWriter w)
		{
			w.Write(Hash);
			w.Write(Offset);
			w.Write(Length);
		}

		public override string ToString()
		{
			string filename;
			if (Names.TryGetValue(Hash, out filename))
				return "{0} - offset 0x{1:x8} - length 0x{2:x8}".F(filename, Offset, Length);
			else
				return "0x{0:x8} - offset 0x{1:x8} - length 0x{2:x8}".F(Hash, Offset, Length);
		}

		public static uint HashFilename(string name) // Red Alert 1 and Tiberian Dawn
		{
			name = name.ToUpperInvariant();
			if (name.Length % 4 != 0)
				name = name.PadRight(name.Length + (4 - name.Length % 4), '\0');

			MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(name));
			BinaryReader reader = new BinaryReader(ms);

			int len = name.Length >> 2;
			uint result = 0;

			while (len-- != 0)
				result = ((result << 1) | (result >> 31)) + reader.ReadUInt32();

			return result;
		}

		public static uint CrcHashFilename(string name) // Tiberian Sun
		{
			name = name.ToUpperInvariant();
			var l = name.Length;
			int a = l >> 2;
			if ((l & 3) != 0)
			{
				name += (char)(l - (a << 2));
				int i = 3 - (l & 3);
				while (i-- != 0)
					name += name[a << 2];
			}
			return CRC32.Calculate(Encoding.ASCII.GetBytes(name));
		}

		static Dictionary<uint, string> Names = new Dictionary<uint,string>();

		public static void AddStandardName(string s)
		{
			uint hash = HashFilename(s); // RA1 and TD
			Names.Add(hash, s);
			uint crcHash = CrcHashFilename(s); // TS
			Names.Add(crcHash, s);
		}

		public const int Size = 12;
	}
}
