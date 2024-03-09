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
using System.Runtime.InteropServices;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.SpriteLoaders
{
	public class R8Loader : ISpriteLoader
	{
		public sealed class RemappableFrame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Bgra32;
			public Size Size => inner.Size;
			public Size FrameSize => inner.FrameSize;
			public float2 Offset => inner.Offset;
			public bool DisableExportPadding => inner.DisableExportPadding;

			readonly Frame inner;
			readonly bool useShadow;
			readonly bool convertShroudToFog;
			readonly Color remap;
			byte[] data;

			public RemappableFrame(Frame inner, bool useShadow = true, bool convertShroudToFog = false, Color remap = default)
			{
				this.inner = inner;
				this.useShadow = useShadow;
				this.convertShroudToFog = convertShroudToFog;
				this.remap = remap;
			}

			public byte[] Data
			{
				get
				{
					if (data == null)
					{
						var pixelCount = inner.Size.Width * inner.Size.Height;
						data = new byte[4 * pixelCount];

						var palette = inner.Palette;
						if (useShadow || convertShroudToFog || remap != default)
						{
							palette = new uint[256];
							Array.Copy(inner.Palette, palette, 256);
						}

						// Bit twiddling is equivalent to unpacking RGB channels, dividing them by 2, subtracting from 255, then repacking
						if (convertShroudToFog)
							for (var i = 0; i < Palette.Size; i++)
								palette[i] = ~((palette[i] >> 1) & 0x007F7F7F);

						// Remap index 1 to shadow
						if (useShadow)
							palette[1] = 140u << 24;

						if (remap != default)
						{
							var r = new PlayerColorRemap(Enumerable.Range(240, 16).ToArray(), remap);
							for (var i = 240; i < 256; i++)
								palette[i] = r.GetRemappedColor(Color.FromArgb(palette[i]), i).ToArgb();
						}

						unsafe
						{
							fixed (byte* bd = &Data[0])
							{
								var data = (uint*)bd;
								for (var i = 0; i < pixelCount; i++)
									data[i] = palette[inner.Data[i]];
							}
						}
					}

					return data;
				}
			}

			public RemappableFrame WithSequenceFlags(bool useShadow, bool convertShroudToFog, Color remap)
			{
				return new RemappableFrame(inner, useShadow, convertShroudToFog, remap);
			}
		}

		public sealed class Frame : ISpriteFrame
		{
			public SpriteFrameType Type { get; }
			public Size Size { get; }
			public Size FrameSize { get; }
			public float2 Offset { get; }
			public byte[] Data { get; }
			public bool DisableExportPadding => true;

			public uint[] Palette { get; }

			public Frame(Stream s, uint[] lastPalette)
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

				s.ReadUInt32(); // imageHandle
				var paletteHandle = s.ReadInt32();
				var bpp = s.ReadUInt8();
				if (bpp != 8 && bpp != 16)
					throw new InvalidDataException($"Error: {bpp} bits per pixel are not supported.");

				var frameHeight = s.ReadUInt8();
				var frameWidth = s.ReadUInt8();
				FrameSize = new Size(frameWidth, frameHeight);

				// Skip alignment byte
				s.ReadUInt8();

				if (bpp == 16)
				{
					Data = new byte[width * height * 4];
					Type = SpriteFrameType.Bgra32;

					var data = MemoryMarshal.Cast<byte, uint>(Data);
					s.ReadBytes(Data.AsSpan()[..(Data.Length / 2)]);
					for (var i = width * height - 1; i >= 0; i--)
					{
						var packed = Data[i * 2 + 1] << 8 | Data[i * 2];
						data[i] = (uint)((0xFF << 24) | ((packed & 0x7C00) << 9) | ((packed & 0x3E0) << 6) | ((packed & 0x1f) << 3));
					}
				}
				else
				{
					Data = s.ReadBytes(width * height);
					Type = SpriteFrameType.Indexed8;
				}

				// Read palette
				if (type == 1 && paletteHandle != 0)
				{
					// Skip header
					s.ReadUInt32();
					s.ReadUInt32();

					Palette = new uint[256];
					var palette = MemoryMarshal.Cast<uint, byte>(Palette);
					s.ReadBytes(palette[..(palette.Length / 2)]);
					for (var i = 255; i >= 0; i--)
					{
						var packed = palette[i * 2 + 1] << 8 | palette[i * 2];
						Palette[i] = (uint)((0xFF << 24) | ((packed & 0x7C00) << 9) | ((packed & 0x3E0) << 6) | ((packed & 0x1f) << 3));
					}

					// Remap index 0 to transparent
					Palette[0] = 0;
				}
				else if (type == 2)
					Palette = lastPalette;
			}
		}

		static bool IsR8(Stream s)
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
			if (!IsR8(s))
			{
				frames = null;
				return false;
			}

			var start = s.Position;
			var tmp = new List<Frame>();

			uint[] lastPalette = null;
			while (s.Position < s.Length)
			{
				var f = new Frame(s, lastPalette);
				if (f.Palette != null)
					lastPalette = f.Palette;

				tmp.Add(f);
			}

			s.Position = start;
			frames = tmp.Select<Frame, ISpriteFrame>(f => f.Palette != null ? new RemappableFrame(f) : f).ToArray();

			return true;
		}
	}
}
