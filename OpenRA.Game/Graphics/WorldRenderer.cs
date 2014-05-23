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
using OpenRA.Primitives;
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
		public readonly Theater Theater;
		public Viewport Viewport { get; private set; }

		internal readonly TerrainRenderer terrainRenderer;
		internal readonly HardwarePalette palette;
		internal Cache<string, PaletteReference> palettes;
		Lazy<DeveloperMode> devTrait;

		internal WorldRenderer(World world)
		{
			this.world = world;
			Viewport = new Viewport(this, world.Map);
			palette = new HardwarePalette();

			palettes = new Cache<string, PaletteReference>(CreatePaletteReference);
			foreach (var pal in world.traitDict.ActorsWithTraitMultiple<IPalette>(world))
				pal.Trait.InitPalette(this);

			palette.Initialize();

			Theater = new Theater(world.TileSet);
			terrainRenderer = new TerrainRenderer(world, this);

			devTrait = Exts.Lazy(() => world.LocalPlayer != null ? world.LocalPlayer.PlayerActor.Trait<DeveloperMode>() : null);
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

		List<IRenderable> GenerateRenderables()
		{
			var comparer = new RenderableComparer(this);
			var actors = world.ScreenMap.ActorsInBox(Viewport.TopLeft, Viewport.BottomRight)
				.Append(world.WorldActor);

			// Include player actor for the rendered player
			if (world.RenderPlayer != null)
				actors.Append(world.RenderPlayer.PlayerActor);

			var worldRenderables = actors.SelectMany(a => a.Render(this));
			if (world.OrderGenerator != null)
				worldRenderables = worldRenderables.Concat(world.OrderGenerator.Render(this, world));

			worldRenderables = worldRenderables.OrderBy(r => r, comparer);

			// Effects are drawn on top of all actors
			// TODO: Allow effects to be interleaved with actors
			var effectRenderables = world.Effects
				.SelectMany(e => e.Render(this));

			// Iterating via foreach() copies the structs, so enumerate by index
			var renderables = worldRenderables.Concat(effectRenderables).ToList();

			Game.Renderer.WorldVoxelRenderer.BeginFrame();
			for (var i = 0; i < renderables.Count; i++)
				renderables[i].BeforeRender(this);
			Game.Renderer.WorldVoxelRenderer.EndFrame();

			return renderables;
		}

		public void Draw()
		{
			RefreshPalette();

			if (world.IsShellmap && !Game.Settings.Game.ShowShellmap)
				return;

			var renderables = GenerateRenderables();
			var bounds = Viewport.ScissorBounds;
			Game.Renderer.EnableScissor(bounds);

			terrainRenderer.Draw(this, Viewport);
			Game.Renderer.Flush();

			for (var i = 0; i < renderables.Count; i++)
				renderables[i].Render(this);

			// added for contrails
			foreach (var a in world.ActorsWithTrait<IPostRender>())
				if (a.Actor.IsInWorld && !a.Actor.Destroyed)
					a.Trait.RenderAfterWorld(this, a.Actor);

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderAfterWorld(this, world);

			var renderShroud = world.RenderPlayer != null ? world.RenderPlayer.Shroud : null;

			foreach (var a in world.ActorsWithTrait<IRenderShroud>())
				a.Trait.RenderShroud(this, renderShroud);

			if (devTrait.Value != null && devTrait.Value.ShowDebugGeometry)
				for (var i = 0; i < renderables.Count; i++)
					renderables[i].RenderDebugGeometry(this);

			Game.Renderer.DisableScissor();

			foreach (var g in world.Selection.Actors.Where(a => !a.Destroyed)
				.SelectMany(a => a.TraitsImplementing<IPostRenderSelection>())
				.GroupBy(prs => prs.GetType()))
				foreach (var t in g)
					t.RenderAfterWorld(this);

			if (!world.IsShellmap && Game.Settings.Game.AlwaysShowStatusBars)
			{
				foreach (var g in world.Actors.Where(a => !a.Destroyed
					&& a.HasTrait<Selectable>()
					&& !world.FogObscures(a)
					&& !world.Selection.Actors.Contains(a)))

					DrawRollover(g);
			}

			Game.Renderer.Flush();
		}

		public void DrawSelectionBox(Actor a, Color c)
		{
			var pos = ScreenPxPosition(a.CenterPosition);
			var bounds = a.Bounds.Value;

			var tl = pos + new float2(bounds.Left, bounds.Top);
			var br = pos + new float2(bounds.Right, bounds.Bottom);
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			var u = new float2(4f / Viewport.Zoom, 0);
			var v = new float2(0, 4f / Viewport.Zoom);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(tl + u, tl, c, c);
			wlr.DrawLine(tl, tl + v, c, c);
			wlr.DrawLine(tr, tr - u, c, c);
			wlr.DrawLine(tr, tr + v, c, c);

			wlr.DrawLine(bl, bl + u, c, c);
			wlr.DrawLine(bl, bl - v, c, c);
			wlr.DrawLine(br, br - u, c, c);
			wlr.DrawLine(br, br - v, c, c);
		}

		public void DrawRollover(Actor unit)
		{
			var selectable = unit.TraitOrDefault<Selectable>();
			if (selectable != null)
				selectable.DrawRollover(this);
		}

		public void DrawRangeCircle(WPos pos, WRange range, Color c)
		{
			var offset = new WVec(range.Range, 0, 0);
			for (var i = 0; i < 32; i++)
			{
				var pa = pos + offset.Rotate(WRot.FromFacing(8 * i));
				var pb = pos + offset.Rotate(WRot.FromFacing(8 * i + 6));
				Game.Renderer.WorldLineRenderer.DrawLine(ScreenPosition(pa), ScreenPosition(pb), c, c);
			}
		}

		public void DrawRangeCircleWithContrast(WPos pos, WRange range, Color fg, Color bg)
		{
			var wlr = Game.Renderer.WorldLineRenderer;
			var oldWidth = wlr.LineWidth;
			wlr.LineWidth = 3;
			DrawRangeCircle(pos, range, bg);
			wlr.LineWidth = 1;
			DrawRangeCircle(pos, range, fg);
			wlr.LineWidth = oldWidth;
		}

		public void DrawTargetMarker(Color c, float2 location)
		{
			var tl = new float2(-1 / Viewport.Zoom, -1 / Viewport.Zoom);
			var br = new float2(1 / Viewport.Zoom, 1 / Viewport.Zoom);
			var bl = new float2(tl.X, br.Y);
			var tr = new float2(br.X, tl.Y);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(location + tl, location + tr, c, c);
			wlr.DrawLine(location + tr, location + br, c, c);
			wlr.DrawLine(location + br, location + bl, c, c);
			wlr.DrawLine(location + bl, location + tl, c, c);
		}

		public void RefreshPalette()
		{
			palette.ApplyModifiers(world.WorldActor.TraitsImplementing<IPaletteModifier>());
			Game.Renderer.SetPalette(palette);
		}

		// Conversion between world and screen coordinates
		public float2 ScreenPosition(WPos pos)
		{
			var ts = Game.modData.Manifest.TileSize;
			return new float2(ts.Width * pos.X / 1024f, ts.Height * (pos.Y - pos.Z) / 1024f);
		}

		public int2 ScreenPxPosition(WPos pos)
		{
			// Round to nearest pixel
			var px = ScreenPosition(pos);
			return new int2((int)Math.Round(px.X), (int)Math.Round(px.Y));
		}

		float ScreenVectorX(WVec vec) { return Game.modData.Manifest.TileSize.Width * vec.X / 1024f; }
		float ScreenVectorY(WVec vec) { return Game.modData.Manifest.TileSize.Width * vec.Y / 1024f; }
		float ScreenVectorZ(WVec vec) { return Game.modData.Manifest.TileSize.Width * vec.Z / 1024f; }

		// For scaling vectors to pixel sizes in the voxel renderer
		public float[] ScreenVector(WVec vec)
		{
			return new float[] { ScreenVectorX(vec), ScreenVectorY(vec), ScreenVectorZ(vec), 1 };
		}

		public int2 ScreenPxOffset(WVec vec)
		{
			// Round to nearest pixel
			return new int2((int)Math.Round(ScreenVectorX(vec)), (int)Math.Round(ScreenVectorY(vec) - ScreenVectorZ(vec)));
		}

		public float ScreenZPosition(WPos pos, int offset)
		{
			var ts = Game.modData.Manifest.TileSize;
			return (pos.Y + pos.Z + offset) * ts.Height / 1024f;
		}

		public WPos Position(int2 screenPx)
		{
			var ts = Game.modData.Manifest.TileSize;
			return new WPos(1024 * screenPx.X / ts.Width, 1024 * screenPx.Y / ts.Height, 0);
		}
	}
}
