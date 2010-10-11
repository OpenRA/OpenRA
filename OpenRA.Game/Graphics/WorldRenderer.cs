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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class WorldRenderer
	{
		readonly World world;
		internal readonly TerrainRenderer terrainRenderer;
		public readonly UiOverlay uiOverlay;
		internal readonly HardwarePalette palette;

		internal WorldRenderer(World world)
		{
			this.world = world;

			terrainRenderer = new TerrainRenderer(world, this);
			uiOverlay = new UiOverlay();
			palette = new HardwarePalette(world.Map);

			foreach( var pal in world.traitDict.ActorsWithTraitMultiple<IPalette>( world ) )
				pal.Trait.InitPalette( this );
		}
		
		public int GetPaletteIndex(string name) { return palette.GetPaletteIndex(name); }
		public Palette GetPalette(string name) { return palette.GetPalette(name); }
		public void AddPalette(string name, Palette pal) { palette.AddPalette(name, pal); }
		
		class SpriteComparer : IComparer<Renderable>
		{
			public int Compare(Renderable x, Renderable y)
			{
				return (x.Z + x.ZOffset).CompareTo(y.Z + y.ZOffset);
			}
		}

		Rectangle GetBoundsRect()
		{
			if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.Disabled && world.LocalPlayer.Shroud.Bounds.HasValue)
			{
				var r = world.LocalPlayer.Shroud.Bounds.Value;

				var left = (int)(Game.CellSize * r.Left - Game.viewport.Location.X);
				var top = (int)(Game.CellSize * r.Top - Game.viewport.Location.Y);
				var right = left + (int)(Game.CellSize * r.Width);
				var bottom = top + (int)(Game.CellSize * r.Height);

				if (left < 0) left = 0;
				if (top < 0) top = 0;
				if (right > Game.viewport.Width) right = Game.viewport.Width;
				if (bottom > Game.viewport.Height) bottom = Game.viewport.Height;

				return new Rectangle(left, top, right - left, bottom - top);
			}
			else
				return new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height);
		}

		Renderable[] worldSprites = { };
		public void Tick()
		{
			var bounds = GetBoundsRect();
			var comparer = new SpriteComparer();

			bounds.Offset((int)Game.viewport.Location.X, (int)Game.viewport.Location.Y);

			var actors = world.FindUnits(
				new float2(bounds.Left, bounds.Top),
				new float2(bounds.Right, bounds.Bottom));

			var renderables = actors.SelectMany(a => a.Render())
				.OrderBy(r => r, comparer);

			var effects = world.Effects.SelectMany(e => e.Render());

			worldSprites = renderables.Concat(effects).ToArray();
		}

		public void Draw()
		{
			RefreshPalette();
			var bounds = GetBoundsRect();
			Game.Renderer.EnableScissor(bounds.Left, bounds.Top, bounds.Width, bounds.Height);

			terrainRenderer.Draw(this, Game.viewport);

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderBeforeWorld(this, world);

			foreach( var image in worldSprites )
				image.Sprite.DrawAt( image.Pos, this.GetPaletteIndex( image.Palette ) );
			uiOverlay.Draw(this, world);

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderAfterWorld(this, world);

			if (world.LocalPlayer != null)
				world.LocalPlayer.Shroud.Draw( this );

			Game.Renderer.DisableScissor();
		}

		void DrawBox(RectangleF r, Color color)
		{
			var a = new float2(r.Left, r.Top);
			var b = new float2(r.Right - a.X, 0);
			var c = new float2(0, r.Bottom - a.Y);
			Game.Renderer.LineRenderer.DrawLine(a, a + b, color, color);
			Game.Renderer.LineRenderer.DrawLine(a + b, a + b + c, color, color);
			Game.Renderer.LineRenderer.DrawLine(a + b + c, a + c, color, color);
			Game.Renderer.LineRenderer.DrawLine(a, a + c, color, color);
		}

		void DrawBins(RectangleF bounds)
		{
			DrawBox(bounds, Color.Red);
			if (world.LocalPlayer != null)
				DrawBox(world.LocalPlayer.Shroud.Bounds.Value, Color.Blue);

			for (var j = 0; j < world.Map.MapSize.Y;
				j += world.WorldActor.Info.Traits.Get<SpatialBinsInfo>().BinSize)
			{
				Game.Renderer.LineRenderer.DrawLine(new float2(0, j * Game.CellSize), new float2(world.Map.MapSize.X * Game.CellSize, j * Game.CellSize), Color.Black, Color.Black);
				Game.Renderer.LineRenderer.DrawLine(new float2(j * Game.CellSize, 0), new float2(j * Game.CellSize, world.Map.MapSize.Y * Game.CellSize), Color.Black, Color.Black);
			}
		}

		public void DrawSelectionBox(Actor selectedUnit, Color c)
		{
			var bounds = selectedUnit.GetBounds(true);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);
				}

		public void DrawLocus(Color c, int2[] cells)
		{
			var dict = cells.ToDictionary(a => a, a => 0);
			foreach (var t in dict.Keys)
			{
				if (!dict.ContainsKey(t + new int2(-1, 0)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(0, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(1, 0)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * (t + new int2(1, 0)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, -1)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(1, 0)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, 1)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * (t + new int2(0, 1)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
			}
		}

		public void DrawRangeCircle(Color c, float2 location, int range)
		{
			var prev = location + Game.CellSize * range * float2.FromAngle(0);
			for (var i = 1; i <= 32; i++)
			{
				var pos = location + Game.CellSize * range * float2.FromAngle((float)(Math.PI * i) / 16);
				Game.Renderer.LineRenderer.DrawLine(prev, pos, c, c);
				prev = pos;
			}
		}

		public void RefreshPalette()
		{
			palette.Update( world.WorldActor.TraitsImplementing<IPaletteModifier>() );
		}
	}
}
