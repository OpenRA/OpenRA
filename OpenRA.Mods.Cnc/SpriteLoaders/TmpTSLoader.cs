#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.SpriteLoaders
{
	public class TmpTSLoader : ISpriteLoader
	{
		class TmpTSDepthFrame : ISpriteFrame
		{
			readonly TmpTSFrame parent;

			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size => parent.Size;
			public Size FrameSize => Size;
			public float2 Offset => parent.Offset;
			public byte[] Data => parent.DepthData;
			public bool DisableExportPadding => false;

			public TmpTSDepthFrame(TmpTSFrame parent)
			{
				this.parent = parent;
			}
		}

		class TmpTSFrame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size { get; }
			public Size FrameSize => Size;
			public float2 Offset { get; }
			public byte[] Data { get; set; }
			public byte[] DepthData { get; set; }
			public bool DisableExportPadding => false;

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
					DepthData = new byte[bounds.Width * bounds.Height];

					UnpackTileData(s, Data, size, bounds);
					UnpackTileData(s, DepthData, size, bounds);

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

					// Extra data depth
					for (var j = 0; j < extraHeight; j++)
					{
						var start = (j + extraY - bounds.Y) * bounds.Width + extraX - bounds.X;
						for (var i = 0; i < extraWidth; i++)
						{
							var extra = s.ReadUInt8();

							// XCC source indicates that there are only 32 valid values
							if (extra < 32)
								DepthData[start + i] = extra;
						}
					}
				}
				else
					Data = Array.Empty<byte>();
			}
		}

		static void UnpackTileData(Stream s, byte[] data, Size size, Rectangle frameBounds)
		{
			var width = 4;
			for (var j = 0; j < size.Height; j++)
			{
				var start = (j - frameBounds.Y) * frameBounds.Width + (size.Width - width) / 2 - frameBounds.X;
				for (var i = 0; i < width; i++)
					data[start + i] = s.ReadUInt8();

				width += (j < size.Height / 2 - 1 ? 1 : -1) * 4;
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

		ISpriteFrame[] ParseFrames(Stream s)
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

			// Depth information are stored as a second set of frames (like split shadows)
			var stride = offsets.Length;
			var tiles = new ISpriteFrame[stride * 2];

			for (var j = 0; j < templateHeight; j++)
			{
				for (var i = 0; i < templateWidth; i++)
				{
					var k = j * templateWidth + i;
					s.Position = offsets[k];

					var frame = new TmpTSFrame(s, size, i, j);
					tiles[k] = frame;
					tiles[k + stride] = new TmpTSDepthFrame(frame);
				}
			}

			s.Position = start;
			return tiles;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
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
