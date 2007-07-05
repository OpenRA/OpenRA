using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		GraphicsDevice device;

		public MainWindow()
		{
			Text = "OpenRA";
			ClientSize = new Size(640, 480);

			Visible = true;

			device = GraphicsDevice.Create(this, ClientSize.Width, ClientSize.Height, true, false);
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Frame();
				Application.DoEvents();
			}
		}

		void Frame()
		{
			device.Begin();
			device.Clear(0);

			device.End();
			device.Present();
		}
	}
}
