using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenRA.FileFormats;
using System.Drawing;

namespace OpenRA.Editor
{
	class Surface : Control
	{
		public Map Map { get; set; }
		public TileSet TileSet { get; set; }
		public int2 Offset { get; set; }

		public Surface()
			: base()
		{
			BackColor = Color.Black;

			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			UpdateStyles();
		}

		public const int CellSize = 24;
		static readonly Pen RedPen = new Pen(Color.Red);

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Map == null) return;
			if (TileSet == null) return;

			var n = (ushort)14;

			var template = TileSet.walk[n];
			var tile = TileSet.tiles[n];

			for( var u = 0; u < template.Size.X; u++ )
				for( var v = 0; v < template.Size.Y; v++ )
					if (template.TerrainType.ContainsKey(u + v * template.Size.X))
					{
						e.Graphics.DrawRectangle(RedPen, new Rectangle(CellSize * u, CellSize * v, CellSize, CellSize));
					}
		}
	}
}