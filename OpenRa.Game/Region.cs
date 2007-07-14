using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class Region
	{
		Point location;
		Size size;
		MethodInvoker drawFunction;

		static Size MakeSize(Viewport v, DockStyle d, int size)
		{
			switch (d)
			{
				case DockStyle.Top:
				case DockStyle.Bottom:
					return new Size(v.Width, size);

				case DockStyle.Left:
				case DockStyle.Right:
					return new Size(size, v.Height);

				default:
					throw new NotImplementedException();
			}
		}

		public static Region Create(Viewport v, DockStyle d, int size, MethodInvoker f)
		{
			Point topLeft = new Point(0, 0);
			Point bottomRight = new Point(v.Width, v.Height);

			Size s = MakeSize(v, d, size);

			switch (d)
			{
				case DockStyle.Top:
				case DockStyle.Left:
					return new Region(topLeft, s, f);

				case DockStyle.Right:
				case DockStyle.Bottom:
					Point origin = bottomRight; origin.Offset( -s.Width, -s.Height );
					return new Region(origin, s, f);

				default:
					throw new NotImplementedException();
			}
		}

		Region(Point location, Size size, MethodInvoker drawFunction)
		{
			this.location = location;
			this.size = size;
			this.drawFunction = drawFunction;
		}

		public void Draw(Renderer renderer, Viewport viewport)
		{
			renderer.Device.EnableScissor(location.X, location.Y, size.Width, size.Height);
			drawFunction();
			renderer.Device.DisableScissor();
		}
	}
}
