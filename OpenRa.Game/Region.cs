using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

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
		RectangleF rect;

		static float2 MakeSize(Viewport v, DockStyle d, float size)
		{
			switch (d)
			{
				case DockStyle.Top:
				case DockStyle.Bottom:
					return new float2(v.Width, size);

				case DockStyle.Left:
				case DockStyle.Right:
					return new float2(size, v.Height);

				default:
					throw new NotImplementedException();
			}
		}

		public void Clicked(MouseEventArgs e)
		{
			mouseHandler(this, e);
		}

		public static Region Create(Viewport v, DockStyle d, float size, Action f, MouseEventHandler m)
		{
			float2 s = MakeSize(v, d, size);

			switch (d)
			{
				case DockStyle.Top:
				case DockStyle.Left:
					return new Region(new float2(0,0), s, v, f, m);

				case DockStyle.Right:
				case DockStyle.Bottom:
					return new Region(new float2( v.Width - s.X, v.Height - s.Y ), s, v, f, m);

				default:
					throw new NotImplementedException();
			}
		}

		Region(float2 location, float2 size, Viewport viewport, Action drawFunction, MouseEventHandler mouseHandler)
		{
			this.location = location;
			this.size = size;
			this.drawFunction = drawFunction;
			this.viewport = viewport;
			this.mouseHandler = mouseHandler;
			rect = new RectangleF(location.ToPointF(), new SizeF(size.ToPointF()));
		}

		public bool Contains(float2 point)
		{
			return rect.Contains(point.ToPointF());
		}

		public void Draw(Renderer renderer)
		{
			renderer.Device.EnableScissor((int)location.X, (int)location.Y, (int)size.X, (int)size.Y);
			drawFunction();
			renderer.Device.DisableScissor();
		}
	}
}
