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

	public enum SheetType
	{
		Indexed = 1,
		DualIndexed = 2,
		BGRA = 4,
	}

	public class SheetBuilder
	{
		Sheet current;
		TextureChannel channel;
		SheetType type;
		int rowHeight = 0;
		Point p;

		internal SheetBuilder(SheetType t)
		{
			current = new Sheet(new Size(Renderer.SheetSize, Renderer.SheetSize));;
			channel = TextureChannel.Red;
			type = t;
		}

		public Sprite Add(byte[] src, Size size, bool allowSheetOverflow)
		{
			var rect = Allocate(size, allowSheetOverflow);
			Util.FastCopyIntoChannel(rect, src);
			return rect;
		}

		public Sprite Add(Size size, byte paletteIndex, bool allowSheetOverflow)
		{
			var data = new byte[size.Width * size.Height];
			for (var i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size, allowSheetOverflow);
		}

		TextureChannel? NextChannel(TextureChannel t)
		{
			var nextChannel = (int)t + (int)type;
			if (nextChannel > (int)TextureChannel.Alpha)
				return null;

			return (TextureChannel)nextChannel;
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

			var rect = new Sprite(current, new Rectangle(p, imageSize), channel);
			current.MakeDirty();
			p.X += imageSize.Width;

			return rect;
		}

		public Sheet Current { get { return current; } }
	}
}
