using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using System.Runtime.InteropServices;
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
			Location = new Point();
			Visible = true;

			renderer = new Renderer(this, GetResolution(settings), false);

			map = new Map(new IniFile(File.OpenRead("../../../" + settings.GetValue("map", "scm12ea.ini"))));

			viewport = new Viewport(ClientSize, new float2(map.Size));

			SheetBuilder.Initialize(renderer.Device);

			TileMix = new Package("../../../" + map.Theater + ".mix");

			renderer.SetPalette(new HardwarePalette(renderer.Device, map));
			terrain = new TerrainRenderer(renderer, map, TileMix);

			world = new World(renderer);
			treeCache = new TreeCache(renderer.Device, map, TileMix);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map));

			world.Add(new Mcv(24 * new float2(5, 5), 3));
			world.Add(new Mcv(24 * new float2(7, 5), 2));
			world.Add(new Mcv(24 * new float2(9, 5), 1));

			world.Add(new Refinery(24 * new float2(5, 7), 1));

			sidebar = new Sidebar(Race.Soviet, renderer);
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
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button != 0)
			{
				float2 p = new float2(e.Location);
				viewport.Scroll(lastPos - p);
				lastPos = p;
			}
		}

		void Frame()
		{
			PointF r1 = new PointF(2.0f / viewport.ClientSize.Width, -2.0f / viewport.ClientSize.Height);
			PointF r2 = new PointF(-1, 1);

			renderer.BeginFrame(r1, r2, viewport.ScrollPosition);

			renderer.Device.EnableScissor(0, 0, viewport.ClientSize.Width - 128, viewport.ClientSize.Height);
			terrain.Draw(viewport);

			world.Draw(renderer, viewport);

			renderer.Device.DisableScissor();
			sidebar.Paint(viewport);

			renderer.EndFrame();
		}
	}
}
