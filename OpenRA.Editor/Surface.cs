using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.Thirdparty;
using System.Collections;

namespace OpenRA.Editor
{
	class Surface : Control
	{
		public Map Map { get; private set; }
		public TileSet TileSet { get; private set; }
		public Palette Palette { get; private set; }
		int2 Offset;

		BrushTemplate Brush;
		ActorTemplate Actor;
		ResourceTemplate Resource;
		WaypointTemplate Waypoint;

		public bool IsPanning;

		Dictionary<string, ActorTemplate> ActorTemplates = new Dictionary<string, ActorTemplate>();
		Dictionary<int, ResourceTemplate> ResourceTemplates = new Dictionary<int, ResourceTemplate>();

		public void Bind(Map m, TileSet ts, Palette p)
		{
			Map = m;
			TileSet = ts;
			Palette = p;
			Brush = null;
			Chunks.Clear();
		}

		public void SetBrush(BrushTemplate brush) { Actor = null; Brush = brush; Resource = null; Waypoint = null; }
		public void SetActor(ActorTemplate actor) { Brush = null; Actor = actor; Resource = null; Waypoint = null; }
		public void SetResource(ResourceTemplate resource) { Brush = null; Actor = null; Resource = resource; Waypoint = null; }
		public void SetWaypoint(WaypointTemplate waypoint) { Brush = null; Actor = null; Resource = null; Waypoint = waypoint; }

		public void BindActorTemplates(IEnumerable<ActorTemplate> templates)
		{
			ActorTemplates = templates.ToDictionary(a => a.Info.Name.ToLowerInvariant());
		}

		public void BindResourceTemplates(IEnumerable<ResourceTemplate> templates)
		{
			ResourceTemplates = templates.ToDictionary(a => a.Info.ResourceType);
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

		public void Scroll(int2 dx)
		{
			Offset -= dx;
			Invalidate();
		}
		
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var oldMousePos = MousePos;
			MousePos = new int2(e.Location);

			if (e.Button == MouseButtons.Middle || (e.Button != MouseButtons.None && IsPanning))
				Scroll(oldMousePos - MousePos);
			else
			{
				if (e.Button == MouseButtons.Right)
					Erase();

				if (e.Button == MouseButtons.Left)
				Draw();

				Invalidate();
			}
		}

		void FloodFillWithBrush(int2 pos)
		{
			var queue = new Queue<int2>();
			var replace = Map.MapTiles[pos.X, pos.Y];

			queue.Enqueue(pos);
			while (queue.Count > 0)
			{
				var p = queue.Dequeue();
				if (!Map.MapTiles[p.X, p.Y].Equals(replace))
					continue;

				var a = FindEdge(p, new int2(-1, 0), replace);
				var b = FindEdge(p, new int2(1, 0), replace);

				for (var x = a.X; x <= b.X; x++)
				{
					Map.MapTiles[x, p.Y] = new TileReference<ushort, byte> { type = Brush.N, image = (byte)0, index = (byte)0 };
					if (Map.MapTiles[x, p.Y - 1].Equals(replace) && Map.IsInMap(x, p.Y - 1))
						queue.Enqueue(new int2(x, p.Y - 1));
					if (Map.MapTiles[x, p.Y + 1].Equals(replace) && Map.IsInMap(x, p.Y + 1))
						queue.Enqueue(new int2(x, p.Y + 1));
				}
			}

			/* todo: optimize */
			foreach (var ch in Chunks.Values) ch.Dispose();
			Chunks.Clear();
		}

		int2 FindEdge(int2 p, int2 d, TileReference<ushort, byte> replace)
		{
			for (; ; )
			{
				var q = p+d;
				if (!Map.IsInMap(q)) return p;
				if (!Map.MapTiles[q.X, q.Y].Equals(replace)) return p;
				p = q;
			}
		}

