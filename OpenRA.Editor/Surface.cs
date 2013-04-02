#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		public Palette PlayerPalette { get; private set; }
		public int2 Offset;

		public int2 GetOffset() { return Offset; }

		public float Zoom = 1.0f;

		ITool Tool;

		public bool IsPanning;
		public bool ShowActorNames;
		public bool ShowGrid;
		public bool ShowRuler;

		public bool IsPaste { get { return TileSelection != null && ResourceSelection != null; } }
		public TileReference<ushort, byte>[,] TileSelection;
		public TileReference<byte, byte>[,] ResourceSelection;
		public CPos SelectionStart;
		public CPos SelectionEnd;

		public string NewActorOwner;

		public event Action AfterChange = () => { };
		public event Action<string> MousePositionChanged = _ => { };
		public event Action<KeyValuePair<string, ActorReference>> ActorDoubleClicked = _ => { };

		Dictionary<string, ActorTemplate> ActorTemplates = new Dictionary<string, ActorTemplate>();
		public Dictionary<int, ResourceTemplate> ResourceTemplates = new Dictionary<int, ResourceTemplate>();

		static readonly Font MarkerFont = new Font(FontFamily.GenericSansSerif, 12.0f, FontStyle.Regular);
		static readonly SolidBrush TextBrush = new SolidBrush(Color.Red);

		public Keys GetModifiers() { return ModifierKeys; }

		public void Bind(Map m, TileSet ts, Palette p, Palette pp)
		{
			Map = m;
			TileSet = ts;
			Palette = p;
			PlayerPalette = pp;
			PlayerPalettes = null;
			Chunks.Clear();
			Tool = null;
		}

		public void SetTool(ITool tool) { Tool = tool; ClearSelection(); }

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

		static readonly Pen SelectionPen = new Pen(Color.Blue);
		static readonly Pen PastePen = new Pen(Color.Green);
		static readonly Pen CordonPen = new Pen(Color.Red);
		int2 MousePos;

		public void Scroll(int2 dx)
		{
			Offset -= dx;
			Invalidate();
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			base.OnDoubleClick(e);

			var x = Map.Actors.Value.FirstOrDefault(a => a.Value.Location() == GetBrushLocation());
			if (x.Key != null)
				ActorDoubleClicked(x);
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

		void Erase()
		{
			// Crash preventing
			var brushLocation = GetBrushLocation();

			if (Map == null || brushLocation.X >= Map.MapSize.X ||
				brushLocation.Y >= Map.MapSize.Y ||
				brushLocation.X < 0 ||
				brushLocation.Y < 0)
				return;

			Tool = null;

			var key = Map.Actors.Value.FirstOrDefault(a => a.Value.Location() == brushLocation);
			if (key.Key != null) Map.Actors.Value.Remove(key.Key);

			if (Map.MapResources.Value[brushLocation.X, brushLocation.Y].type != 0)
			{
				Map.MapResources.Value[brushLocation.X, brushLocation.Y] = new TileReference<byte, byte>();
				var ch = new int2((brushLocation.X) / ChunkSize, (brushLocation.Y) / ChunkSize);
				if (Chunks.ContainsKey(ch))
				{
					Chunks[ch].Dispose();
					Chunks.Remove(ch);
				}
			}

			AfterChange();
			ClearSelection();
		}

		void Draw()
		{
			if (Tool != null)
			{
				Tool.Apply(this);
				AfterChange();
			}
			else if (IsPaste)
				PasteSelection();
			else
				SelectionEnd = GetBrushLocationBR();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (Map == null) return;

			if (!IsPanning)
			{
				if (e.Button == MouseButtons.Right) Erase();
				if (e.Button == MouseButtons.Left)
				{
					Draw();
					if (!IsPaste)
						SelectionStart = SelectionEnd = GetBrushLocation();
				}
			}

			Invalidate();
		}

		public const int ChunkSize = 8;		// 8x8 chunks ==> 192x192 bitmaps.

		Bitmap RenderChunk(int u, int v)
		{

			var bitmap = new Bitmap(ChunkSize * TileSet.TileSize, ChunkSize * TileSet.TileSize);

			var data = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* p = (int*)data.Scan0.ToPointer();
				var stride = data.Stride >> 2;

				for (var i = 0; i < ChunkSize; i++)
					for (var j = 0; j < ChunkSize; j++)
					{
						var tr = Map.MapTiles.Value[u * ChunkSize + i, v * ChunkSize + j];
						var tile = TileSet.Templates[tr.type].Data;
						var index = (tr.index < tile.TileBitmapBytes.Count) ? tr.index : (byte)0;
						var rawImage = tile.TileBitmapBytes[index];
						for (var x = 0; x < TileSet.TileSize; x++)
							for (var y = 0; y < TileSet.TileSize; y++)
								p[(j * TileSet.TileSize + y) * stride + i * TileSet.TileSize + x] = Palette.GetColor(rawImage[x + TileSet.TileSize * y]).ToArgb();

						if (Map.MapResources.Value[u * ChunkSize + i, v * ChunkSize + j].type != 0)
						{
							var resourceImage = ResourceTemplates[Map.MapResources.Value[u * ChunkSize + i, v * ChunkSize + j].type].Bitmap;
							var srcdata = resourceImage.LockBits(resourceImage.Bounds(),
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

			if (ShowGrid)
				using( var g = SGraphics.FromImage(bitmap) )
				{
					var rect = new Rectangle(0,0,bitmap.Width, bitmap.Height);
					ControlPaint.DrawGrid( g, rect,	new Size(2, Game.CellSize), Color.DarkRed );
					ControlPaint.DrawGrid( g, rect,	new Size(Game.CellSize, 2), Color.DarkRed );
					ControlPaint.DrawGrid( g, rect,	new Size(Game.CellSize, Game.CellSize), Color.Red );
				}

			return bitmap;
		}

		public CPos GetBrushLocation()
		{
			var vX = (int)Math.Floor((MousePos.X - Offset.X) / Zoom);
			var vY = (int)Math.Floor((MousePos.Y - Offset.Y) / Zoom);
			return new CPos(vX / TileSet.TileSize, vY / TileSet.TileSize);
		}

		public CPos GetBrushLocationBR()
		{
			var vX = (int)Math.Floor((MousePos.X - Offset.X) / Zoom);
			var vY = (int)Math.Floor((MousePos.Y - Offset.Y) / Zoom);
			return new CPos((vX + TileSet.TileSize - 1) / TileSet.TileSize,
			                (vY + TileSet.TileSize - 1) / TileSet.TileSize);
		}

		public void DrawActor(SGraphics g, CPos p, ActorTemplate t, ColorPalette cp)
		{
			var centered = t.Appearance == null || !t.Appearance.RelativeToTopLeft;
			var actorPalette = cp;
			if (t.Appearance != null && t.Appearance.UseTerrainPalette)
				actorPalette = Palette.AsSystemPalette();
			DrawImage(g, t.Bitmap, p, centered, actorPalette);
		}

		float2 GetDrawPosition(CPos location, Bitmap bmp, bool centered)
		{
			float offsetX = centered ? bmp.Width / 2 - TileSet.TileSize / 2 : 0;
			float drawX = TileSet.TileSize * location.X * Zoom + Offset.X - offsetX;

			float offsetY = centered ? bmp.Height / 2 - TileSet.TileSize / 2 : 0;
			float drawY = TileSet.TileSize * location.Y * Zoom + Offset.Y - offsetY;

			return new float2(drawX, drawY);
		}

		public void DrawImage(SGraphics g, Bitmap bmp, CPos location, bool centered, ColorPalette cp)
		{
			var drawPos = GetDrawPosition(location, bmp, centered);

			var sourceRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
			var destRect = new RectangleF(drawPos.X, drawPos.Y, bmp.Width * Zoom, bmp.Height * Zoom);

			var restorePalette = bmp.Palette;
			if (cp != null) bmp.Palette = cp;
			g.DrawImage(bmp, destRect, sourceRect, GraphicsUnit.Pixel);
			if (cp != null) bmp.Palette = restorePalette;
		}

		void DrawActorBorder(SGraphics g, CPos p, ActorTemplate t)
		{
			var centered = t.Appearance == null || !t.Appearance.RelativeToTopLeft;
			var drawPos = GetDrawPosition(p, t.Bitmap, centered);

			g.DrawRectangle(CordonPen,
				drawPos.X, drawPos.Y,
				t.Bitmap.Width * Zoom, t.Bitmap.Height * Zoom);
		}

		ColorPalette GetPaletteForPlayerInner(string name)
		{
			var pr = Map.Players[name];
			var pcpi = Rules.Info["player"].Traits.Get<PlayerColorPaletteInfo>();
			var remap = new PlayerColorRemap(pcpi.RemapIndex, pr.ColorRamp);
			return new Palette(PlayerPalette, remap).AsSystemPalette();
		}

		Cache<string, ColorPalette> PlayerPalettes;

		public ColorPalette GetPaletteForPlayer(string player)
		{
			if (PlayerPalettes == null)
				PlayerPalettes = new Cache<string, ColorPalette>(GetPaletteForPlayerInner);

			return PlayerPalettes[player];
		}

		ColorPalette GetPaletteForActor(ActorReference ar)
		{
			var ownerInit = ar.InitDict.GetOrDefault<OwnerInit>();
			if (ownerInit == null)
				return null;

			return GetPaletteForPlayer(ownerInit.PlayerName);
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

					var bmp = Chunks[x];

					float DrawX = TileSet.TileSize * (float)ChunkSize * (float)x.X * Zoom + Offset.X;
					float DrawY = TileSet.TileSize * (float)ChunkSize * (float)x.Y * Zoom + Offset.Y;
					RectangleF sourceRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
					RectangleF destRect = new RectangleF(DrawX, DrawY, bmp.Width * Zoom, bmp.Height * Zoom);
					e.Graphics.DrawImage(bmp, destRect, sourceRect, GraphicsUnit.Pixel);
				}

			e.Graphics.DrawRectangle(CordonPen,
				Map.Bounds.Left * TileSet.TileSize * Zoom + Offset.X,
				Map.Bounds.Top * TileSet.TileSize * Zoom + Offset.Y,
				Map.Bounds.Width * TileSet.TileSize * Zoom,
				Map.Bounds.Height * TileSet.TileSize * Zoom);

			e.Graphics.DrawRectangle(SelectionPen,
				(SelectionStart.X * TileSet.TileSize * Zoom) + Offset.X,
				(SelectionStart.Y * TileSet.TileSize * Zoom) + Offset.Y,
				(SelectionEnd - SelectionStart).X * TileSet.TileSize * Zoom,
				(SelectionEnd - SelectionStart).Y * TileSet.TileSize * Zoom);

			if (IsPaste)
			{
				var loc = GetBrushLocation();
				var width = Math.Abs((SelectionStart - SelectionEnd).X);
				var height = Math.Abs((SelectionStart - SelectionEnd).Y);

				e.Graphics.DrawRectangle(PastePen,
					(loc.X * TileSet.TileSize * Zoom) + Offset.X,
					(loc.Y * TileSet.TileSize * Zoom) + Offset.Y,
					width * (TileSet.TileSize * Zoom),
					height * (TileSet.TileSize * Zoom));
			}

			foreach (var ar in Map.Actors.Value)
			{
				if (ActorTemplates.ContainsKey(ar.Value.Type))
					DrawActor(e.Graphics, ar.Value.Location(), ActorTemplates[ar.Value.Type],
						GetPaletteForActor(ar.Value));
				else
					Console.WriteLine("Warning: Unknown or excluded actor: {0}", ar.Value.Type);
			}

			if (ShowActorNames)
				foreach (var ar in Map.Actors.Value)
					if (!ar.Key.StartsWith("Actor"))	// if it has a custom name
						e.Graphics.DrawStringContrast(Font, ar.Key,
							(int)(ar.Value.Location().X * TileSet.TileSize * Zoom + Offset.X),
							(int)(ar.Value.Location().Y * TileSet.TileSize * Zoom + Offset.Y),
							Brushes.White,
							Brushes.Black);

			if (ShowRuler && Zoom > 0.2)
			{
				for (int i = Map.Bounds.Left; i <= Map.Bounds.Right; i+=8)
				{
					if( i % 8 == 0)
					{
						PointF point = new PointF(i * TileSet.TileSize * Zoom + Offset.X, (Map.Bounds.Top - 8) * TileSet.TileSize * Zoom + Offset.Y);
						e.Graphics.DrawString((i - Map.Bounds.Left).ToString(), MarkerFont, TextBrush, point);
					}
				}

				for (int i = Map.Bounds.Top; i <= Map.Bounds.Bottom; i+=8)
				{
					if (i % 8 == 0)
					{
						PointF point = new PointF((Map.Bounds.Left - 8) * TileSet.TileSize * Zoom + Offset.X, i * TileSet.TileSize * Zoom + Offset.Y);
						e.Graphics.DrawString((i - Map.Bounds.Left).ToString(), MarkerFont, TextBrush, point);
					}
				}
			}

			if (Tool != null)
				Tool.Preview(this, e.Graphics);

			if (Tool == null)
			{
				var x = Map.Actors.Value.FirstOrDefault(a => a.Value.Location() == GetBrushLocation());
				if (x.Key != null)
					DrawActorBorder(e.Graphics, x.Value.Location(), ActorTemplates[x.Value.Type]);
			}
		}

		public void CopySelection()
		{
			// Grab tiles and resources within selection (doesn't do actors)
			var start = SelectionStart;
			var end = SelectionEnd;

			if (start == end) return;

			int width = Math.Abs((start - end).X);
			int height = Math.Abs((start - end).Y);

			TileSelection = new TileReference<ushort, byte>[width, height];
			ResourceSelection = new TileReference<byte, byte>[width, height];

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					//TODO: crash prevention
					TileSelection[x, y] = Map.MapTiles.Value[start.X + x, start.Y + y];
					ResourceSelection[x, y] = Map.MapResources.Value[start.X + x, start.Y + y];
				}
			}
		}

		void PasteSelection()
		{
			var loc = GetBrushLocation();
			var width = Math.Abs((SelectionStart - SelectionEnd).X);
			var height = Math.Abs((SelectionStart - SelectionEnd).Y);

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					var mapX = loc.X + x;
					var mapY = loc.Y + y;

					//TODO: crash prevention for outside of bounds
					Map.MapTiles.Value[mapX, mapY] = TileSelection[x, y];
					Map.MapResources.Value[mapX, mapY] = ResourceSelection[x, y];

					var ch = new int2(mapX / ChunkSize, mapY / ChunkSize);
					if (Chunks.ContainsKey(ch))
					{
						Chunks[ch].Dispose();
						Chunks.Remove(ch);
					}
				}
			}
			AfterChange();
		}

		void ClearSelection()
		{
			SelectionStart = CPos.Zero;
			SelectionEnd = CPos.Zero;
			TileSelection = null;
			ResourceSelection = null;
		}
	}

	static class ActorReferenceExts
	{
		public static CPos Location(this ActorReference ar)
		{
			return (CPos)ar.InitDict.Get<LocationInit>().value;
		}

		public static void DrawStringContrast(this SGraphics g, Font f, string s, int x, int y, Brush fg, Brush bg)
		{
			g.DrawString(s, f, bg, x - 1, y - 1);
			g.DrawString(s, f, bg, x + 1, y - 1);
			g.DrawString(s, f, bg, x - 1, y + 1);
			g.DrawString(s, f, bg, x + 1, y + 1);

			g.DrawString(s, f, fg, x, y);
		}
	}
}