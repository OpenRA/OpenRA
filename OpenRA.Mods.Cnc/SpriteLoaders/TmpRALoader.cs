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
	public class TmpRALoader : ISpriteLoader
	{
		class TmpRAFrame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size { get; }
			public Size FrameSize { get; }
			public float2 Offset => float2.Zero;
			public byte[] Data { get; set; }
			public bool DisableExportPadding => false;

			public TmpRAFrame(byte[] data, Size size)
			{
				FrameSize = size;
				Data = data;

				if (data == null)
					Data = Array.Empty<byte>();
				else
					Size = size;
			}
		}

		bool IsTmpRA(Stream s)
		{
			var start = s.Position;

			s.Position += 20;
			var a = s.ReadUInt32();
			s.Position += 2;
			var b = s.ReadUInt16();

			s.Position = start;
			return a == 0 && b == 0x2c73;
		}

		TmpRAFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);

			s.Position += 12;
			var imgStart = s.ReadUInt32();
			s.Position += 8;
			var indexEnd = s.ReadInt32();
			s.Position += 4;
			var indexStart = s.ReadInt32();

			s.Position = indexStart;
			var count = indexEnd - indexStart;
			var tiles = new TmpRAFrame[count];

			var tilesIndex = 0;
			foreach (var b in s.ReadBytes(count))
			{
				if (b != 255)
				{
					s.Position = imgStart + b * width * height;
					tiles[tilesIndex++] = new TmpRAFrame(s.ReadBytes(width * height), size);
				}
				else
					tiles[tilesIndex++] = new TmpRAFrame(null, size);
			}

			s.Position = start;
			return tiles;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsTmpRA(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}
