#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRA.FileFormats
{
	public class R8Image
	{
		public readonly Size Size;
		public readonly int2 Offset;
		public readonly byte[] Image;

		// Legacy variable. Can be removed when the utility command is made sensible.
		public readonly Size FrameSize;

		public R8Image(Stream s)
		{
			// Scan forward until we find some data
			var type = s.ReadUInt8();
			while (type == 0)
				type = s.ReadUInt8();

			var width = s.ReadInt32();
			var height = s.ReadInt32();
			var x = s.ReadInt32();
			var y = s.ReadInt32();

			Size = new Size(width, height);
			Offset = new int2(width/2 - x, height/2 - y);

			/*var imageOffset = */s.ReadInt32();
			var paletteOffset = s.ReadInt32();
			var bpp = s.ReadUInt8();
			if (bpp != 8)
				throw new InvalidDataException("Error: {0} bits per pixel are not supported.".F(bpp));

			var frameHeight = s.ReadUInt8();
			var frameWidth = s.ReadUInt8();
			FrameSize = new Size(frameWidth, frameHeight);

			// Skip alignment byte
			s.ReadUInt8();

			// Ignore palette header
			if (type == 1 && paletteOffset != 0)
				s.Seek(8, SeekOrigin.Current);

			Image = s.ReadBytes(width*height);

			// Ignore palette data
			if (type == 1 && paletteOffset != 0)
				s.Seek(512, SeekOrigin.Current);
		}
	}

	public class R8Reader : IEnumerable<R8Image>
	{
		readonly List<R8Image> headers = new List<R8Image>();

		public readonly int Frames;
		public R8Reader(Stream stream)
		{
			while (stream.Position < stream.Length)
			{
				headers.Add(new R8Image(stream));
				Frames++;
			}
		}

		public R8Image this[int index]
		{
			get { return headers[index]; }
		}

		public IEnumerator<R8Image> GetEnumerator()
		{
			return headers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
