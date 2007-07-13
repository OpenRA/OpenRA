using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly Renderer renderer;
		readonly Map map;
		
		Package TileMix;

		World world;
		TreeCache treeCache;
		TerrainRenderer terrain;
		Sidebar sidebar;

		static Size GetResolution(Settings settings)
		{
			Size desktopResolution = Screen.PrimaryScreen.Bounds.Size;

			return new Size(settings.GetValue("width", desktopResolution.Width),
				settings.GetValue("height", desktopResolution.Height));
		}

		public MainWindow( Settings settings )
		{
			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Black;
			renderer = new Renderer(this, GetResolution(settings), false);
			Visible = true;

			CoreSheetBuilder.Initialize(renderer.Device);

			map = new Map(new IniFile(File.OpenRead("../../../" + settings.GetValue("map", "scm12ea.ini"))));

			TileMix = new Package("../../../" + map.Theater + ".mix");

			renderer.SetPalette(new HardwarePalette(renderer.Device, map));
			terrain = new TerrainRenderer(renderer, map, TileMix);

			world = new World(renderer.Device);
			treeCache = new TreeCache(renderer.Device, map, TileMix);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map));

			world.Add(new Mcv(new PointF(24 * 5, 24 * 5), 3));
			world.Add(new Mcv(new PointF(24 * 7, 24 * 5), 2));
			world.Add(new Mcv(new PointF(24 * 9, 24 * 5), 1));

			world.Add(new Refinery(new PointF(24 * 5, 24 * 7), 1));

			sidebar = new Sidebar(OpenRa.TechTree.Race.None, renderer);
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Frame();
				Application.DoEvents();
			}
		}

		PointF scrollPos;
		int x1,y1;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			x1 = e.X;
			y1 = e.Y;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button != 0)
			{
				scrollPos.X += x1 - e.X;
				scrollPos.Y += y1 - e.Y;

				x1 = e.X;
				y1 = e.Y;

				scrollPos.X = Util.Constrain(scrollPos.X, new Range<float>(0, map.Width * 24 - ClientSize.Width));
				scrollPos.Y = Util.Constrain(scrollPos.Y, new Range<float>(0, map.Height * 24 - ClientSize.Height));
			}
		}

		void Frame()
		{
			PointF r1 = new PointF(2.0f / ClientSize.Width, -2.0f / ClientSize.Height);
			PointF r2 = new PointF(-1, 1);

			renderer.BeginFrame(r1, r2, scrollPos);

			terrain.Draw( ClientSize, scrollPos );

			world.Draw(renderer,
				new Range<float>(scrollPos.X, scrollPos.X + ClientSize.Width),
				new Range<float>(scrollPos.Y, scrollPos.Y + ClientSize.Height));

			sidebar.Paint(scrollPos);

			renderer.EndFrame();
		}
	}
}
