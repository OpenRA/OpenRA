using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Editor
{
	class Surface : Control
	{
		public Map Map { get; private set; }
		public TileSet TileSet { get; private set; }
		public Palette Palette { get; private set; }
		int2 Offset;

		public BrushTemplate Brush;
		public ActorTemplate Actor;

		Dictionary<string, ActorTemplate> ActorTemplates = new Dictionary<string, ActorTemplate>();

		public void Bind(Map m, TileSet ts, Palette p)
		{
			Map = m;
			TileSet = ts;
			Palette = p;
			Brush = null;
			Chunks.Clear();
		}

		public void SetBrush(BrushTemplate brush) { Actor = null; Brush = brush; }
		public void SetActor(ActorTemplate actor) { Brush = null; Actor = actor; }

		public void BindActorTemplates(IEnumerable<ActorTemplate> templates)
		{
			ActorTemplates = templates.ToDictionary(a => a.Info.Name.ToLowerInvariant());
		}

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
			{
				if (e.Button == MouseButtons.Left && Brush != null)
					DrawWithBrush();
				if (e.Button == MouseButtons.Left && Actor != null)
					DrawWithActor();

				if (Brush != null || Actor != null)
					Invalidate();
			}
		}

		void DrawWithBrush()
		{
			// change the bits in the map
			var tile = TileSet.tiles[Brush.N];
			var template = TileSet.walk[Brush.N];
			var pos = GetBrushLocation();

			for (var u = 0; u < template.Size.X; u++)
				for (var v = 0; v < template.Size.Y; v++)
				{
					if (Map.IsInMap(new int2(u, v) + pos))
					{
						var z = u + v * template.Size.X;
						if (tile.TileBitmapBytes[z] != null)
							Map.MapTiles[u + pos.X, v + pos.Y] =
								new TileReference<ushort, byte> { type = Brush.N, image = (byte)z, index = (byte)z };

						var ch = new int2((pos.X + u) / ChunkSize, (pos.Y + v) / ChunkSize);
						if (Chunks.ContainsKey(ch))
						{
							Chunks[ch].Dispose();
							Chunks.Remove(ch);
						}
					}
				}
		}

		void DrawWithActor()
		{
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Right)
			{
				Actor = null;
				Brush = null;
			}

			if (e.Button == MouseButtons.Left && Brush != null)
				DrawWithBrush();
			if (e.Button == MouseButtons.Left && Actor != null)
				DrawWithActor();

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

						var index = (tr.index < tile.TileBitmapBytes.Count) ? tr.index : (byte)0;
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

		void DrawActor(System.Drawing.Graphics g, int2 p, ActorTemplate t)
		{
			g.DrawImage(t.Bitmap,
					((24 * p + Offset
					- (t.Centered
					? new int2(t.Bitmap.Width / 2 - 12, t.Bitmap.Height / 2 - 12)
					: int2.Zero)).ToPoint()));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Map == null) return;
			if (TileSet == null) return;

			for( var u = 0; u < Map.MapSize.X; u += ChunkSize )
				for (var v = 0; v < Map.MapSize.Y; v += ChunkSize)
				{
					var x = new int2(u/ChunkSize,v/ChunkSize);
					if (!Chunks.ContainsKey(x)) Chunks[x] = RenderChunk(u / ChunkSize, v / ChunkSize);
					e.Graphics.DrawImage(Chunks[x], (24 * ChunkSize * x + Offset).ToPoint());
				}

			e.Graphics.DrawRectangle(CordonPen,
				new Rectangle(Map.XOffset * 24 + Offset.X, Map.YOffset * 24 + Offset.Y, Map.Width * 24, Map.Height * 24));

			foreach (var ar in Map.Actors)
				DrawActor(e.Graphics, ar.Value.Location, ActorTemplates[ar.Value.Name]);

			if (Brush != null)
				e.Graphics.DrawImage(Brush.Bitmap,
					(24 * GetBrushLocation() + Offset).ToPoint());

			if (Actor != null)
				DrawActor(e.Graphics, GetBrushLocation(), Actor);
		}
	}
}