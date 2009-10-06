using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenRa.Game.Graphics
{
	class Region
	{
		int2 location;
		Viewport viewport;

        public int2 Location
		{
			get { return location + new int2( (int)viewport.Location.X, (int)viewport.Location.Y ); }	// WTF HACK HACK HACK
		}

		public readonly float2 Size;

		Action drawFunction;
        Action<MouseInput> mouseHandler;
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

        public bool HandleMouseInput(MouseInput mi)
        {
            /* todo: route to the mousehandler once that's sorted */
            if (mouseHandler != null) mouseHandler(new MouseInput
            {
                Button = mi.Button,
                Event = mi.Event,
                Location = mi.Location - Location
            });
            return mouseHandler != null;
        }

		public static Region Create(Viewport v, DockStyle d, int size, Action f, Action<MouseInput> m)
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

		Region(int2 location, int2 size, Viewport viewport, Action drawFunction, Action<MouseInput> mouseHandler)
		{
			this.location = location;
			this.Size = size;
			this.drawFunction = drawFunction;
			this.viewport = viewport;
			this.mouseHandler = mouseHandler;
			rect = new Rectangle(location.X, location.Y, size.X, size.Y);
		}

		public bool Contains(int2 point) { return rect.Contains(point.ToPoint()); }

		public void Draw(Renderer renderer)
		{
			renderer.Device.EnableScissor((int)location.X, (int)location.Y, (int)Size.X, (int)Size.Y);
			drawFunction();
			renderer.Device.DisableScissor();
		}
	}
}
