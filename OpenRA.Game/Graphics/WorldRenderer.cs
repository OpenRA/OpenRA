#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public sealed class WorldRenderer : IDisposable
	{
		public static readonly Func<IRenderable, int> RenderableScreenZPositionComparisonKey =
			r => ZPosition(r.Pos, r.ZOffset);

		public readonly Size TileSize;
		public readonly int TileScale;
		public readonly World World;
		public readonly Theater Theater;
		public Viewport Viewport { get; private set; }

		public event Action PaletteInvalidated = null;

		readonly HashSet<Actor> onScreenActors = new HashSet<Actor>();
		readonly HardwarePalette palette = new HardwarePalette();
		readonly Dictionary<string, PaletteReference> palettes = new Dictionary<string, PaletteReference>();
		readonly IRenderTerrain terrainRenderer;
		readonly Lazy<DebugVisualizations> debugVis;
		readonly Func<string, PaletteReference> createPaletteReference;
		readonly bool enableDepthBuffer;

		readonly List<IFinalizedRenderable> preparedRenderables = new List<IFinalizedRenderable>();
		readonly List<IFinalizedRenderable> preparedOverlayRenderables = new List<IFinalizedRenderable>();

		bool lastDepthPreviewEnabled;

		internal WorldRenderer(ModData modData, World world)
		{
			World = world;
			TileSize = World.Map.Grid.TileSize;
			TileScale = World.Map.Grid.Type == MapGridType.RectangularIsometric ? 1448 : 1024;
			Viewport = new Viewport(this, world.Map);

			createPaletteReference = CreatePaletteReference;

			var mapGrid = modData.Manifest.Get<MapGrid>();
			enableDepthBuffer = mapGrid.EnableDepthBuffer;

			foreach (var pal in world.TraitDict.ActorsWithTrait<ILoadsPalettes>())
				pal.Trait.LoadPalettes(this);

			foreach (var p in world.Players)
				UpdatePalettesForPlayer(p.InternalName, p.Color, false);

			palette.Initialize();

			Theater = new Theater(world.Map.Rules.TileSet);
			terrainRenderer = world.WorldActor.TraitOrDefault<IRenderTerrain>();

			debugVis = Exts.Lazy(() => world.WorldActor.TraitOrDefault<DebugVisualizations>());
		}

		public void UpdatePalettesForPlayer(string internalName, Color color, bool replaceExisting)
		{
			foreach (var pal in World.WorldActor.TraitsImplementing<ILoadsPlayerPalettes>())
				pal.LoadPlayerPalettes(this, internalName, color, replaceExisting);
		}

		PaletteReference CreatePaletteReference(string name)
		{
			var pal = palette.GetPalette(name);
			return new PaletteReference(name, palette.GetPaletteIndex(name), pal, palette);
		}

		public PaletteReference Palette(string name) { return palettes.GetOrAdd(name, createPaletteReference); }
		public void AddPalette(string name, ImmutablePalette pal, bool allowModifiers = false, bool allowOverwrite = false)
		{
			if (allowOverwrite && palette.Contains(name))
				ReplacePalette(name, pal);
			else
			{
				var oldHeight = palette.Height;
				palette.AddPalette(name, pal, allowModifiers);

				if (oldHeight != palette.Height && PaletteInvalidated != null)
					PaletteInvalidated();
			}
		}

		public void ReplacePalette(string name, IPalette pal)
		{
			palette.ReplacePalette(name, pal);

			// Update cached PlayerReference if one exists
			if (palettes.ContainsKey(name))
				palettes[name].Palette = pal;
		}

		IEnumerable<IFinalizedRenderable> GenerateRenderables()
		{
			var actors = onScreenActors.Append(World.WorldActor);
			if (World.RenderPlayer != null)
				actors = actors.Append(World.RenderPlayer.PlayerActor);

			var worldRenderables = actors.SelectMany(a => a.Render(this));
			if (World.OrderGenerator != null)
				worldRenderables = worldRenderables.Concat(World.OrderGenerator.Render(this, World));

			// Unpartitioned effects
			worldRenderables = worldRenderables.Concat(World.UnpartitionedEffects.SelectMany(e => e.Render(this)));

			// Partitioned, currently on-screen effects
			var effectRenderables = World.ScreenMap.RenderableEffectsInBox(Viewport.TopLeft, Viewport.BottomRight);
			worldRenderables = worldRenderables.Concat(effectRenderables.SelectMany(e => e.Render(this)));

			worldRenderables = worldRenderables.OrderBy(RenderableScreenZPositionComparisonKey);

			return worldRenderables.Select(r => r.PrepareRender(this));
		}

		IEnumerable<IFinalizedRenderable> GenerateOverlayRenderables()
		{
			var aboveShroud = World.ActorsWithTrait<IRenderAboveShroud>()
				.Where(a => a.Actor.IsInWorld && !a.Actor.Disposed && (!a.Trait.SpatiallyPartitionable || onScreenActors.Contains(a.Actor)))
					.SelectMany(a => a.Trait.RenderAboveShroud(a.Actor, this));

			var aboveShroudSelected = World.Selection.Actors.Where(a => a.IsInWorld && !a.Disposed)
				.SelectMany(a => a.TraitsImplementing<IRenderAboveShroudWhenSelected>()
					.Where(t => !t.SpatiallyPartitionable || onScreenActors.Contains(a))
					.SelectMany(t => t.RenderAboveShroud(a, this)));

			var aboveShroudEffects = World.Effects.Select(e => e as IEffectAboveShroud)
				.Where(e => e != null)
				.SelectMany(e => e.RenderAboveShroud(this));

			var aboveShroudOrderGenerator = SpriteRenderable.None;
			if (World.OrderGenerator != null)
				aboveShroudOrderGenerator = World.OrderGenerator.RenderAboveShroud(this, World);

			var overlayRenderables = aboveShroud
				.Concat(aboveShroudSelected)
				.Concat(aboveShroudEffects)
				.Concat(aboveShroudOrderGenerator);

			return overlayRenderables.Select(r => r.PrepareRender(this));
		}

		public void PrepareRenderables()
		{
			if (World.WorldActor.Disposed)
				return;

			RefreshPalette();

			// PERF: Reuse collection to avoid allocations.
			onScreenActors.UnionWith(World.ScreenMap.RenderableActorsInBox(Viewport.TopLeft, Viewport.BottomRight));
			preparedRenderables.AddRange(GenerateRenderables());
			preparedOverlayRenderables.AddRange(GenerateOverlayRenderables());
			onScreenActors.Clear();
		}

		public void Draw()
		{
			if (World.WorldActor.Disposed)
				return;

			if (debugVis.Value != null && lastDepthPreviewEnabled != debugVis.Value.DepthBuffer)
			{
				lastDepthPreviewEnabled = debugVis.Value.DepthBuffer;
				Game.Renderer.WorldSpriteRenderer.SetDepthPreviewEnabled(lastDepthPreviewEnabled);
			}

			var bounds = Viewport.GetScissorBounds(World.Type != WorldType.Editor);
			Game.Renderer.EnableScissor(bounds);

			if (enableDepthBuffer)
				Game.Renderer.Context.EnableDepthBuffer();

			if (terrainRenderer != null)
				terrainRenderer.RenderTerrain(this, Viewport);

			Game.Renderer.Flush();

			for (var i = 0; i < preparedRenderables.Count; i++)
				preparedRenderables[i].Render(this);

			if (enableDepthBuffer)
				Game.Renderer.ClearDepthBuffer();

			foreach (var a in World.ActorsWithTrait<IRenderAboveWorld>())
				if (a.Actor.IsInWorld && !a.Actor.Disposed)
					a.Trait.RenderAboveWorld(a.Actor, this);

			var renderShroud = World.RenderPlayer != null ? World.RenderPlayer.Shroud : null;

			if (enableDepthBuffer)
				Game.Renderer.ClearDepthBuffer();

			foreach (var a in World.ActorsWithTrait<IRenderShroud>())
				a.Trait.RenderShroud(renderShroud, this);

			if (enableDepthBuffer)
				Game.Renderer.Context.DisableDepthBuffer();

			Game.Renderer.DisableScissor();

			// HACK: Keep old grouping behaviour
			var groupedOverlayRenderables = preparedOverlayRenderables.GroupBy(prs => prs.GetType());
			foreach (var g in groupedOverlayRenderables)
				foreach (var r in g)
					r.Render(this);

			if (debugVis.Value != null && debugVis.Value.RenderGeometry)
			{
				for (var i = 0; i < preparedRenderables.Count; i++)
					preparedRenderables[i].RenderDebugGeometry(this);

				foreach (var g in groupedOverlayRenderables)
					foreach (var r in g)
						r.RenderDebugGeometry(this);
			}

			if (debugVis.Value != null && debugVis.Value.ScreenMap)
			{
				foreach (var r in World.ScreenMap.RenderBounds(World.RenderPlayer))
					Game.Renderer.WorldRgbaColorRenderer.DrawRect(
						new float3(r.Left, r.Top, r.Bottom),
						new float3(r.Right, r.Bottom, r.Bottom),
						1 / Viewport.Zoom, Color.MediumSpringGreen);

				foreach (var r in World.ScreenMap.MouseBounds(World.RenderPlayer))
					Game.Renderer.WorldRgbaColorRenderer.DrawRect(
						new float3(r.Left, r.Top, r.Bottom),
						new float3(r.Right, r.Bottom, r.Bottom),
						1 / Viewport.Zoom, Color.OrangeRed);
			}

			Game.Renderer.Flush();
			preparedRenderables.Clear();
			preparedOverlayRenderables.Clear();
		}

		public void RefreshPalette()
		{
			palette.ApplyModifiers(World.WorldActor.TraitsImplementing<IPaletteModifier>());
			Game.Renderer.SetPalette(palette);
		}

		// Conversion between world and screen coordinates
		public float2 ScreenPosition(WPos pos)
		{
			return new float2((float)TileSize.Width * pos.X / TileScale, (float)TileSize.Height * (pos.Y - pos.Z) / TileScale);
		}

		public float3 Screen3DPosition(WPos pos)
		{
			var z = ZPosition(pos, 0) * (float)TileSize.Height / TileScale;
			return new float3((float)TileSize.Width * pos.X / TileScale, (float)TileSize.Height * (pos.Y - pos.Z) / TileScale, z);
		}

		public int2 ScreenPxPosition(WPos pos)
		{
			// Round to nearest pixel
			var px = ScreenPosition(pos);
			return new int2((int)Math.Round(px.X), (int)Math.Round(px.Y));
		}

		public float3 Screen3DPxPosition(WPos pos)
		{
			// Round to nearest pixel
			var px = Screen3DPosition(pos);
			return new float3((float)Math.Round(px.X), (float)Math.Round(px.Y), px.Z);
		}

		// For scaling vectors to pixel sizes in the model renderer
		public float3 ScreenVectorComponents(WVec vec)
		{
			return new float3(
				(float)TileSize.Width * vec.X / TileScale,
				(float)TileSize.Height * (vec.Y - vec.Z) / TileScale,
				(float)TileSize.Height * vec.Z / TileScale);
		}

		// For scaling vectors to pixel sizes in the model renderer
		public float[] ScreenVector(WVec vec)
		{
			var xyz = ScreenVectorComponents(vec);
			return new[] { xyz.X, xyz.Y, xyz.Z, 1f };
		}

		public int2 ScreenPxOffset(WVec vec)
		{
			// Round to nearest pixel
			var xyz = ScreenVectorComponents(vec);
			return new int2((int)Math.Round(xyz.X), (int)Math.Round(xyz.Y));
		}

		public float ScreenZPosition(WPos pos, int offset)
		{
			return ZPosition(pos, offset) * (float)TileSize.Height / TileScale;
		}

		static int ZPosition(WPos pos, int offset)
		{
			return pos.Y + pos.Z + offset;
		}

		/// <summary>
		/// Returns a position in the world that is projected to the given screen position.
		/// There are many possible world positions, and the returned value chooses the value with no elevation.
		/// </summary>
		public WPos ProjectedPosition(int2 screenPx)
		{
			return new WPos(TileScale * screenPx.X / TileSize.Width, TileScale * screenPx.Y / TileSize.Height, 0);
		}

		public void Dispose()
		{
			// HACK: Disposing the world from here violates ownership
			// but the WorldRenderer lifetime matches the disposal
			// behavior we want for the world, and the root object setup
			// is so horrible that doing it properly would be a giant mess.
			World.Dispose();

			palette.Dispose();
			Theater.Dispose();
		}
	}
}
