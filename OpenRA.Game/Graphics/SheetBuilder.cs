#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Graphics
{
	public class SheetOverflowException : Exception
	{
		public SheetOverflowException()
			: base("Sprite sequence spans multiple sheets.\n"+
				"This should be considered as a bug, but you "+
				"can increase the Graphics.SheetSize setting "+
				"to temporarily avoid the problem.") {}
	}

	public class SheetBuilder
	{
		Sheet current;
		int rowHeight = 0;
		Point p;
		TextureChannel channel;
		Sheet NewSheet() { return new Sheet(new Size(Renderer.SheetSize, Renderer.SheetSize)); }

		internal SheetBuilder(TextureChannel ch)
		{
			current = NewSheet();
			channel = ch;
		}

		public Sprite Add(byte[] src, Size size, bool allowSheetOverflow)
		{
			Sprite rect = Allocate(size, allowSheetOverflow);
			Util.FastCopyIntoChannel(rect, src);
			return rect;
		}

		public Sprite Add(Size size, byte paletteIndex, bool allowSheetOverflow)
		{
			byte[] data = new byte[size.Width * size.Height];
			for (int i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size, allowSheetOverflow);
		}

		TextureChannel? NextChannel(TextureChannel t)
		{
			switch (t)
			{
				case TextureChannel.Red: return TextureChannel.Green;
				case TextureChannel.Green: return TextureChannel.Blue;
				case TextureChannel.Blue: return TextureChannel.Alpha;
				case TextureChannel.Alpha:
				default: return null;
			}
		}

		public Sprite Allocate(Size imageSize, bool allowSheetOverflow)
		{
			if (imageSize.Width + p.X > current.Size.Width)
			{
				p = new Point(0, p.Y + rowHeight);
				rowHeight = imageSize.Height;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (p.Y + imageSize.Height > current.Size.Height)
			{
				var next = NextChannel(channel);
				if (next == null)
				{
					if (!allowSheetOverflow)
						throw new SheetOverflowException();

					current = new Sheet(new Size(Renderer.SheetSize, Renderer.SheetSize));
					channel = TextureChannel.Red;
				}
				else
					channel = next.Value;

				rowHeight = imageSize.Height;
				p = new Point(0,0);
			}

			Sprite rect = new Sprite(current, new Rectangle(p, imageSize), channel);
			current.MakeDirty();
			p.X += imageSize.Width;

			return rect;
		}
	}
}
