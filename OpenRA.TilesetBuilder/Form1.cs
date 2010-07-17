using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using OpenRA.FileFormats;
namespace OpenRA.TilesetBuilder
{
	public partial class Form1 : Form
	{
		string srcfile;
		public Form1( string src )
		{
			srcfile = src;

			InitializeComponent();

			surface1.Image = (Bitmap)Image.FromFile(src);
			surface1.TerrainTypes = new int[surface1.Image.Width / 24, surface1.Image.Height / 24];		/* all passable by default */
			surface1.Templates = new List<Template>();
			surface1.Size = surface1.Image.Size;

			/* todo: load stuff from previous session */
			Load();
		}

		public new void Load()
		{
			try
			{
				var doc = new XmlDocument();
				doc.Load(Path.ChangeExtension(srcfile, "tsx"));

				foreach (var e in doc.SelectNodes("//terrain").OfType<XmlElement>())
					surface1.TerrainTypes[
						int.Parse(e.GetAttribute("x")),
						int.Parse(e.GetAttribute("y"))] = int.Parse(e.GetAttribute("t"));

				foreach (var e in doc.SelectNodes("//template").OfType<XmlElement>())
					surface1.Templates.Add(new Template
					{
						Cells = e.SelectNodes("./cell").OfType<XmlElement>()
							.Select(f => new int2(int.Parse(f.GetAttribute("x")), int.Parse(f.GetAttribute("y"))))
							.ToDictionary(a => a, a => true)
					});
			}
			catch { }
		}

		public void Save()
		{
			using (var w = XmlWriter.Create(Path.ChangeExtension(srcfile, "tsx"), 
				new XmlWriterSettings { Indent = true, IndentChars = "  " }))
			{
				w.WriteStartDocument();
				w.WriteStartElement("tileset");

				for( var i = 0; i <= surface1.TerrainTypes.GetUpperBound(0); i++ )
					for( var j = 0; j <= surface1.TerrainTypes.GetUpperBound(1); j++ )
						if (surface1.TerrainTypes[i, j] != 0)
						{
							w.WriteStartElement("terrain");
							w.WriteAttributeString("x", i.ToString());
							w.WriteAttributeString("y", j.ToString());
							w.WriteAttributeString("t", surface1.TerrainTypes[i, j].ToString());
						}

				foreach (var t in surface1.Templates)
				{
					w.WriteStartElement("template");

					foreach (var c in t.Cells.Keys)
					{
						w.WriteStartElement("cell");
						w.WriteAttributeString("x", c.X.ToString());
						w.WriteAttributeString("y", c.Y.ToString());
						w.WriteEndElement();
					}

					w.WriteEndElement();
				}

				w.WriteEndElement();
				w.WriteEndDocument();
			}
		}

		void TerrainTypeSelectorClicked(object sender, EventArgs e)
		{
			surface1.InputMode = (sender as ToolStripButton).Tag as string;
			foreach (var tsb in (sender as ToolStripButton).Owner.Items.OfType<ToolStripButton>())
				tsb.Checked = false;
			(sender as ToolStripButton).Checked = true;
		}
		
		void SaveClicked(object sender, EventArgs e) { Save(); }
		void ShowOverlaysClicked(object sender, EventArgs e) { surface1.ShowTerrainTypes ^= true; }

