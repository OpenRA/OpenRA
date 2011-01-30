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

using SGraphics = System.Drawing.Graphics;

namespace OpenRA.Editor
{
	class Surface : Control
	{
		public Map Map { get; private set; }
		public TileSet TileSet { get; private set; }
		public Palette Palette { get; private set; }
		public int2 Offset;

		public int2 GetOffset() { return Offset; }

		public float Zoom = 1.0f;

		ITool Tool;
		
		public bool IsPanning;
		public event Action AfterChange = () => { };
		public event Action<string> MousePositionChanged = _ => { };

		Dictionary<string, ActorTemplate> ActorTemplates = new Dictionary<string, ActorTemplate>();
		Dictionary<int, ResourceTemplate> ResourceTemplates = new Dictionary<int, ResourceTemplate>();

		public Keys GetModifiers() { return ModifierKeys; }

		public void Bind(Map m, TileSet ts, Palette p)
		{
			Map = m;
			TileSet = ts;
			Palette = p;
			PlayerPalettes = null;
			Chunks.Clear();
			Tool = null;
		}

		public void SetTool(ITool tool) { Tool = tool; }

		public void BindActorTemplates(IEnumerable<ActorTemplate> templates)
		{
			ActorTemplates = templates.ToDictionary(a => a.Info.Name.ToLowerInvariant());
		}

		public void BindResourceTemplates(IEnumerable<ResourceTemplate> templates)
		{
			ResourceTemplates = templates.ToDictionary(a => a.Info.ResourceType);
		}

		public Dictionary<int2, Bitmap> Chunks = new Dictionary<int2, Bitmap>();

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

		int wpid;
		public string NextWpid()
		{
			for (; ; )
			{
				var a = "wp{0}".F(wpid++);
				if (!Map.Waypoints.ContainsKey(a))
					return a;
			}
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

			Tool = null;
			
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
			if (Tool != null) Tool.Apply(this);
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

		public const int ChunkSize = 8;		// 8x8 chunks ==> 192x192 bitmaps.

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

		public int2 GetBrushLocation()
		{
			var vX = (int)Math.Floor((MousePos.X - Offset.X) / Zoom);
			var vY = (int)Math.Floor((MousePos.Y - Offset.Y) / Zoom);
			return new int2(vX / TileSet.TileSize, vY / TileSet.TileSize);
		}

		public void DrawActor(SGraphics g, int2 p, ActorTemplate t, ColorPalette cp)
		{
			var centered = t.Appearance == null || !t.Appearance.RelativeToTopLeft;
			DrawImage(g, t.Bitmap, p, centered, cp);
		}

		public void DrawImage(SGraphics g, Bitmap bmp, int2 location, bool centered, ColorPalette cp)
		{
			float OffsetX = centered ? bmp.Width / 2 - TileSet.TileSize / 2 : 0;
			float DrawX = TileSet.TileSize * location.X * Zoom + Offset.X - OffsetX;

			float OffsetY = centered ? bmp.Height / 2 - TileSet.TileSize / 2 : 0;
			float DrawY = TileSet.TileSize * location.Y * Zoom + Offset.Y - OffsetY;

			float width = bmp.Width * Zoom;
			float height = bmp.Height * Zoom;
			RectangleF sourceRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
			RectangleF destRect = new RectangleF(DrawX, DrawY, width, height);

			var restorePalette = bmp.Palette;
			if (cp != null) bmp.Palette = cp;
			g.DrawImage(bmp, destRect, sourceRect, GraphicsUnit.Pixel);
			if (cp != null) bmp.Palette = restorePalette;
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
			var remap = new PlayerColorRemap(pr.ColorRamp, pcpi.PaletteFormat);
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
				Map.Bounds.Left * TileSet.TileSize * Zoom + Offset.X,
				Map.Bounds.Top * TileSet.TileSize * Zoom + Offset.Y,
				Map.Bounds.Width * TileSet.TileSize * Zoom,
				Map.Bounds.Height * TileSet.TileSize * Zoom);

			foreach (var ar in Map.Actors)
				DrawActor(e.Graphics, ar.Value.Location(), ActorTemplates[ar.Value.Type],
					GetPaletteForActor(ar.Value));

			foreach (var wp in Map.Waypoints)
				e.Graphics.DrawRectangle(Pens.LimeGreen,
					TileSet.TileSize * wp.Value.X * Zoom + Offset.X + 4,
					TileSet.TileSize * wp.Value.Y * Zoom + Offset.Y + 4,
					(TileSet.TileSize - 8) * Zoom, (TileSet.TileSize - 8) * Zoom);

			if (Tool != null)
				Tool.Preview(this, e.Graphics);
				
			if (Tool == null)
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