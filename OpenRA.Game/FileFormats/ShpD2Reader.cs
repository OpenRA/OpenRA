#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	public class ShpD2Reader : ISpriteSource
	{
		[Flags] enum FormatFlags : int
		{
			PaletteTable = 1,
			SkipFormat80 = 2,
			VariableLengthTable = 4
		}

		class Frame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get { return Size; } }
			public float2 Offset { get { return float2.Zero; } }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public Frame(Stream s)
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
				if ((flags & FormatFlags.SkipFormat80) == 0)
				{
					var temp = new byte[dataSize];
					Format80.DecodeInto(compressed, temp);
					compressed = temp;
				}

				Format2.DecodeInto(compressed, Data, 0);

				// Lookup values in lookup table
				for (var j = 0; j < Data.Length; j++)
					Data[j] = table[Data[j]];
			}
		}

		public IReadOnlyList<ISpriteFrame> Frames { get; private set; }

		public ShpD2Reader(Stream s)
		{
			var imageCount = s.ReadUInt16();

			// Last offset is pointer to end of file.
			var offsets = new uint[imageCount + 1];
			var temp = s.ReadUInt32();

			// If fourth byte in file is non-zero, the offsets are two bytes each.
			var twoByteOffset = (temp & 0xFF0000) > 0;
			s.Position = 2;

			for (var i = 0; i < imageCount + 1; i++)
				offsets[i] = (twoByteOffset ? s.ReadUInt16() : s.ReadUInt32()) + 2;

			var frames = new Frame[imageCount];
			Frames = frames.AsReadOnly();
			for (var i = 0; i < frames.Length; i++)
			{
				s.Position = offsets[i];
				frames[i] = new Frame(s);
			}
		}
	}
}
