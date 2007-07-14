using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	class Region
	{
		PointF location;
		Size size;
		Renderable drawFunction;

		public Region(PointF location, Size size, Renderable drawFunction)
		{
			this.location = location;
			this.size = size;
			this.drawFunction = drawFunction;
		}

		public void Draw(Renderer renderer, Viewport viewport)
		{
			renderer.Device.EnableScissor(location.X, location.Y, size.Width, size.Height);
			drawFunction(renderer, viewport);
			renderer.Device.DisableScissor();
		}
	}
}
