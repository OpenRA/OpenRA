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
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.SpriteLoaders
{
	public class ShpD2Loader : ISpriteLoader
	{
		[Flags]
		enum FormatFlags : int
		{
			PaletteTable = 1,
			NotLCWCompressed = 2,
			VariableLengthTable = 4
		}

		class ShpD2Frame : ISpriteFrame
		{
			public SpriteFrameType Type { get { return SpriteFrameType.Indexed8; } }
			public Size Size { get; private set; }
			public Size FrameSize { get { return Size; } }
			public float2 Offset { get { return float2.Zero; } }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public ShpD2Frame(Stream s)
			{
				var flags = (FormatFlags)s.ReadUInt16();
				s.Position += 1;
				var width = s.ReadUInt16();
				var height = s.ReadUInt8();
				Size = new Size(width, height);

				// Subtract header size
				var dataLeft = s.ReadUInt16() - 10;
				var dataSize = s.ReadUInt16();

				byte[] table;
				if ((flags & FormatFlags.PaletteTable) != 0)
				{
					var n = (flags & FormatFlags.VariableLengthTable) != 0 ? s.ReadUInt8() : (byte)16;
					table = new byte[n];
					for (var i = 0; i < n; i++)
						table[i] = s.ReadUInt8();

					dataLeft -= n;
				}
				else
				{
					table = new byte[256];
					for (var i = 0; i < 256; i++)
						table[i] = (byte)i;
					table[1] = 0x7f;
					table[2] = 0x7e;
					table[3] = 0x7d;
					table[4] = 0x7c;
				}

				Data = new byte[width * height];

				// Decode image data
				var compressed = s.ReadBytes(dataLeft);
				if ((flags & FormatFlags.NotLCWCompressed) == 0)
				{
					var temp = new byte[dataSize];
					LCWCompression.DecodeInto(compressed, temp);
					compressed = temp;
				}

				RLEZerosCompression.DecodeInto(compressed, Data, 0);

				// Lookup values in lookup table
				for (var j = 0; j < Data.Length; j++)
					Data[j] = table[Data[j]];
			}
		}

		bool IsShpD2(Stream s)
		{
			var start = s.Position;

			// First word is the image count
			var imageCount = s.ReadUInt16();
			if (imageCount == 0)
			{
				s.Position = start;
				return false;
			}

			// Test for two vs four byte offset
			var testOffset = s.ReadUInt32();
			var offsetSize = (testOffset & 0xFF0000) > 0 ? 2 : 4;

			// Last offset should point to the end of file
			var finalOffset = start + 2 + offsetSize * imageCount;
			if (finalOffset > s.Length)
			{
				s.Position = start;
				return false;
			}

			s.Position = finalOffset;
			var eof = offsetSize == 2 ? s.ReadUInt16() : s.ReadUInt32();
			if (eof + 2 != s.Length)
			{
				s.Position = start;
				return false;
			}

			// Check the format flag on the first frame
			var b = s.ReadUInt16();
			s.Position = start;
			return b == 5 || b <= 3;
		}

		ShpD2Frame[] ParseFrames(Stream s)
		{
			var start = s.Position;

			var imageCount = s.ReadUInt16();

			// Last offset is pointer to end of file.
			var offsets = new uint[imageCount + 1];
			var temp = s.ReadUInt32();

			// If fourth byte in file is non-zero, the offsets are two bytes each.
			var twoByteOffset = (temp & 0xFF0000) > 0;
			s.Position = 2;

			for (var i = 0; i < imageCount + 1; i++)
				offsets[i] = (twoByteOffset ? s.ReadUInt16() : s.ReadUInt32()) + 2;

			var frames = new ShpD2Frame[imageCount];
			for (var i = 0; i < frames.Length; i++)
			{
				s.Position = offsets[i];
				frames[i] = new ShpD2Frame(s);
			}

			s.Position = start;
			return frames;
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsShpD2(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}
