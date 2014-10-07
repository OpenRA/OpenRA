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
			public float2 Offset { get { return float2.Zero; } }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public TmpTSFrame(Stream s, Size size)
			{
				if (s.Position != 0)
				{
					Size = size;

					// Ignore tile header for now
					s.Position += 52;

					Data = new byte[size.Width * size.Height];

					// Unpack tile data
					var width = 4;
					for (var i = 0; i < size.Height; i++)
					{
						var start = i * size.Width + (size.Width - width) / 2;
						for (var j = 0; j < width; j++)
							Data[start + j] = s.ReadUInt8();

						width += (i < size.Height / 2 - 1 ? 1 : -1) * 4;
					}

					// Ignore Z-data for now
					// Ignore extra data for now
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
			for (var i = 0; i < offsets.Length; i++)
			{
				s.Position = offsets[i];
				tiles[i] = new TmpTSFrame(s, size);
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
