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
	public class WorldRenderer
	{
		public readonly World world;
		internal readonly TerrainRenderer terrainRenderer;
		internal readonly ShroudRenderer shroudRenderer;
		internal readonly HardwarePalette palette;

		internal WorldRenderer(World world)
		{
			this.world = world;
			this.palette = Game.modData.Palette;
			foreach( var pal in world.traitDict.ActorsWithTraitMultiple<IPalette>( world ) )
				pal.Trait.InitPalette( this );

			terrainRenderer = new TerrainRenderer(world, this);
			shroudRenderer = new ShroudRenderer(world);
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

		IEnumerable<Renderable> SpritesToRender()
		{
			var bounds = Game.viewport.WorldBounds(world);
			var comparer = new SpriteComparer();

			var actors = world.FindUnits(
				new int2(Game.CellSize*bounds.Left, Game.CellSize*bounds.Top),
				new int2(Game.CellSize*bounds.Right, Game.CellSize*bounds.Bottom));

			var renderables = actors.SelectMany(a => a.Render())
				.OrderBy(r => r, comparer);

			var effects = world.Effects.SelectMany(e => e.Render());

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
				foreach (var r in a.Trait.RenderAsTerrain(a.Actor))
					r.Sprite.DrawAt(r.Pos, this.GetPaletteIndex(r.Palette), r.Scale);

			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(this, a);

			Game.Renderer.Flush();

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderBeforeWorld(this, world);

			foreach (var image in SpritesToRender())
				image.Sprite.DrawAt(image.Pos, this.GetPaletteIndex(image.Palette), image.Scale);

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

		public void DrawLocus(Color c, int2[] cells)
		{
			var dict = cells.ToDictionary(a => a, a => 0);
			var wlr = Game.Renderer.WorldLineRenderer;

			foreach (var t in dict.Keys)
			{
				if (!dict.ContainsKey(t + new int2(-1, 0)))
					wlr.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(0, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(1, 0)))
					wlr.DrawLine(Game.CellSize * (t + new int2(1, 0)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, -1)))
					wlr.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(1, 0)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, 1)))
					wlr.DrawLine(Game.CellSize * (t + new int2(0, 1)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
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

		public void RefreshPalette()
		{
			palette.Update( world.WorldActor.TraitsImplementing<IPaletteModifier>() );
		}
	}
}
