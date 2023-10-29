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

using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.SpriteLoaders
{
	public class R816Loader : ISpriteLoader
	{
		sealed class R816Frame : ISpriteFrame
		{
			public SpriteFrameType Type { get; }
			public Size Size { get; }
			public Size FrameSize { get; }
			public float2 Offset { get; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding => true;

			public readonly uint[] Palette = null;

			public R816Frame(Stream s)
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
				Offset = new int2(width / 2 - x, height / 2 - y);

				/*var imageOffset = */
				s.ReadInt32();
				var paletteOffset = s.ReadInt32();
				var bpp = s.ReadUInt8();

				Type = bpp switch
				{
					8 => SpriteFrameType.Indexed8,
					16 => SpriteFrameType.Rgb24,
					_ => throw new InvalidDataException($"Error: {bpp} bits per pixel are not supported.")
				};

				var frameHeight = s.ReadUInt8();
				var frameWidth = s.ReadUInt8();
				FrameSize = new Size(frameWidth, frameHeight);

				// Skip alignment byte
				s.ReadUInt8();

				if (bpp == 8)
					Data = s.ReadBytes(width * height);
				else
				{
					Data = new byte[width * height * 3];

					for (var i = 0; i < Data.Length;)
					{
						var color16 = s.ReadUInt16();

						Data[i++] = (byte)(((color16 >> 7) & 0xf8) | ((color16 >> 12) & 0x07));
						Data[i++] = (byte)(((color16 >> 2) & 0xf8) | ((color16 >> 7) & 0x07));
						Data[i++] = (byte)(((color16 << 3) & 0xf8) | ((color16 >> 2) & 0x07));
					}
				}

				// Read palette
				if (type == 1 && paletteOffset != 0)
				{
					// Skip header
					s.ReadUInt32();
					s.ReadUInt32();

					Palette = new uint[256];
					for (var i = 0; i < 256; i++)
					{
						var packed = s.ReadUInt16();
						Palette[i] = (uint)((255 << 24) | ((packed & 0xF800) << 8) | ((packed & 0x7E0) << 5) | ((packed & 0x1f) << 3));
					}
				}
			}
		}

		static bool IsR816(Stream s)
		{
			var start = s.Position;

			// First byte is nonzero
			if (s.ReadUInt8() == 0)
			{
				s.Position = start;
				return false;
			}

			// Check the format of the first frame
			s.Position = start + 25;
			var d = s.ReadUInt8();

			s.Position = start;
			return d == 8 || d == 16;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsR816(s))
			{
				frames = null;
				return false;
			}

			var start = s.Position;
			var tmp = new List<R816Frame>();
			var palettes = new Dictionary<int, uint[]>();
			while (s.Position < s.Length)
			{
				var f = new R816Frame(s);
				if (f.Palette != null)
					palettes.Add(tmp.Count, f.Palette);
				tmp.Add(f);
			}

			s.Position = start;

			frames = tmp.ToArray();
			if (palettes.Count > 0)
				metadata = new TypeDictionary { new EmbeddedSpritePalette(framePalettes: palettes) };

			return true;
		}
	}
}
