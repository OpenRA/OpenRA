#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class IcnD2Loader : ISpriteLoader
	{
		public const int SizeX = 16;
		public const int SizeY = 16;
		public ushort BuildingsStartIndex;
		const int TileSize = SizeX * SizeY / 2;

		uint ssetOffset, ssetLength;
		uint rpalOffset, rpalLength;
		uint rtblOffset, rtblLength;
		uint numTiles;

		byte[] rtbl;
		byte[][] rpal;

		class IcnD2Tile : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get { return Size; } }
			public float2 Offset { get { return float2.Zero; } }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public IcnD2Tile(Stream s, byte[] palette)
			{
				var tile = StreamExts.ReadBytes(s, TileSize);

				Size = new Size(SizeX, SizeY);
				Data = new byte[Size.Width * Size.Height];

				var i = 0;
				for (var y = 0; y < SizeY; y++)
				{
					for (var x = 0; x < SizeX; x += 2)
					{
						var val = tile[(y * SizeX + x) / 2];
						Data[i++] = palette[val >> 4];
						Data[i++] = palette[val & 0x0F];
					}
				}
			}
		}

		bool IsIcnD2(Stream s)
		{
			if (s.Length < 0x20)
				return false;

			var start = s.Position;

			s.Position = 0x18;
			if (s.ReadASCII(4) != "SSET")
			{
				s.Position = start;
				return false;
			}

			ssetLength = int2.Swap(s.ReadUInt32()) - 8;
			s.Position += 3;
			BuildingsStartIndex = s.ReadUInt8();
			ssetOffset = 0x18 + 16;
			if (s.Length < ssetOffset + ssetLength)
			{
				s.Position = start;
				return false;
			}

			s.Position = ssetOffset + ssetLength;
			if (s.ReadASCII(4) != "RPAL")
			{
				s.Position = start;
				return false;
			}

			rpalLength = int2.Swap(s.ReadUInt32());
			rpalOffset = ssetOffset + ssetLength + 8;
			if (s.Length < rpalOffset + rpalLength)
			{
				s.Position = start;
				return false;
			}

			s.Position = rpalOffset + rpalLength;
			if (s.ReadASCII(4) != "RTBL")
			{
				s.Position = start;
				return false;
			}

			rtblLength = int2.Swap(s.ReadUInt32());
			rtblOffset = rpalOffset + rpalLength + 8;

			if (s.Length < rtblOffset + rtblLength)
			{
				s.Position = start;
				return false;
			}

			numTiles = ssetLength / TileSize;

			if (rtblLength < numTiles)
			{
				s.Position = start;
				return false;
			}

			s.Position = start;
			return true;
		}

		void ReadTables(Stream s)
		{
			var start = s.Position;

			s.Position = rtblOffset;
			rtbl = StreamExts.ReadBytes(s, (int)rtblLength);

			s.Position = rpalOffset;
			rpal = new byte[rpalLength / 16][];
			for (var i = 0; i < rpal.Length; i++)
				rpal[i] = StreamExts.ReadBytes(s, 16);

			s.Position = start;
		}

		IcnD2Tile[] ParseFrames(Stream s)
		{
			var start = s.Position;

			ReadTables(s);

			var tiles = new IcnD2Tile[numTiles];
			s.Position = ssetOffset;
			for (var i = 0; i < tiles.Length; i++)
				tiles[i] = new IcnD2Tile(s, rpal[rtbl[i]]);

			s.Position = start;
			return tiles;
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames)
		{
			if (!IsIcnD2(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}
