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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using Pfim;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class TgaLoader : ISpriteLoader
	{
		public static bool IsTga(Stream s)
		{
			var start = s.Position;
			try
			{
				// Require true-color images
				s.Position += 1;
				var colorMapType = s.ReadUInt8();
				if (colorMapType != 0)
					return false;

				var imageType = s.ReadUInt8();
				if (imageType != 2)
					return false;

				var colorMapOffsetAndSize = s.ReadUInt32();
				if (colorMapOffsetAndSize != 0)
					return false;

				var colorMapBits = s.ReadUInt8();
				if (colorMapBits != 0)
					return false;

				return true;
			}
			finally
			{
				s.Position = start;
			}
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsTga(s))
			{
				frames = null;
				return false;
			}

			frames = new TgaSprite(s).Frames.ToArray();
			return true;
		}
	}

	public class TgaSprite
	{
		public class TgaFrame : ISpriteFrame
		{
			public SpriteFrameType Type { get; }
			public Size Size { get; }
			public Size FrameSize { get; }
			public float2 Offset { get; }
			public byte[] Data { get; }
			public bool DisableExportPadding => false;

			public TgaFrame()
			{
				Data = Array.Empty<byte>();
			}

			public TgaFrame(Stream stream)
			{
				using (var tga = Targa.Create(stream, new PfimConfig()))
				{
					Size = FrameSize = new Size(tga.Width, tga.Height);
					Data = tga.Data;
					switch (tga.Format)
					{
						// SpriteFrameType refers to the channel byte order, which is reversed from the little-endian bit order
						case ImageFormat.Rgba32: Type = SpriteFrameType.Bgra32; break;
						case ImageFormat.Rgb24: Type = SpriteFrameType.Bgr24; break;
						default: throw new InvalidDataException($"Unhandled ImageFormat {tga.Format}");
					}
				}
			}

			public TgaFrame(Stream stream, Size frameSize, Rectangle frameWindow)
				: this(stream)
			{
				FrameSize = frameSize;
				Offset = 0.5f * new float2(frameWindow.Left + frameWindow.Right - FrameSize.Width, frameWindow.Top + frameWindow.Bottom - FrameSize.Height);
			}
		}

		public IReadOnlyList<ISpriteFrame> Frames { get; }

		public TgaSprite(Stream stream)
		{
			Frames = new ISpriteFrame[] { new TgaFrame(stream) };
		}
	}
}
