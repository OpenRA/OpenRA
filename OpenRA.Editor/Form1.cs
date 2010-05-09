using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;

namespace OpenRA.Editor
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			LocateGameRoot();

			LoadMap("ra", "scm01ea");
		}

		void LoadMap(string mod, string mapname)
		{
			tilePalette.Controls.Clear();

			var mods = new[] { mod };

			var manifest = new Manifest(mods);

			foreach (var folder in manifest.Folders) FileSystem.Mount(folder);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);

			// load the map
			var map = new Map(new Folder("mods/{0}/maps/{1}".F(mod, mapname)));

			Game.LoadModAssemblies(manifest);
			Rules.LoadRules(manifest, map);

			// we're also going to need a tileset...
			var tsinfo = fileMapping[Pair.New(mods[0], map.Theater)];
			var tileset = new TileSet("tileset.til", "templates.ini", tsinfo.First);

			var palette = new Palette(FileSystem.Open(map.Theater.ToLowerInvariant() + ".pal"), true);

			surface1.Bind(map, tileset, palette);

			// construct the palette of tiles

			foreach (var n in tileset.tiles.Keys)
			{
				try
				{
					var bitmap = RenderTemplate(tileset, (ushort)n, palette);
					var ibox = new PictureBox
					{
						Image = bitmap,
						Width = bitmap.Width / 2,
						Height = bitmap.Height / 2,
						SizeMode = PictureBoxSizeMode.StretchImage
					};

					var p = Pair.New(n, bitmap);
					ibox.Click += (_, e) => surface1.Brush = p;

					var template = tileset.walk[n];
					tilePalette.Controls.Add(ibox);

					tt.SetToolTip(ibox,
						"{1}:{0} ({3}x{4} {2})".F(
						template.Name,
						template.Index,
						template.Bridge,
						template.Size.X,
						template.Size.Y));
				}
				catch { }
			}
		}

		void LocateGameRoot()
		{
			while (!Directory.Exists("mods"))
			{
				var current = Directory.GetCurrentDirectory();
				if (Directory.GetDirectoryRoot(current) == current)
					throw new InvalidOperationException("Unable to find game root.");
				Directory.SetCurrentDirectory("..");
			}
		}

		static Dictionary<Pair<string, string>, Pair<string, string>> fileMapping = new Dictionary<Pair<string, string>, Pair<string, string>>()
		{
			{Pair.New("ra","TEMPERAT"),Pair.New("tem","temperat.col")},
			{Pair.New("ra","SNOW"),Pair.New("sno","snow.col")},
			{Pair.New("ra","INTERIOR"),Pair.New("int","temperat.col")},
			{Pair.New("cnc","DESERT"),Pair.New("des","desert.col")},
			{Pair.New("cnc","TEMPERAT"),Pair.New("tem","temperat.col")},
			{Pair.New("cnc","WINTER"),Pair.New("win","winter.col")},
		};

		static Bitmap RenderTemplate(TileSet ts, ushort n, Palette p)
		{
			var template = ts.walk[n];
			var tile = ts.tiles[n];

			var bitmap = new Bitmap(24 * template.Size.X, 24 * template.Size.Y);
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* q = (int*)data.Scan0.ToPointer();
				var stride = data.Stride >> 2;

				for (var u = 0; u < template.Size.X; u++)
					for (var v = 0; v < template.Size.Y; v++)
						if (tile.TileBitmapBytes[u + v * template.Size.X] != null)
						{
							var rawImage = tile.TileBitmapBytes[u + v * template.Size.X];
							for (var i = 0; i < 24; i++)
								for (var j = 0; j < 24; j++)
									q[(v * 24 + j) * stride + u * 24 + i] = p.GetColor(rawImage[i + 24 * j]).ToArgb();
						}
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}
	}
}