		void ExportClicked(object sender, EventArgs e)
		{
			var dir = Path.Combine(Path.GetDirectoryName(srcfile), "output");
			Directory.CreateDirectory(dir);

			// Create a Tileset definition
			// Todo: Pull this info from the gui
			var tilesetFile = "tileset-arrakis.yaml";
			var tileset = new TileSet()
			{
				Name = "Arrakis",
				Id = "ARRAKIS",
				Palette = "arrakis.pal",
				Extensions = new string[] {".arr", ".shp"}
			};
			
			// export palette (use the embedded palette)
			var p = surface1.Image.Palette.Entries.ToList();
			ExportPalette(p, Path.Combine(dir, tileset.Palette));
			
			// Export tile artwork
			foreach (var t in surface1.Templates)
				ExportTemplate(t, surface1.Templates.IndexOf(t), tileset.Extensions.First(), dir);			
			
			// Add the terraintypes
			// Todo: add support for multiple/different terraintypes
			var terraintype = new TerrainTypeInfo()
			{
				Type = "clear",
				Buildable = true,
				AcceptSmudge = true,
				IsWater = false,
				Color = Color.White
			};
			tileset.Terrain.Add("clear", terraintype);
			
			// Add the templates
			ushort cur = 0;
			foreach (var tp in surface1.Templates)
			{
				var template = new TileTemplate()
				{
					Id = cur,
					Image = "t{0:00}".F(cur),
					Size = new int2(tp.Width,tp.Height),
				};
				
				// Todo: add support for different terraintypes
				// Todo: restrict cells? this doesn't work: .Where( c => surface1.TerrainTypes[c.Key.X, c.Key.Y] != 0 )
				foreach (var t in tp.Cells)
					template.Tiles.Add((byte)((t.Key.X - tp.Left) + tp.Width * (t.Key.Y - tp.Top)), "clear");

				tileset.Templates.Add(cur, template);
				cur++;
			}
			
			tileset.Save(Path.Combine(dir, tilesetFile));
			System.Console.WriteLine("Finished export");
		}
		
		void ExportPalette(List<Color> p, string file)
		{
			while (p.Count < 256) p.Add(Color.Black); // pad the palette out with extra blacks
			var paletteData = p.Take(256).SelectMany(c => new byte[] { (byte)(c.R >> 2), (byte)(c.G >> 2), (byte)(c.B >> 2) }).ToArray();
			File.WriteAllBytes(file, paletteData);
		}

		void ExportTemplate(Template t, int n, string suffix, string dir)
		{
			var filename = Path.Combine(dir, "t{0:00}{1}".F(n, suffix));
			var totalTiles = t.Width * t.Height;

			var ms = new MemoryStream();
			using (var bw = new BinaryWriter(ms))
			{
				bw.Write((ushort)24);
				bw.Write((ushort)24);
				bw.Write((uint)totalTiles);
				bw.Write((ushort)t.Width);
				bw.Write((ushort)t.Height);
				bw.Write((uint)0);				// filesize placeholder
				bw.Flush();
				bw.Write((uint)ms.Position + 24);		// image start
				bw.Write((uint)0);				// 0 (32bits)		
				bw.Write((uint)0x2c730f8a);		// magic?
				bw.Write((uint)0);				// flags start
				bw.Write((uint)0);				// walk start
				bw.Write((uint)0);				// index start

				var src = surface1.Image;

				var data = src.LockBits(new Rectangle(0, 0, src.Width, src.Height),
					ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

				unsafe
				{
					byte* p = (byte*)data.Scan0;

					for (var v = 0; v < t.Height; v++)
						for (var u = 0; u < t.Width; u++)
						{
							if (t.Cells.ContainsKey(new int2(u + t.Left, v + t.Top)))
							{
								byte* q = p + data.Stride * 24 * (v + t.Top) + 24 * (u + t.Left);
								for (var j = 0; j < 24; j++)
									for (var i = 0; i < 24; i++)
										bw.Write(q[i + j * data.Stride]);
							}
							else
								for (var x = 0; x < 24 * 24; x++)
									bw.Write((byte)0);					/* todo: don't fill with air */
						}
				}

				src.UnlockBits(data);

				bw.Flush();
				var indexStart = ms.Position;
				for (var v = 0; v < t.Height; v++)
					for (var u = 0; u < t.Width; u++)
						bw.Write(t.Cells.ContainsKey(new int2(u + t.Left, v + t.Top))
							? (byte)(u + t.Width * v)
							: (byte)0xff);

				bw.Flush();

				var flagsStart = ms.Position;
				for (var x = 0; x < totalTiles; x++ )
					bw.Write((byte)0);

				bw.Flush();

				var walkStart = ms.Position;
				for (var x = 0; x < totalTiles; x++)
					bw.Write((byte)0x8);

				var bytes = ms.ToArray();
				Array.Copy(BitConverter.GetBytes((uint)bytes.Length), 0, bytes, 12, 4);
				Array.Copy(BitConverter.GetBytes(flagsStart), 0, bytes, 28, 4);
				Array.Copy(BitConverter.GetBytes(walkStart), 0, bytes, 32, 4);
				Array.Copy(BitConverter.GetBytes(indexStart), 0, bytes, 36, 4);

				File.WriteAllBytes(filename, bytes);
			}
		}
	}

