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

using System.Drawing;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class TmpTDLoader : ISpriteLoader
	{
		class TmpTDFrame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get { return float2.Zero; } }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public TmpTDFrame(byte[] data, Size size)
			{
				FrameSize = size;
				Data = data;

				if (data == null)
					Data = new byte[0];
				else
					Size = size;
			}
		}

		bool IsTmpTD(Stream s)
		{
			var start = s.Position;

			s.Position += 16;
			var a = s.ReadUInt32();
			var b = s.ReadUInt32();

			s.Position = start;
			return a == 0 && b == 0x0D1AFFFF;
		}

		TmpTDFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);

			s.Position += 8;
			var imgStart = s.ReadUInt32();
			s.Position += 8;
			var indexEnd = s.ReadInt32();
			var indexStart = s.ReadInt32();

			s.Position = indexStart;
			var count = indexEnd - indexStart;
			var tiles = new TmpTDFrame[count];
			var tilesIndex = 0;
			foreach (var b in s.ReadBytes(count))
			{
				if (b != 255)
				{
					s.Position = imgStart + b * width * height;
					tiles[tilesIndex++] = new TmpTDFrame(s.ReadBytes(width * height), size);
				}
				else
					tiles[tilesIndex++] = new TmpTDFrame(null, size);
			}

			s.Position = start;
			return tiles;
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsTmpTD(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}
