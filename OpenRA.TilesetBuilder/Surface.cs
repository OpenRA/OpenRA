using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenRA.TilesetBuilder
{
	class Surface : Control
	{
		public Bitmap Image;
		private ImageList ImagesListControl;
		public int[,] TerrainTypes;
		public List<Template> Templates = new List<Template>();
		private bool bShowTerrainTypes;
		public string InputMode;
		public Bitmap[] icon;
		public int TileSize;
		public int TilesPerRow;
		//private System.ComponentModel.IContainer components;

		
		public event Action<int, int, int> UpdateMouseTilePosition = (x, y, t) => { };

		Template CurrentTemplate;

		public bool ShowTerrainTypes
		{
			get { return bShowTerrainTypes; }
			set { bShowTerrainTypes = value; }
		}

		public ImageList ImagesList
		{
			get { return ImagesListControl; }
			set { ImagesListControl = value; }
		}

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
			{
				for (var i = 0; i <= TerrainTypes.GetUpperBound(0); i++)
					for (var j = 0; j <= TerrainTypes.GetUpperBound(1); j++)
						if (TerrainTypes[i, j] != 0)
						{
							//e.Graphics.FillRectangle(Brushes.Black, TileSize * i + 8, TileSize * j + 8, 16, 16);

							e.Graphics.DrawImage(icon[TerrainTypes[i, j]], TileSize * i + 8, TileSize * j + 8, 16, 16);

							//e.Graphics.DrawString(TerrainTypes[i, j].ToString(),
							//Font, Brushes.LimeGreen, TileSize * i + 10, TileSize * j + 10);
						}
			}

			/* draw template outlines */
			foreach (var t in Templates)
			{
				System.Drawing.Pen pen = Pens.White;

				foreach (var c in t.Cells.Keys)
				{
					if (CurrentTemplate == t)
						e.Graphics.FillRectangle(currentBrush, TileSize * c.X, TileSize * c.Y, TileSize, TileSize);

					if (!t.Cells.ContainsKey(c + new int2(-1, 0)))
						e.Graphics.DrawLine(pen, (TileSize * c).ToPoint(), (TileSize * (c + new int2(0, 1))).ToPoint());
					if (!t.Cells.ContainsKey(c + new int2(+1, 0)))
						e.Graphics.DrawLine(pen, (TileSize * (c + new int2(1, 0))).ToPoint(), (TileSize * (c + new int2(1, 1))).ToPoint());
					if (!t.Cells.ContainsKey(c + new int2(0, +1)))
						e.Graphics.DrawLine(pen, (TileSize * (c + new int2(0, 1))).ToPoint(), (TileSize * (c + new int2(1, 1))).ToPoint());
					if (!t.Cells.ContainsKey(c + new int2(0, -1)))
						e.Graphics.DrawLine(pen, (TileSize * c).ToPoint(), (TileSize * (c + new int2(1, 0))).ToPoint());
				}
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			var pos = new int2(e.X / TileSize, e.Y / TileSize);

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
			var pos = new int2(e.X / TileSize, e.Y / TileSize);

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

			UpdateMouseTilePosition(pos.X, pos.Y, (pos.Y * TilesPerRow) + pos.X);
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			this.ResumeLayout(false);
		}
	}
}
