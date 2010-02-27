#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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

		public PackageEntry(BinaryReader r)
		{
			Hash = r.ReadUInt32();
			Offset = r.ReadUInt32();
			Length = r.ReadUInt32();
		}

		public override string ToString()
		{
			string filename;
			if (Names.TryGetValue(Hash, out filename))
				return string.Format("{0} - offset 0x{1:x8} - length 0x{2:x8}", filename, Offset, Length);
			else
				return string.Format("0x{0:x8} - offset 0x{1:x8} - length 0x{2:x8}", Hash, Offset, Length);
		}

		public static uint HashFilename(string name)
		{
			if (name.Length > 12)
				name = name.Substring(0, 12);

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

		static Dictionary<uint, string> Names = new Dictionary<uint,string>();
		
		public static void AddStandardName(string s)
		{
			uint hash = HashFilename(s);
			Names.Add(hash, s);
		}

		public const int Size = 12;
	}
}
