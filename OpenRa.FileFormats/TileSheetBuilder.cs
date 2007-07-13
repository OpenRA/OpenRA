using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public delegate T Provider<T>();

	public class TileSheetBuilder<T>
		where T : class
	{
		readonly Size pageSize;
		readonly Provider<T> pageProvider;

		public TileSheetBuilder(Size pageSize, Provider<T> pageProvider)
		{
			this.pageSize = pageSize;
			this.pageProvider = pageProvider;
		}

		T current = null;
		int x = 0, y = 0, rowHeight = 0;

		public SheetRectangle<T> AddImage(Size imageSize)
		{
			if (imageSize.Width > pageSize.Width || imageSize.Height > pageSize.Height)
				return null;

			if (current == null)
				current = pageProvider();

			if (rowHeight == 0 || imageSize.Width + x > pageSize.Width)
			{
				y += rowHeight;
				rowHeight = imageSize.Height;
				x = 0;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (y + imageSize.Height > pageSize.Height)
			{
				current = pageProvider();
				x = y = rowHeight = 0;
			}

			SheetRectangle<T> rect = new SheetRectangle<T>(current, new Point(x, y), imageSize);
			x += imageSize.Width;

			return rect;
		}
	}

	public class SheetRectangle<T>
		where T : class
	{
		public readonly Point origin;
		public readonly Size size;
		public readonly T sheet;
		public readonly TextureChannel channel;

		internal SheetRectangle(T sheet, Point origin, Size size, TextureChannel channel)
		{
			this.origin = origin;
			this.size = size;
			this.sheet = sheet;
			this.channel = channel;
		}

		internal SheetRectangle(T sheet, Point origin, Size size)
			: this(sheet, origin, size, TextureChannel.Red)
		{
		}
	}

	public enum TextureChannel
	{
		Red,
		Green,
		Blue,
		Alpha,
	}
}
