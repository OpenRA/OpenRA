#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	class Surface : Control
	{
		public Map Map { get; private set; }
		public TileSet TileSet { get; private set; }
		public Palette Palette { get; private set; }
		int2 Offset;

		float Zoom = 1.0f;

		BrushTemplate Brush;
		ActorTemplate Actor;
		ResourceTemplate Resource;
		WaypointTemplate Waypoint;

		public bool IsPanning;
		public event Action AfterChange = () => { };
		public event Action<string> MousePositionChanged = _ => { };

		Dictionary<string, ActorTemplate> ActorTemplates = new Dictionary<string, ActorTemplate>();
		Dictionary<int, ResourceTemplate> ResourceTemplates = new Dictionary<int, ResourceTemplate>();

		public void Bind(Map m, TileSet ts, Palette p)
		{
			Map = m;
			TileSet = ts;
			Palette = p;
			Brush = null;
			PlayerPalettes = null;
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

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			if (Map == null) return;

			Zoom *= e.Delta > 0 ? 4.0f / 3.0f : .75f;

			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			this.Parent.Focus();

			Invalidate();
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseLeave(e);

			this.Focus();

			Invalidate();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (Map == null) return;

			var oldMousePos = MousePos;
			MousePos = new int2(e.Location);
			MousePositionChanged(GetBrushLocation().ToString());

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

			AfterChange();
		}

		int2 FindEdge(int2 p, int2 d, TileReference<ushort, byte> replace)
		{
			for (; ; )
			{
				var q = p + d;
				if (!Map.IsInMap(q)) return p;
				if (!Map.MapTiles[q.X, q.Y].Equals(replace)) return p;
				p = q;
			}
		}

		void DrawWithBrush()
		{
			// change the bits in the map
			var tile = TileSet.Tiles[Brush.N];
			var template = TileSet.Templates[Brush.N];
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
								new TileReference<ushort, byte>
								{
									type = Brush.N,
									index = template.PickAny ? byte.MaxValue : (byte)z,
									image = template.PickAny ? (byte)((u + pos.X) % 4 + ((v + pos.Y) % 4) * 4) : (byte)z,
								};

						var ch = new int2((pos.X + u) / ChunkSize, (pos.Y + v) / ChunkSize);
						if (Chunks.ContainsKey(ch))
						{
							Chunks[ch].Dispose();
							Chunks.Remove(ch);
						}
					}
				}

			AfterChange();
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

			AfterChange();
		}

		void Erase()
		{
			// Crash preventing
			var BrushLocation = GetBrushLocation();

			if (Map == null || BrushLocation.X >= Map.MapSize.X ||
				BrushLocation.Y >= Map.MapSize.Y ||
				BrushLocation.X < 0 ||
				BrushLocation.Y < 0)
				return;

			Actor = null;
			Brush = null;
			Resource = null;
			Waypoint = null;

			var key = Map.Actors.FirstOrDefault(a => a.Value.Location() == BrushLocation);
			if (key.Key != null) Map.Actors.Remove(key.Key);

			if (Map.MapResources[BrushLocation.X, BrushLocation.Y].type != 0)
			{
				Map.MapResources[BrushLocation.X, BrushLocation.Y] = new TileReference<byte, byte>();
				var ch = new int2((BrushLocation.X) / ChunkSize, (BrushLocation.Y) / ChunkSize);
				if (Chunks.ContainsKey(ch))
				{
					Chunks[ch].Dispose();
					Chunks.Remove(ch);
				}
			}

			var k = Map.Waypoints.FirstOrDefault(a => a.Value == BrushLocation);
			if (k.Key != null) Map.Waypoints.Remove(k.Key);

			AfterChange();
		}

		void Draw()
		{
			if (Brush != null) DrawWithBrush();
			if (Actor != null) DrawWithActor();
			if (Resource != null) DrawWithResource();
			if (Waypoint != null) DrawWithWaypoint();

			AfterChange();
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
			if (Map.Actors.Any(a => a.Value.Location() == GetBrushLocation()))
				return;

			var owner = "Neutral";
			var id = NextActorName();
			Map.Actors[id] = new ActorReference(Actor.Info.Name.ToLowerInvariant())
			{
				new LocationInit( GetBrushLocation() ),
				new OwnerInit( owner)
			};

			AfterChange();
		}

		System.Random r = new System.Random();
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

			AfterChange();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (Map == null) return;

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

			var bitmap = new Bitmap(ChunkSize * TileSet.TileSize, ChunkSize * TileSet.TileSize);
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
						var tile = TileSet.Tiles[tr.type];
						var index = (tr.image < tile.TileBitmapBytes.Count) ? tr.image : (byte)0;
						var rawImage = tile.TileBitmapBytes[index];
						for (var x = 0; x < TileSet.TileSize; x++)
							for (var y = 0; y < TileSet.TileSize; y++)
								p[(j * TileSet.TileSize + y) * stride + i * TileSet.TileSize + x] = Palette.GetColor(rawImage[x + TileSet.TileSize * y]).ToArgb();

						if (Map.MapResources[u * ChunkSize + i, v * ChunkSize + j].type != 0)
						{
							var resourceImage = ResourceTemplates[Map.MapResources[u * ChunkSize + i, v * ChunkSize + j].type].Bitmap;
							var srcdata = resourceImage.LockBits(new Rectangle(0, 0, resourceImage.Width, resourceImage.Height),
								ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

							int* q = (int*)srcdata.Scan0.ToPointer();
							var srcstride = srcdata.Stride >> 2;

							for (var x = 0; x < TileSet.TileSize; x++)
								for (var y = 0; y < TileSet.TileSize; y++)
								{
									var c = q[y * srcstride + x];
									if ((c & 0xff000000) != 0)	/* quick & dirty, i cbf doing real alpha */
										p[(j * TileSet.TileSize + y) * stride + i * TileSet.TileSize + x] = c;
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
			var vX = (int)Math.Floor((MousePos.X - Offset.X) / Zoom);
			var vY = (int)Math.Floor((MousePos.Y - Offset.Y) / Zoom);
			return new int2(vX / TileSet.TileSize, vY / TileSet.TileSize);
		}

		void DrawActor(System.Drawing.Graphics g, int2 p, ActorTemplate t, ColorPalette cp)
		{
			var centered = t.Appearance == null || !t.Appearance.RelativeToTopLeft;

			float OffsetX = centered ? t.Bitmap.Width / 2 - TileSet.TileSize / 2 : 0;
			float DrawX = TileSet.TileSize * p.X * Zoom + Offset.X - OffsetX;

			float OffsetY = centered ? t.Bitmap.Height / 2 - TileSet.TileSize / 2 : 0;
			float DrawY = TileSet.TileSize * p.Y * Zoom + Offset.Y - OffsetY;

			float width = t.Bitmap.Width * Zoom;
			float height = t.Bitmap.Height * Zoom;
			RectangleF sourceRect = new RectangleF(0, 0, t.Bitmap.Width, t.Bitmap.Height);
			RectangleF destRect = new RectangleF(DrawX, DrawY, width, height);

			var restorePalette = t.Bitmap.Palette;
			if (cp != null) t.Bitmap.Palette = cp;
			g.DrawImage(t.Bitmap, destRect, sourceRect, GraphicsUnit.Pixel);
			if (cp != null) t.Bitmap.Palette = restorePalette;
		}

		void DrawImage(System.Drawing.Graphics g, Bitmap bmp, int2 location)
		{
			float OffsetX = bmp.Width / 2 - TileSet.TileSize / 2;
			float DrawX = TileSet.TileSize * location.X * Zoom + Offset.X - OffsetX;

			float OffsetY = bmp.Height / 2 - TileSet.TileSize / 2;
			float DrawY = TileSet.TileSize * location.Y * Zoom + Offset.Y - OffsetY;

			float width = bmp.Width * Zoom;
			float height = bmp.Height * Zoom;
			RectangleF sourceRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
			RectangleF destRect = new RectangleF(DrawX, DrawY, width, height);
			g.DrawImage(bmp, destRect, sourceRect, GraphicsUnit.Pixel);
		}

		void DrawActorBorder(System.Drawing.Graphics g, int2 p, ActorTemplate t)
		{
			var centered = t.Appearance == null || !t.Appearance.RelativeToTopLeft;

			float OffsetX = centered ? t.Bitmap.Width / 2 - TileSet.TileSize / 2 : 0;
			float DrawX = TileSet.TileSize * p.X * Zoom + Offset.X - OffsetX;

			float OffsetY = centered ? t.Bitmap.Height / 2 - TileSet.TileSize / 2 : 0;
			float DrawY = TileSet.TileSize * p.Y * Zoom + Offset.Y - OffsetY;

			g.DrawRectangle(CordonPen,
				DrawX, DrawY,
				t.Bitmap.Width * Zoom, t.Bitmap.Height * Zoom);
		}

		ColorPalette GetPaletteForPlayer(string name)
		{
			var pr = Map.Players[name];
			var pcpi = Rules.Info["player"].Traits.Get<PlayerColorPaletteInfo>();
			var remap = new PlayerColorRemap(pr.Color, pr.Color2, pcpi.PaletteFormat);
			return RenderUtils.MakeSystemPalette(new Palette(Palette, remap));
		}

		Cache<string, ColorPalette> PlayerPalettes;

		ColorPalette GetPaletteForActor(ActorReference ar)
		{
			if (PlayerPalettes == null)
				PlayerPalettes = new Cache<string, ColorPalette>(GetPaletteForPlayer);

			var ownerInit = ar.InitDict.GetOrDefault<OwnerInit>();
			if (ownerInit == null)
				return null;

			return PlayerPalettes[ownerInit.PlayerName];
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Map == null) return;
			if (TileSet == null) return;

			for (var u = 0; u < Map.MapSize.X; u += ChunkSize)
				for (var v = 0; v < Map.MapSize.Y; v += ChunkSize)
				{
					var x = new int2(u / ChunkSize, v / ChunkSize);
					if (!Chunks.ContainsKey(x)) Chunks[x] = RenderChunk(u / ChunkSize, v / ChunkSize);

					Bitmap bmp = Chunks[x];

					float DrawX = TileSet.TileSize * 1f * (float)ChunkSize * (float)x.X * Zoom + Offset.X;
					float DrawY = TileSet.TileSize * 1f * (float)ChunkSize * (float)x.Y * Zoom + Offset.Y;
					RectangleF sourceRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
					RectangleF destRect = new RectangleF(DrawX, DrawY, bmp.Width * Zoom, bmp.Height * Zoom);
					e.Graphics.DrawImage(bmp, destRect, sourceRect, GraphicsUnit.Pixel);
				}

			e.Graphics.DrawRectangle(CordonPen,
				Map.TopLeft.X * TileSet.TileSize * Zoom + Offset.X,
				Map.TopLeft.Y * TileSet.TileSize * Zoom + Offset.Y,
				Map.Width * TileSet.TileSize * Zoom,
				Map.Height * TileSet.TileSize * Zoom);

			foreach (var ar in Map.Actors)
				DrawActor(e.Graphics, ar.Value.Location(), ActorTemplates[ar.Value.Type],
					GetPaletteForActor(ar.Value));

			foreach (var wp in Map.Waypoints)
				e.Graphics.DrawRectangle(Pens.LimeGreen,
					TileSet.TileSize * wp.Value.X * Zoom + Offset.X + 4,
					TileSet.TileSize * wp.Value.Y * Zoom + Offset.Y + 4,
					(TileSet.TileSize - 8) * Zoom, (TileSet.TileSize - 8) * Zoom);

			if (Brush != null)
				e.Graphics.DrawImage(Brush.Bitmap,
					TileSet.TileSize * GetBrushLocation().X * Zoom + Offset.X,
					TileSet.TileSize * GetBrushLocation().Y * Zoom + Offset.Y,
					Brush.Bitmap.Width * Zoom,
					Brush.Bitmap.Height * Zoom);

			if (Actor != null)
				DrawActor(e.Graphics, GetBrushLocation(), Actor, null);	/* todo: include the player 
																		 * in the brush so we can color new buildings too */

			if (Resource != null)
				DrawImage(e.Graphics, Resource.Bitmap, GetBrushLocation());

			if (Waypoint != null)
				e.Graphics.DrawRectangle(Pens.LimeGreen,
					TileSet.TileSize * GetBrushLocation().X * Zoom + Offset.X + 4,
					TileSet.TileSize * GetBrushLocation().Y * Zoom + Offset.Y + 4,
					(TileSet.TileSize - 8) * Zoom, (TileSet.TileSize - 8) * Zoom);

			if (Brush == null && Actor == null && Resource == null)
			{
				var x = Map.Actors.FirstOrDefault(a => a.Value.Location() == GetBrushLocation());
				if (x.Key != null)
					DrawActorBorder(e.Graphics, x.Value.Location(), ActorTemplates[x.Value.Type]);
			}
		}
	}

	static class ActorReferenceExts
	{
		public static int2 Location(this ActorReference ar)
		{
			return ar.InitDict.Get<LocationInit>().value;
		}
	}
}