#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.Mods.D2k.SpriteLoaders
{
	public class R8Loader : ISpriteLoader
	{
		class R8Frame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get; private set; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return true; } }

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
					throw new InvalidDataException("Error: {0} bits per pixel are not supported.".F(bpp));

				var frameHeight = s.ReadUInt8();
				var frameWidth = s.ReadUInt8();
				FrameSize = new Size(frameWidth, frameHeight);

				// Skip alignment byte
				s.ReadUInt8();

				Data = s.ReadBytes(width * height);

				// Ignore palette
				if (type == 1 && paletteOffset != 0)
					s.Seek(520, SeekOrigin.Current);
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

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames)
		{
			if (!IsR8(s))
			{
				frames = null;
				return false;
			}

			var start = s.Position;
			var tmp = new List<R8Frame>();
			while (s.Position < s.Length)
				tmp.Add(new R8Frame(s));
			s.Position = start;

			frames = tmp.ToArray();
			return true;
		}
	}
}
