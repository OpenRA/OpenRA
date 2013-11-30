#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	enum Dune2ImageFlags : int
	{
		F80_F2 = 0,
		F2 = 2,
		L16_F80_F2_1 = 1,
		L16_F80_F2_2 = 3,
		Ln_F80_F2 = 5
	}

	class Frame : ISpriteFrame
	{
		public Size Size { get; private set; }
		public Size FrameSize { get { return Size; } }
		public float2 Offset { get { return float2.Zero; } }
		public byte[] Data { get; set; }

		public Frame(Stream s)
		{
			var flags = (Dune2ImageFlags)s.ReadUInt16();
			s.Position += 1;
			var width = s.ReadUInt16();
			var height = s.ReadUInt8();
			Size = new Size(width, height);

			var frameSize = s.ReadUInt16();
			var dataSize = s.ReadUInt16();

			byte[] table;
			if (flags == Dune2ImageFlags.L16_F80_F2_1 ||
				flags == Dune2ImageFlags.L16_F80_F2_2 ||
				flags == Dune2ImageFlags.Ln_F80_F2)
			{
				var n = flags == Dune2ImageFlags.Ln_F80_F2 ? s.ReadUInt8() : (byte)16;
				table = new byte[n];
				for (var i = 0; i < n; i++)
					table[i] = s.ReadUInt8();
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

			// Subtract header size
			var imgData = s.ReadBytes(frameSize - 10);
			Data = new byte[width * height];

			// Decode image data
			if (flags != Dune2ImageFlags.F2)
			{
				var tempData = new byte[dataSize];
				Format80.DecodeInto(imgData, tempData);
				Format2.DecodeInto(tempData, Data);
			}
			else
				Format2.DecodeInto(imgData, Data);

			// Lookup values in lookup table
			for (var j = 0; j < Data.Length; j++)
				Data[j] = table[Data[j]];
		}
	}

	public class ShpD2Reader : ISpriteSource
	{
		List<Frame> headers = new List<Frame>();
		public IEnumerable<ISpriteFrame> Frames { get { return headers.Cast<ISpriteFrame>(); } }

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

			for (var i = 0; i < imageCount; i++)
			{
				s.Position = offsets[i];
				headers.Add(new Frame(s));
			}
		}
	}
}
