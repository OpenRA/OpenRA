using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace OpenRA.TilesetBuilder
{
	public partial class Form1 : Form
	{
		public Form1( string src )
		{
			InitializeComponent();

			surface1.Image = (Bitmap)Image.FromFile(src);
			surface1.TerrainTypes = new int[surface1.Image.Width / 24, surface1.Image.Height / 24];		/* all passable by default */
			surface1.Templates = new List<Template>();

			/* todo: load stuff from previous session */
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

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var pos = new int2(e.X / 24, e.Y / 24);

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
