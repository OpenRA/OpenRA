using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenRA.FileFormats;
using System.IO;

namespace OpenRA.Editor
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			LocateGameRoot();

			var mods = new[] { "ra" };

			var manifest = new Manifest(mods);

			foreach (var folder in manifest.Folders) FileSystem.Mount(folder);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);

			// load the map
			var map = new Map(new Folder("mods/ra/maps/mjolnir"));

			// we're also going to need a tileset...
			var tsinfo = fileMapping[Pair.New(mods[0], map.Theater)];
			var tileset = new TileSet("tileset.til", "templates.ini", tsinfo.First);

			var palette = new Palette( FileSystem.Open( map.Theater.ToLowerInvariant() + ".pal" ), true);

			surface1.TileSet = tileset;
			surface1.Map = map;

			// construct the palette of tiles

			foreach( var n in tileset.tiles.Keys )
			{
				try
				{
					var bitmap = RenderTemplate(tileset, (ushort)n, palette);
					var ibox = new PictureBox { Image = bitmap, Width = bitmap.Width, Height = bitmap.Height };
					tilePalette.Controls.Add(ibox);
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

			var bitmap = new Bitmap(Surface.CellSize * template.Size.X, Surface.CellSize * template.Size.Y);

			for( var u = 0; u < template.Size.X; u++ )
				for( var v = 0; v < template.Size.Y; v++ )
					if (template.TerrainType.ContainsKey(u + v * template.Size.X))
					{
						var rawImage = tile.TileBitmapBytes[u + v * template.Size.X];
						for (var i = 0; i < 24; i++)
							for (var j = 0; j < 24; j++)
								bitmap.SetPixel(u * 24 + i, v * 24 + j, p.GetColor(rawImage[i + 24 * j]));
					}

			return bitmap;
		}
	}
}
