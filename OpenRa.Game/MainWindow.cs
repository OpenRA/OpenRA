using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using OpenRa.TechTree;

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
		Viewport viewport;

		ISelectable myUnit;

		static Size GetResolution(Settings settings)
		{
			Size desktopResolution = Screen.PrimaryScreen.Bounds.Size;

			return new Size(settings.GetValue("width", desktopResolution.Width),
				settings.GetValue("height", desktopResolution.Height));
		}

		public MainWindow(Settings settings)
		{
			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Black;
			StartPosition = FormStartPosition.Manual;
			Location = Point.Empty;
			Visible = true;

			renderer = new Renderer(this, GetResolution(settings), false);

			map = new Map(new IniFile(File.OpenRead("../../../" + settings.GetValue("map", "scm12ea.ini"))));

			viewport = new Viewport(new float2(ClientSize), new float2(map.Size), renderer);

			SheetBuilder.Initialize(renderer.Device);

			TileMix = new Package("../../../" + map.Theater + ".mix");

			renderer.SetPalette(new HardwarePalette(renderer.Device, map));
			terrain = new TerrainRenderer(renderer, map, TileMix, viewport);

			world = new World(renderer, viewport);
			treeCache = new TreeCache(renderer.Device, map, TileMix);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map));

			world.Add(new Mcv(new int2(5, 5), 3));
			world.Add(new Mcv(new int2(7, 5), 2));
			Mcv mcv = new Mcv( new int2( 9, 5 ), 1 );
			myUnit = mcv;
			world.Add( mcv );

			world.Add(new Refinery(24 * new float2(5, 7), 1));

			sidebar = new Sidebar(Race.Soviet, renderer, viewport);
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Frame();
				Application.DoEvents();
			}
		}

		float2 lastPos;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			lastPos = new float2(e.Location);

			if (e.Button == MouseButtons.Left)
			{
				int x = (int)( ( e.X + viewport.Location.X ) / 24 );
				int y = (int)( ( e.Y + viewport.Location.Y ) / 24 );
				myUnit.Order( new int2( x, y ) ).Apply();
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button == MouseButtons.Right)
			{
				float2 p = new float2(e.Location);
				viewport.Scroll(lastPos - p);
				lastPos = p;
			}
		}

		void Frame()
		{
			viewport.DrawRegions();
		}
	}
}
