using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using IjwFramework.Delegates;

namespace OpenRa.Game
{
	class Region
	{
		float2 location;
		Viewport viewport;

		public float2 Location
		{
			get { return location + viewport.Location; }
		}

		float2 size;

		public float2 Size
		{
			get { return size; }
		}

		Action drawFunction;
		MouseEventHandler mouseHandler;
		Rectangle rect;

		static int2 MakeSize(Viewport v, DockStyle d, int size)
		{
			switch (d)
			{
				case DockStyle.Top:
				case DockStyle.Bottom:
					return new int2(v.Width, size);

				case DockStyle.Left:
				case DockStyle.Right:
					return new int2(size, v.Height);

				default:
					throw new NotImplementedException();
			}
		}

		public void Clicked(MouseEventArgs e)
		{
			mouseHandler(this, new MouseEventArgs(e.Button, e.Clicks, e.X - rect.Left, e.Y - rect.Top, e.Delta));
		}

		public static Region Create(Viewport v, DockStyle d, int size, Action f, MouseEventHandler m)
		{
			int2 s = MakeSize(v, d, size);

			switch (d)
			{
				case DockStyle.Top:
				case DockStyle.Left:
					return new Region(int2.Zero, s, v, f, m);

				case DockStyle.Right:
				case DockStyle.Bottom:
					return new Region(new int2( v.Width - s.X, v.Height - s.Y ), s, v, f, m);

				default:
					throw new NotImplementedException();
			}
		}

		Region(int2 location, int2 size, Viewport viewport, Action drawFunction, MouseEventHandler mouseHandler)
		{
			this.location = location;
			this.size = size;
			this.drawFunction = drawFunction;
			this.viewport = viewport;
			this.mouseHandler = mouseHandler;
			rect = new Rectangle(location.X, location.Y, size.X, size.Y);
		}

		public bool Contains(int2 point) { return rect.Contains(point.ToPoint()); }

		public void Draw(Renderer renderer)
		{
			renderer.Device.EnableScissor((int)location.X, (int)location.Y, (int)size.X, (int)size.Y);
			drawFunction();
			renderer.Device.DisableScissor();
		}
	}
}
