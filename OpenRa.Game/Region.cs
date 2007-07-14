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

		public Region(Point location, Size size, MethodInvoker drawFunction)
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
