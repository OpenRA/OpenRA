using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenRA.FileFormats;
using System;
using System.Drawing.Imaging;

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

		static readonly Pen CordonPen = new Pen(Color.Red);
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

			if (e.Button == MouseButtons.Left && Brush.Second != null)
			{
				// change the bits in the map
				var template = TileSet.walk[Brush.First];
				var pos = GetBrushLocation();

				for( var u = 0; u < template.Size.X; u++ )
					for (var v = 0; v < template.Size.Y; v++)
					{
						if (Map.IsInMap(new int2(u, v) + pos))
						{
							var z = u + v * template.Size.X;
							if (template.TerrainType.ContainsKey(z))
								Map.MapTiles[u + pos.X, v + pos.Y] =
									new TileReference<ushort, byte> { type = Brush.First, image = (byte)z, index = (byte)z };

							var ch = new int2( (pos.X + u) / ChunkSize, (pos.Y + v) / ChunkSize);
							if (Chunks.ContainsKey(ch))
							{
								Chunks[ch].Dispose();
								Chunks.Remove(ch);
							}
						}
					}
			}

			Invalidate();
		}

		const int ChunkSize = 8;		// 8x8 chunks ==> 192x192 bitmaps.

		Bitmap RenderChunk(int u, int v)
		{
			var bitmap = new Bitmap(ChunkSize * 24, ChunkSize * 24);
			bitmap.SetPixel(0, 0, Color.Green);

			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), 
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* p = (int*)data.Scan0.ToPointer();
				var stride = data.Stride >> 2;

				for (var i = 0; i < ChunkSize; i++)
					for (var j = 0; j < ChunkSize; j++)
					{
						var tr = Map.MapTiles[u * ChunkSize + i, v * ChunkSize + j];
						var tile = TileSet.tiles[tr.type];

						var index = (tr.index < tile.TileBitmapBytes.Count) ? tr.index : 0;
						var rawImage = tile.TileBitmapBytes[index];
						for (var x = 0; x < 24; x++)
							for (var y = 0; y < 24; y++)
								p[ (j * 24 + y) * stride + i * 24 + x ] = Palette.GetColor(rawImage[x + 24 * y]).ToArgb();
					}
			}

			bitmap.UnlockBits(data);
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

			for( var u = Map.TopLeft.X - Map.TopLeft.X % ChunkSize; u <= Map.BottomRight.X; u += ChunkSize )
				for (var v = Map.TopLeft.Y - Map.TopLeft.Y % ChunkSize; v <= Map.BottomRight.Y; v += ChunkSize)
				{
					var x = new int2(u/ChunkSize,v/ChunkSize);
					if (!Chunks.ContainsKey(x)) Chunks[x] = RenderChunk(u / ChunkSize, v / ChunkSize);
					e.Graphics.DrawImage(Chunks[x], (24 * ChunkSize * x + Offset).ToPoint());
				}

			e.Graphics.DrawRectangle(CordonPen,
				new Rectangle(Map.XOffset * 24 + Offset.X, Map.YOffset * 24 + Offset.Y, Map.Width * 24, Map.Height * 24));

			if (Brush.Second != null)
				e.Graphics.DrawImage(Brush.Second,
					(24 * GetBrushLocation() + Offset).ToPoint());
		}
	}
}