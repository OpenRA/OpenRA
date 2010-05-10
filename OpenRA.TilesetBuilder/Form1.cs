using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

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

			// export palette (use the embedded palette)
			var p = surface1.Image.Palette.Entries.ToList();
			while (p.Count < 256) p.Add(Color.Black);				// pad the palette out with extra blacks
			var paletteData = p.Take(256).SelectMany(c => new byte[] { (byte)(c.R >> 2), (byte)(c.G >> 2), (byte)(c.B >> 2) }).ToArray();
			File.WriteAllBytes(Path.Combine(dir, "terrain.pal"), paletteData);

			// todo: write out a TMP for each template
			// todo: write out a tileset definition
			// todo: write out a templates ini
		}
	}

	class Template
	{
		public Dictionary<int2, bool> Cells = new Dictionary<int2, bool>();
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
