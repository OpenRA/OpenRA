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
using System.Drawing;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class TSImageHeader
	{
		public readonly Size Size;
		public readonly float2 Offset;

		public readonly uint FileOffset;
		public readonly byte Format;

		public byte[] Image;

		public TSImageHeader(Stream stream, Size frameSize)
		{
			var x = stream.ReadUInt16();
			var y = stream.ReadUInt16();
			var width = stream.ReadUInt16();
			var height = stream.ReadUInt16();

			Offset = new float2(x + 0.5f * (width - frameSize.Width), y + 0.5f * (height - frameSize.Height));
			Size = new Size(width, height);

			Format = stream.ReadUInt8();
			stream.Position += 11;
			FileOffset = stream.ReadUInt32();
		}
	}

	public class ShpTSReader
	{
		public readonly int ImageCount;
		public readonly Size Size;

		readonly List<TSImageHeader> frames = new List<TSImageHeader>();
		public IEnumerable<TSImageHeader> Frames { get { return frames; } }

		public ShpTSReader(Stream stream)
		{
			stream.ReadUInt16();
			var width = stream.ReadUInt16();
			var height = stream.ReadUInt16();
			Size = new Size(width, height);
			ImageCount = stream.ReadUInt16();

			for (var i = 0; i < ImageCount; i++)
				frames.Add(new TSImageHeader(stream, Size));

			for (var i = 0; i < ImageCount; i++)
			{
				var f = frames[i];
				if (f.FileOffset == 0)
					continue;

				stream.Position = f.FileOffset;

				// Uncompressed
				if (f.Format == 1 || f.Format == 0)
					f.Image = stream.ReadBytes(f.Size.Width * f.Size.Height);

				// Uncompressed scanlines
				else if (f.Format == 2)
				{
					f.Image = new byte[f.Size.Width * f.Size.Height];
					for (var j = 0; j < f.Size.Height; j++)
					{
						var length = stream.ReadUInt16() - 2;
						stream.Read(f.Image, f.Size.Width * j, length);
					}
				}

				// RLE-zero compressed scanlines
				else if (f.Format == 3)
				{
					f.Image = new byte[f.Size.Width * f.Size.Height];

					for (var j = 0; j < f.Size.Height; j++)
					{
						var k = j * f.Size.Width;
						var length = stream.ReadUInt16() - 2;
						while (length > 0)
						{
							var b = stream.ReadUInt8();
							length--;

							if (b == 0)
							{
								k += stream.ReadUInt8();
								length--;
							}
							else
								f.Image[k++] = b;
						}
					}
				}
			}
		}
	}
}