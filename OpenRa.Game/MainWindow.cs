using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		GraphicsDevice device;

		const string mapName = "scm12ea.ini";

		public MainWindow()
		{
			ClientSize = new Size(640, 480);

			Visible = true;

			device = GraphicsDevice.Create(this, ClientSize.Width, ClientSize.Height, true, false);

			IniFile mapFile = new IniFile(File.OpenRead("../../../" + mapName));
			Map map = new Map(mapFile);

			Text = string.Format("OpenRA - {0} - {1}", map.Title, mapName);
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
