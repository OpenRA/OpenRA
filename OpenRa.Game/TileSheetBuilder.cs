using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	// T is probably going to be BluntDirectX.Direct3D.Texture


	public delegate T Provider<T>();

	class TileSheetBuilder<T>
		where T : class
	{
		readonly Size pageSize;
		readonly Provider<T> pageProvider;

		public TileSheetBuilder(Size pageSize, Provider<T> pageProvider)
		{
			this.pageSize = pageSize;
			this.pageProvider = pageProvider;
		}

		public SheetRectangle<T> AddImage(Size imageSize)
		{
			throw new NotImplementedException();
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
