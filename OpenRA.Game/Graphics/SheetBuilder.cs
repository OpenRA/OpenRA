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
		public SheetOverflowException(string message)
			: base(message) {}
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
		Func<Sheet> allocateSheet;

		public static Sheet AllocateSheet()
		{
			return new Sheet(new Size(Renderer.SheetSize, Renderer.SheetSize));;
		}

		internal SheetBuilder(SheetType t)
			: this(t, AllocateSheet) {}

		internal SheetBuilder(SheetType t, Func<Sheet> allocateSheet)
		{
			channel = TextureChannel.Red;
			type = t;
			current = allocateSheet();
			this.allocateSheet = allocateSheet;
		}

		public Sprite Add(byte[] src, Size size)
		{
			var rect = Allocate(size);
			Util.FastCopyIntoChannel(rect, src);
			return rect;
		}

		public Sprite Add(Size size, byte paletteIndex)
		{
			var data = new byte[size.Width * size.Height];
			for (var i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size);
		}

		TextureChannel? NextChannel(TextureChannel t)
		{
			var nextChannel = (int)t + (int)type;
			if (nextChannel > (int)TextureChannel.Alpha)
				return null;

			return (TextureChannel)nextChannel;
		}

		public Sprite Allocate(Size imageSize)
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
					current = allocateSheet();
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
