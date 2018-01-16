#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;

namespace fixheader
{
	class fixheader
	{
		static byte[] data;
		static int peOffset;

		static void Main(string[] args)
		{
			Console.WriteLine("fixheader {0}", args[0]);
			data = File.ReadAllBytes(args[0]);
			peOffset = BitConverter.ToInt32(data, 0x3c);
			var corHeaderRva = BitConverter.ToInt32(data, peOffset + 20 + 100 + 14 * 8);
			var corHeaderOffset = RvaToOffset(corHeaderRva);

			data[corHeaderOffset + 16] |= 2;

			// Set Flag "Application can handle large (>2GB) addresses (/LARGEADDRESSAWARE)"
			data[peOffset + 4 + 18] |= 0x20;

			File.WriteAllBytes(args[0], data);
		}

		static int RvaToOffset(int va)
		{
			var numSections = BitConverter.ToInt16(data, peOffset + 6);
			var numDataDirectories = BitConverter.ToInt32(data, peOffset + 24 + 92);
			var sectionTableStart = peOffset + 24 + 96 + 8 * numDataDirectories;

			for (var i = 0; i < numSections; i++)
			{
				var virtualSize = BitConverter.ToInt32(data, sectionTableStart + 40 * i + 8);
				var virtualAddr = BitConverter.ToInt32(data, sectionTableStart + 40 * i + 12);
				var fileOffset = BitConverter.ToInt32(data, sectionTableStart + 40 * i + 20);

				if (va >= virtualAddr && va < virtualAddr + virtualSize)
					return va - virtualAddr + fileOffset;
			}

			return 0;
		}
	}
}
