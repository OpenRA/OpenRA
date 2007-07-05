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
		readonly GraphicsDevice device;
		readonly Map map;
		readonly TileSet tileSet;
		
		Palette pal;
		Package TileMix;
		string TileSuffix;

		const string mapName = "scm12ea.ini";

		public MainWindow()
		{
			ClientSize = new Size(640, 480);

			Visible = true;

			device = GraphicsDevice.Create(this, ClientSize.Width, ClientSize.Height, true, false);

			IniFile mapFile = new IniFile(File.OpenRead("../../../" + mapName));
			map = new Map(mapFile);

			Text = string.Format("OpenRA - {0} - {1}", map.Title, mapName);

			tileSet = LoadTileSet(map);
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

			// render something :)

			device.End();
			device.Present();
		}

		TileSet LoadTileSet(Map currentMap)
		{
			switch (currentMap.Theater.ToLowerInvariant())
			{
				case "temperate":
					pal = new Palette(File.OpenRead("../../../temperat.pal"));
					TileMix = new Package("../../../temperat.mix");
					TileSuffix = ".tem";
					break;
				case "snow":
					pal = new Palette(File.OpenRead("../../../snow.pal"));
					TileMix = new Package("../../../snow.mix");
					TileSuffix = ".sno";
					break;
				case "interior":
					pal = new Palette(File.OpenRead("../../../interior.pal"));
					TileMix = new Package("../../../interior.mix");
					TileSuffix = ".int";
					break;
				default:
					throw new NotImplementedException();
			}
			return new TileSet(TileMix, TileSuffix, pal);
		}
	}
}
