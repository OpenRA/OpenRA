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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.SpriteLoaders
{
	public class R8Loader : ISpriteLoader
	{
		class R8Frame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size { get; }
			public Size FrameSize { get; }
			public float2 Offset { get; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding => true;

			public readonly uint[] Palette = null;

			public R8Frame(Stream s)
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
				if (bpp != 8)
					throw new InvalidDataException($"Error: {bpp} bits per pixel are not supported.");

				var frameHeight = s.ReadUInt8();
				var frameWidth = s.ReadUInt8();
				FrameSize = new Size(frameWidth, frameHeight);

				// Skip alignment byte
				s.ReadUInt8();

				Data = s.ReadBytes(width * height);

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

		bool IsR8(Stream s)
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
			return d == 8;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsR8(s))
			{
				frames = null;
				return false;
			}

			var start = s.Position;
			var tmp = new List<R8Frame>();
			var palettes = new Dictionary<int, uint[]>();
			while (s.Position < s.Length)
			{
				var f = new R8Frame(s);
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
