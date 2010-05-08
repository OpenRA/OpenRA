using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenRA.FileFormats;
using System;

namespace OpenRA.Editor
{
	class Surface : Control
	{
		public Map Map { get; set; }
		public TileSet TileSet { get; set; }
		public Palette Palette { get; set; }
		public int2 Offset { get; set; }
		public Pair<ushort, Bitmap> Brush { get; set; }

		Dictionary<int2, Bitmap> Chunks = new Dictionary<int2, Bitmap>();

		public Surface()
			: base()
		{
			BackColor = Color.Black;

			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			UpdateStyles();
		}

		static readonly Pen RedPen = new Pen(Color.Red);
		int2 MousePos;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var oldMousePos = MousePos;
			MousePos = new int2(e.Location);

			if (e.Button == MouseButtons.Middle)
			{
				Offset += MousePos - oldMousePos;
				Invalidate();
			}
			else
				if (Brush.Second != null)
					Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Right)
				Brush = Pair.New((ushort)0, null as Bitmap);

			Invalidate();
		}

		const int ChunkSize = 8;		// 8x8 chunks ==> 192x192 bitmaps.

		Bitmap RenderChunk(int u, int v)
		{
			var bitmap = new Bitmap(ChunkSize * 24, ChunkSize * 24);
			bitmap.SetPixel(0, 0, Color.Green);

			var hx = Math.Min(Map.Width - u * ChunkSize, ChunkSize);
			var hy = Math.Min(Map.Height - v * ChunkSize, ChunkSize);

			for( var i = 0; i < hx; i++ )
				for (var j = 0; j < hy; j++)
				{
					var tr = Map.MapTiles[u * ChunkSize + i, v * ChunkSize + j];
					var tile = TileSet.tiles[tr.type];

					var index = (tr.index < tile.TileBitmapBytes.Count) ? tr.index : 0;
					var rawImage = tile.TileBitmapBytes[index];
						for (var x = 0; x < 24; x++)
							for (var y = 0; y < 24; y++)
								bitmap.SetPixel(i * 24 + x, j * 24 + y, Palette.GetColor(rawImage[x + 24 * y]));
				}

			return bitmap;
		}

		int2 GetBrushLocation()
		{
			var v = MousePos - Offset;
			return new int2(v.X / 24, v.Y / 24);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Map == null) return;
			if (TileSet == null) return;

			for( var u = Map.TopLeft.X - Map.TopLeft.X % ChunkSize; u < Map.BottomRight.X; u += ChunkSize )
				for (var v = Map.TopLeft.Y - Map.TopLeft.Y % ChunkSize; v < Map.BottomRight.Y; v += ChunkSize)
				{
					var x = new int2(u/ChunkSize,v/ChunkSize);
					if (!Chunks.ContainsKey(x)) Chunks[x] = RenderChunk(u / ChunkSize, v / ChunkSize);
					e.Graphics.DrawImage(Chunks[x], (24 * ChunkSize * x + Offset).ToPoint());
				}

			if (Brush.Second != null)
				e.Graphics.DrawImage(Brush.Second,
					(24 * GetBrushLocation() + Offset).ToPoint());
		}
	}
}