using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	// T is probably going to be BluntDirectX.Direct3D.Texture
	public delegate T Provider<T>();

	public class TileSheetBuilder<T>
		where T : class
	{
		readonly SizeF pageSize;
		readonly Provider<T> pageProvider;

		public TileSheetBuilder(SizeF pageSize, Provider<T> pageProvider)
		{
			this.pageSize = pageSize;
			this.pageProvider = pageProvider;
		}

		T current = null;
		float x = 0, y = 0, rowHeight = 0;

		public SheetRectangle<T> AddImage(SizeF imageSize)
		{
			if (imageSize.Width > pageSize.Width || imageSize.Height > pageSize.Height)
				return null;

			if (current == null)
				current = pageProvider();

			if (imageSize.Height > rowHeight || rowHeight == 0 || imageSize.Width + x > pageSize.Width)
			{
				y += rowHeight;
				rowHeight = imageSize.Height;
				x = 0;
			}

			if (y + imageSize.Height > pageSize.Height)
			{
				current = pageProvider();
				x = y = rowHeight = 0;
			}

			SheetRectangle<T> rect = new SheetRectangle<T>(current, new PointF(x, y), imageSize);
			x += imageSize.Width;

			return rect;
		}
	}

	public class SheetRectangle<T>
		where T : class
	{
		readonly PointF origin;
		readonly SizeF size;
		readonly T sheet;

		internal SheetRectangle(T sheet, PointF origin, SizeF size)
		{
			this.origin = origin;
			this.size = size;
			this.sheet = sheet;
		}
	}
}