	class Template
	{
		public Dictionary<int2, bool> Cells = new Dictionary<int2, bool>();

		public int Left { get { return Cells.Keys.Min(c => c.X); } }
		public int Top { get { return Cells.Keys.Min(c => c.Y); } }

		public int Right { get { return Cells.Keys.Max(c => c.X) + 1; } }
		public int Bottom { get { return Cells.Keys.Max(c => c.Y) + 1; } }

		public int Width { get { return Right - Left; } }
		public int Height { get { return Bottom - Top; } }
	}

	class Surface : Control
	{
		public Bitmap Image;
		public int[,] TerrainTypes;
		public List<Template> Templates = new List<Template>();
		public bool ShowTerrainTypes = true;
		public string InputMode;

		Template CurrentTemplate;

		public Surface()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			UpdateStyles();
		}

		Brush currentBrush = new SolidBrush(Color.FromArgb(60, Color.White));

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Image == null || TerrainTypes == null || Templates == null)
				return;
		
			/* draw the background */
			e.Graphics.DrawImageUnscaled(Image, 0, 0);

			/* draw terrain type overlays */
			if (ShowTerrainTypes)
				for (var i = 0; i <= TerrainTypes.GetUpperBound(0); i++)
					for (var j = 0; j <= TerrainTypes.GetUpperBound(1); j++)
						if (TerrainTypes[i, j] != 0)
						{
							e.Graphics.FillRectangle(Brushes.Black, 24 * i + 10, 24 * j + 10, 10, 10);
							e.Graphics.DrawString(TerrainTypes[i, j].ToString(),
								Font, Brushes.LimeGreen, 24 * i + 10, 24 * j + 10);
						}

			/* draw template outlines */
			foreach (var t in Templates)
			{
				foreach (var c in t.Cells.Keys)
				{
					if (CurrentTemplate == t)
						e.Graphics.FillRectangle(currentBrush, 24 * c.X, 24 * c.Y, 24, 24);

					if (!t.Cells.ContainsKey(c + new int2(-1, 0)))
						e.Graphics.DrawLine(Pens.Red, (24 * c).ToPoint(), (24 * (c + new int2(0, 1))).ToPoint());
					if (!t.Cells.ContainsKey(c + new int2(+1, 0)))
						e.Graphics.DrawLine(Pens.Red, (24 * (c + new int2(1, 0))).ToPoint(), (24 * (c + new int2(1, 1))).ToPoint());
					if (!t.Cells.ContainsKey(c + new int2(0, +1)))
						e.Graphics.DrawLine(Pens.Red, (24 * (c + new int2(0, 1))).ToPoint(), (24 * (c + new int2(1, 1))).ToPoint());
					if (!t.Cells.ContainsKey(c + new int2(0, -1)))
						e.Graphics.DrawLine(Pens.Red, (24 * c).ToPoint(), (24 * (c + new int2(1, 0))).ToPoint());
				}
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			var pos = new int2( e.X / 24, e.Y / 24 );

			if (InputMode == null)
			{
				if (e.Button == MouseButtons.Left)
				{
					CurrentTemplate = Templates.FirstOrDefault(t => t.Cells.ContainsKey(pos));
					if (CurrentTemplate == null)
						Templates.Add(CurrentTemplate = new Template { Cells = new Dictionary<int2, bool> { { pos, true } } });

					Invalidate();
				}

				if (e.Button == MouseButtons.Right)
				{
					Templates.RemoveAll(t => t.Cells.ContainsKey(pos));
					CurrentTemplate = null;
					Invalidate();
				}
			}
			else
			{
				TerrainTypes[pos.X, pos.Y] = int.Parse(InputMode);
				Invalidate();
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var pos = new int2(e.X / 24, e.Y / 24);

			if (InputMode == null)
			{
				if (e.Button == MouseButtons.Left && CurrentTemplate != null)
				{
					if (!CurrentTemplate.Cells.ContainsKey(pos))
					{
						CurrentTemplate.Cells[pos] = true;
						Invalidate();
					}
				}
			}
		}
	}
}
