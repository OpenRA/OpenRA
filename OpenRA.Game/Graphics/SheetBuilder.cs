#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	[Serializable]
	public class SheetOverflowException : Exception
	{
		public SheetOverflowException(string message)
			: base(message) { }
	}

	// The enum values indicate the number of channels used by the type
	// They are not arbitrary IDs!
	public enum SheetType
	{
		Indexed = 1,
		BGRA = 4,
	}

	public sealed class SheetBuilder : IDisposable
	{
		public readonly SheetType Type;
		readonly List<Sheet> sheets = new List<Sheet>();
		readonly Func<Sheet> allocateSheet;
		readonly int margin;

		Sheet current;
		TextureChannel channel;
		int rowHeight = 0;
		int2 p;

		public static Sheet AllocateSheet(SheetType type, int sheetSize)
		{
			return new Sheet(type, new Size(sheetSize, sheetSize));
		}

		public static SheetType FrameTypeToSheetType(SpriteFrameType t)
		{
			switch (t)
			{
				case SpriteFrameType.Indexed8:
					return SheetType.Indexed;

				// Util.FastCopyIntoChannel will automatically convert these to BGRA
				case SpriteFrameType.Bgra32:
				case SpriteFrameType.Bgr24:
				case SpriteFrameType.Rgba32:
				case SpriteFrameType.Rgb24:
					return SheetType.BGRA;
				default: throw new NotImplementedException("Unknown SpriteFrameType {0}".F(t));
			}
		}

		public SheetBuilder(SheetType t)
			: this(t, Game.Settings.Graphics.SheetSize) { }

		public SheetBuilder(SheetType t, int sheetSize, int margin = 1)
			: this(t, () => AllocateSheet(t, sheetSize), margin) { }

		public SheetBuilder(SheetType t, Func<Sheet> allocateSheet, int margin = 1)
		{
			channel = t == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
			Type = t;
			current = allocateSheet();
			sheets.Add(current);
			this.allocateSheet = allocateSheet;
			this.margin = margin;
		}

		public Sprite Add(ISpriteFrame frame) { return Add(frame.Data, frame.Type, frame.Size, 0, frame.Offset); }
		public Sprite Add(byte[] src, SpriteFrameType type, Size size) { return Add(src, type, size, 0, float3.Zero); }
		public Sprite Add(byte[] src, SpriteFrameType type, Size size, float zRamp, in float3 spriteOffset)
		{
			// Don't bother allocating empty sprites
			if (size.Width == 0 || size.Height == 0)
				return new Sprite(current, Rectangle.Empty, 0, spriteOffset, channel, BlendMode.Alpha);

			var rect = Allocate(size, zRamp, spriteOffset);
			Util.FastCopyIntoChannel(rect, src, type);
			current.CommitBufferedData();
			return rect;
		}

		public Sprite Add(Png src, float scale = 1f)
		{
			var rect = Allocate(new Size(src.Width, src.Height),  scale);
			Util.FastCopyIntoSprite(rect, src);
			current.CommitBufferedData();
			return rect;
		}

		TextureChannel? NextChannel(TextureChannel t)
		{
			var nextChannel = (int)t + (int)Type;
			if (nextChannel > (int)TextureChannel.Alpha)
				return null;

			return (TextureChannel)nextChannel;
		}

		public Sprite Allocate(Size imageSize, float scale = 1f) { return Allocate(imageSize, 0, float3.Zero, scale); }
		public Sprite Allocate(Size imageSize, float zRamp, in float3 spriteOffset, float scale = 1f)
		{
			if (imageSize.Width + p.X + margin > current.Size.Width)
			{
				p = new int2(0, p.Y + rowHeight + margin);
				rowHeight = imageSize.Height;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (p.Y + imageSize.Height + margin > current.Size.Height)
			{
				var next = NextChannel(channel);
				if (next == null)
				{
					current.ReleaseBuffer();
					current = allocateSheet();
					sheets.Add(current);
					channel = Type == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
				}
				else
					channel = next.Value;

				rowHeight = imageSize.Height;
				p = int2.Zero;
			}

			var rect = new Sprite(current, new Rectangle(p.X + margin, p.Y + margin, imageSize.Width, imageSize.Height), zRamp, spriteOffset, channel, BlendMode.Alpha, scale);
			p += new int2(imageSize.Width + margin, 0);

			return rect;
		}

		public Sheet Current { get { return current; } }
		public TextureChannel CurrentChannel { get { return channel; } }
		public IEnumerable<Sheet> AllSheets { get { return sheets; } }

		public void Dispose()
		{
			foreach (var sheet in sheets)
				sheet.Dispose();
			sheets.Clear();
		}
	}
}
