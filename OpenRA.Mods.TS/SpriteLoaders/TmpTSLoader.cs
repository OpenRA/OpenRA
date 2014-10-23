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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.TS.SpriteLoaders
{
	public class TmpTSLoader : ISpriteLoader
	{
		class TmpTSFrame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get { return Size; } }
			public float2 Offset { get; private set; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public TmpTSFrame(Stream s, Size size, int u, int v)
			{
				if (s.Position != 0)
				{
					Size = size;

					// Skip unnecessary header data
					s.Position += 20;

					// Extra data is specified relative to the top-left of the template
					var extraX = s.ReadInt32() - (u - v) * size.Width / 2;
					var extraY = s.ReadInt32() - (u + v) * size.Height / 2;
					var extraWidth = s.ReadInt32();
					var extraHeight = s.ReadInt32();
					var flags = s.ReadUInt32();

					var bounds = new Rectangle(0, 0, size.Width, size.Height);
					if ((flags & 0x01) != 0)
					{
						var extraBounds = new Rectangle(extraX, extraY, extraWidth, extraHeight);
						bounds = Rectangle.Union(bounds, extraBounds);

						Offset = new float2(bounds.X + 0.5f * (bounds.Width - size.Width), bounds.Y + 0.5f * (bounds.Height - size.Height));
						Size = new Size(bounds.Width, bounds.Height);
					}

					// Skip unnecessary header data
					s.Position += 12;

					Data = new byte[bounds.Width * bounds.Height];

					// Unpack tile data
					var width = 4;
					for (var j = 0; j < size.Height; j++)
					{
						var start = (j - bounds.Y) * bounds.Width + (size.Width - width) / 2 - bounds.X;
						for (var i = 0; i < width; i++)
							Data[start + i] = s.ReadUInt8();

						width += (j < size.Height / 2 - 1 ? 1 : -1) * 4;
					}

					// TODO: Load Z-data once the renderer can handle it
					s.Position += size.Width * size.Height / 2;

					if ((flags & 0x01) == 0)
						return;

					// Load extra data (cliff faces, etc)
					for (var j = 0; j < extraHeight; j++)
					{
						var start = (j + extraY - bounds.Y) * bounds.Width + extraX - bounds.X;
						for (var i = 0; i < extraWidth; i++)
						{
							var extra = s.ReadUInt8();
							if (extra != 0)
								Data[start + i] = extra;
						}
					}
				}
				else
					Data = new byte[0];
			}
		}

		bool IsTmpTS(Stream s)
		{
			var start = s.Position;
			s.Position += 8;
			var sx = s.ReadUInt32();
			var sy = s.ReadUInt32();

			// Find the first non-empty frame
			var offset = s.ReadUInt32();
			while (offset == 0)
				offset = s.ReadUInt32();

			if (offset > s.Length - 52)
			{
				s.Position = start;
				return false;
			}

			s.Position = offset + 12;
			var test = s.ReadUInt32();

			s.Position = start;
			return test == sx * sy / 2 + 52;
		}

		TmpTSFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;
			var templateWidth = s.ReadUInt32();
			var templateHeight = s.ReadUInt32();
			var tileWidth = s.ReadInt32();
			var tileHeight = s.ReadInt32();
			var size = new Size(tileWidth, tileHeight);
			var offsets = new uint[templateWidth * templateHeight];
			for (var i = 0; i < offsets.Length; i++)
				offsets[i] = s.ReadUInt32();

			var tiles = new TmpTSFrame[offsets.Length];

			for (var j = 0; j < templateHeight; j++)
			{
				for (var i = 0; i < templateWidth; i++)
				{
					var k = j * templateWidth + i;
					s.Position = offsets[k];
					tiles[k] = new TmpTSFrame(s, size, i, j);
				}
			}

			s.Position = start;
			return tiles;
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames)
		{
			if (!IsTmpTS(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}
