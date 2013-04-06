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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class PaletteReference
	{
		public readonly string Name;
		public readonly int Index;
		public readonly Palette Palette;
		public PaletteReference(string name, int index, Palette palette)
		{
			Name = name;
			Index = index;
			Palette = palette;
		}
	}

	public class WorldRenderer
	{
		public readonly World world;
		internal readonly TerrainRenderer terrainRenderer;
		internal readonly ShroudRenderer shroudRenderer;
		internal readonly HardwarePalette palette;
		internal Cache<string, PaletteReference> palettes;

		internal WorldRenderer(World world)
		{
			this.world = world;
			palette = new HardwarePalette();

			palettes = new Cache<string, PaletteReference>(CreatePaletteReference);
			foreach (var pal in world.traitDict.ActorsWithTraitMultiple<IPalette>(world))
				pal.Trait.InitPalette(this);

			palette.Initialize();

			terrainRenderer = new TerrainRenderer(world, this);
			shroudRenderer = new ShroudRenderer(world);
		}

		PaletteReference CreatePaletteReference(string name)
		{
			var pal = palette.GetPalette(name);
			if (pal == null)
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));

			return new PaletteReference(name, palette.GetPaletteIndex(name), pal);
		}

		public PaletteReference Palette(string name) { return palettes[name]; }
		public void AddPalette(string name, Palette pal, bool allowModifiers) { palette.AddPalette(name, pal, allowModifiers); }

		class SpriteComparer : IComparer<Renderable>
		{
			public int Compare(Renderable x, Renderable y)
			{
				return (x.Z + x.ZOffset).CompareTo(y.Z + y.ZOffset);
			}
		}

		IEnumerable<Renderable> SpritesToRender()
		{
			var bounds = Game.viewport.WorldBounds(world);
			var comparer = new SpriteComparer();

			var actors = world.FindUnits(
				bounds.TopLeftAsCPos().ToPPos(),
				bounds.BottomRightAsCPos().ToPPos()
			);

			var renderables = actors.SelectMany(a => a.Render(this))
				.OrderBy(r => r, comparer);

			var effects = world.Effects.SelectMany(e => e.Render(this));

			return renderables.Concat(effects);
		}

		public void Draw()
		{
			RefreshPalette();

			if (world.IsShellmap && !Game.Settings.Game.ShowShellmap)
				return;

			var bounds = Game.viewport.ViewBounds(world);
			Game.Renderer.EnableScissor(bounds.Left, bounds.Top, bounds.Width, bounds.Height);

			terrainRenderer.Draw(this, Game.viewport);
			foreach (var a in world.traitDict.ActorsWithTraitMultiple<IRenderAsTerrain>(world))
				foreach (var r in a.Trait.RenderAsTerrain(this, a.Actor))
					r.Sprite.DrawAt(r.Pos, r.Palette.Index, r.Scale);

			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(this, a);

			Game.Renderer.Flush();

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderBeforeWorld(this, world);

			foreach (var image in SpritesToRender())
				image.Sprite.DrawAt(image.Pos, image.Palette.Index, image.Scale);

			// added for contrails
			foreach (var a in world.ActorsWithTrait<IPostRender>())
				if (!a.Actor.Destroyed)
					a.Trait.RenderAfterWorld(this, a.Actor);

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderAfterWorld(this, world);

			shroudRenderer.Draw( this );
			Game.Renderer.DisableScissor();

			foreach (var g in world.Selection.Actors.Where(a => !a.Destroyed)
				.SelectMany(a => a.TraitsImplementing<IPostRenderSelection>())
				.GroupBy(prs => prs.GetType()))
				foreach (var t in g)
					t.RenderAfterWorld(this);

			Game.Renderer.Flush();
		}

		public void DrawSelectionBox(Actor selectedUnit, Color c)
		{
			var bounds = selectedUnit.Bounds.Value;

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(xy, xy + new float2(4, 0), c, c);
			wlr.DrawLine(xy, xy + new float2(0, 4), c, c);
			wlr.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
			wlr.DrawLine(Xy, Xy + new float2(0, 4), c, c);

			wlr.DrawLine(xY, xY + new float2(4, 0), c, c);
			wlr.DrawLine(xY, xY + new float2(0, -4), c, c);
			wlr.DrawLine(XY, XY + new float2(-4, 0), c, c);
			wlr.DrawLine(XY, XY + new float2(0, -4), c, c);
		}

		public void DrawRollover(Actor unit)
		{
			var selectable = unit.TraitOrDefault<Selectable>();
			if (selectable != null)
				selectable.DrawRollover(this, unit);
		}

		public void DrawLocus(Color c, CPos[] cells)
		{
			var dict = cells.ToDictionary(a => a, a => 0);
			var wlr = Game.Renderer.WorldLineRenderer;

			foreach (var t in dict.Keys)
			{
				if (!dict.ContainsKey(t + new CVec(-1, 0)))
					wlr.DrawLine(t.ToPPos().ToFloat2(), (t + new CVec(0, 1)).ToPPos().ToFloat2(), c, c);
				if (!dict.ContainsKey(t + new CVec(1, 0)))
					wlr.DrawLine((t + new CVec(1, 0)).ToPPos().ToFloat2(), (t + new CVec(1, 1)).ToPPos().ToFloat2(), c, c);
				if (!dict.ContainsKey(t + new CVec(0, -1)))
					wlr.DrawLine(t.ToPPos().ToFloat2(), (t + new CVec(1, 0)).ToPPos().ToFloat2(), c, c);
				if (!dict.ContainsKey(t + new CVec(0, 1)))
					wlr.DrawLine((t + new CVec(0, 1)).ToPPos().ToFloat2(), (t + new CVec(1, 1)).ToPPos().ToFloat2(), c, c);
			}
		}

		public void DrawRangeCircle(Color c, float2 location, float range)
		{
			for (var i = 0; i < 32; i++)
			{
				var start = location + Game.CellSize * range * float2.FromAngle((float)(Math.PI * i) / 16);
				var end = location + Game.CellSize * range * float2.FromAngle((float)(Math.PI * (i + 0.7)) / 16);

				Game.Renderer.WorldLineRenderer.DrawLine(start, end, c, c);
			}
		}

		public void DrawRangeCircleWithContrast(Color fg, float2 location, float range, Color bg, int offset)
		{
			if (offset > 0) {
				DrawRangeCircle(bg, location, range + (float) offset/Game.CellSize);
				DrawRangeCircle(bg, location, range - (float) offset/Game.CellSize);
			}

			DrawRangeCircle(fg, location, range);
		}

		public void RefreshPalette()
		{
			palette.ApplyModifiers(world.WorldActor.TraitsImplementing<IPaletteModifier>());
			Game.Renderer.SetPalette(palette);
		}

		// Conversion between world and screen coordinates
		public float2 ScreenPosition(WPos pos)
		{
			var c = Game.CellSize/1024f;
			return new float2(c*pos.X, c*(pos.Y - pos.Z));
		}

		public int2 ScreenPxPosition(WPos pos)
		{
			return new int2(Game.CellSize*pos.X/1024, Game.CellSize*(pos.Y - pos.Z)/1024);
		}
		public float ScreenZOffset(WPos pos) { return pos.Z*Game.CellSize/1024f; }

		public int2 ScreenPxOffset(WVec vec)
		{
			return new int2(Game.CellSize*vec.X/1024, Game.CellSize*(vec.Y - vec.Z)/1024);
		}

		public float[] ScreenOffset(WVec vec)
		{
			var c = Game.CellSize/1024f;
			return new float[] {c*vec.X, c*vec.Y, c*vec.Z};
		}
	}
}