		void DrawWithBrush()
		{
			// change the bits in the map
			var tile = TileSet.tiles[Brush.N];
			var template = TileSet.walk[Brush.N];
			var pos = GetBrushLocation();

			if (ModifierKeys == Keys.Shift)
			{
				FloodFillWithBrush(pos);
				return;
			}

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

		int wpid;
		string NextWpid()
		{
			for (; ; )
			{
				var a = "wp{0}".F(wpid++);
				if (!Map.Waypoints.ContainsKey(a))
					return a;
			}
		}

		void DrawWithWaypoint()
		{
			var k = Map.Waypoints.FirstOrDefault(a => a.Value == GetBrushLocation());
			if (k.Key != null) Map.Waypoints.Remove(k.Key);

			Map.Waypoints.Add(NextWpid(), GetBrushLocation());
		}

		void Erase()
		{
			Actor = null;
			Brush = null;
			Resource = null;
			Waypoint = null;

			var key = Map.Actors.FirstOrDefault(a => a.Value.Location == GetBrushLocation());
			if (key.Key != null) Map.Actors.Remove(key.Key);

			if (Map.MapResources[GetBrushLocation().X, GetBrushLocation().Y].type != 0)
			{
				Map.MapResources[GetBrushLocation().X, GetBrushLocation().Y] = new TileReference<byte, byte>();
				var ch = new int2((GetBrushLocation().X) / ChunkSize, (GetBrushLocation().Y) / ChunkSize);
				if (Chunks.ContainsKey(ch))
				{
					Chunks[ch].Dispose();
					Chunks.Remove(ch);
				}
			}

			var k = Map.Waypoints.FirstOrDefault(a => a.Value == GetBrushLocation());
			if (k.Key != null) Map.Waypoints.Remove(k.Key);
		}

		void Draw()
		{
			if (Brush != null) DrawWithBrush();
			if (Actor != null) DrawWithActor();
			if (Resource != null) DrawWithResource();
			if (Waypoint != null) DrawWithWaypoint();
		}

		int id;
		string NextActorName()
		{
			for (; ; )
			{
				var possible = "Actor{0}".F(id++);
				if (!Map.Actors.ContainsKey(possible)) return possible;
			}
		}

		void DrawWithActor()
		{
			if (Map.Actors.Any(a => a.Value.Location == GetBrushLocation()))
				return;

			var owner = "Neutral";
			var id = NextActorName();
			Map.Actors[id] = new ActorReference(id,Actor.Info.Name.ToLowerInvariant(), GetBrushLocation(), owner);
		}

		Random r = new Random();
		void DrawWithResource()
		{
			Map.MapResources[GetBrushLocation().X, GetBrushLocation().Y]
				= new TileReference<byte, byte>
				{
					type = (byte)Resource.Info.ResourceType,
					index = (byte)r.Next(Resource.Info.SpriteNames.Length),
					image = (byte)Resource.Value
				};

			var ch = new int2((GetBrushLocation().X) / ChunkSize, (GetBrushLocation().Y) / ChunkSize);
			if (Chunks.ContainsKey(ch))
			{
				Chunks[ch].Dispose();
				Chunks.Remove(ch);
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (!IsPanning)
			{
				if (e.Button == MouseButtons.Right) Erase();
				if (e.Button == MouseButtons.Left) Draw();
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
						var index = (tr.index < tile.TileBitmapBytes.Count) ? tr.index : (byte)0;
						var rawImage = tile.TileBitmapBytes[index];
						for (var x = 0; x < 24; x++)
							for (var y = 0; y < 24; y++)
								p[ (j * 24 + y) * stride + i * 24 + x ] = Palette.GetColor(rawImage[x + 24 * y]).ToArgb();

						if (Map.MapResources[u * ChunkSize + i, v * ChunkSize + j].type != 0)
						{
							var resourceImage = ResourceTemplates[Map.MapResources[u * ChunkSize + i, v * ChunkSize + j].type].Bitmap;
							var srcdata = resourceImage.LockBits(new Rectangle(0, 0, resourceImage.Width, resourceImage.Height),
								ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

							int* q = (int*)srcdata.Scan0.ToPointer();
							var srcstride = srcdata.Stride >> 2;

							for (var x = 0; x < 24; x++)
								for (var y = 0; y < 24; y++)
								{
									var c = q[y * srcstride + x];
									if ((c & 0xff000000) != 0)	/* quick & dirty, i cbf doing real alpha */
										p[(j * 24 + y) * stride + i * 24 + x] = c;
								}

							resourceImage.UnlockBits(srcdata);
						}
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

		void DrawActorBorder(System.Drawing.Graphics g, int2 p, ActorTemplate t)
		{
			var origin = (24 * p + Offset
					- (t.Centered
					? new int2(t.Bitmap.Width / 2 - 12, t.Bitmap.Height / 2 - 12)
					: int2.Zero)).ToPoint();
			g.DrawRectangle(CordonPen,
				origin.X, origin.Y,
				t.Bitmap.Width, t.Bitmap.Height );
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
				DrawActor(e.Graphics, ar.Value.Location, ActorTemplates[ar.Value.Type]);

			foreach (var wp in Map.Waypoints)
				e.Graphics.DrawRectangle(Pens.LimeGreen, new Rectangle(
					24 * wp.Value.X + Offset.X + 4,
					24 * wp.Value.Y + Offset.Y + 4,
					16, 16));

			if (Brush != null)
				e.Graphics.DrawImage(Brush.Bitmap,
					(24 * GetBrushLocation() + Offset).ToPoint());

			if (Actor != null)
				DrawActor(e.Graphics, GetBrushLocation(), Actor);

			if (Resource != null)
				e.Graphics.DrawImage(Resource.Bitmap,
					(24 * GetBrushLocation() + Offset).ToPoint());

			if (Waypoint != null)
				e.Graphics.DrawRectangle(Pens.LimeGreen, new Rectangle(
					24 * GetBrushLocation().X + Offset.X + 4,
					24 * GetBrushLocation().Y + Offset.Y + 4,
					16, 16));

			if (Brush == null && Actor == null && Resource == null)
			{
				var x = Map.Actors.FirstOrDefault(a => a.Value.Location == GetBrushLocation());
				if (x.Key != null)
					DrawActorBorder(e.Graphics, x.Value.Location, ActorTemplates[x.Value.Type]);
			}
		}
	}
}