#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public sealed class WorldRenderer : IDisposable
	{
		public static readonly Func<IRenderable, int> RenderableScreenZPositionComparisonKey =
			r => ZPosition(r.Pos, r.ZOffset);

		public readonly Size TileSize;
		public readonly World World;
		public readonly Theater Theater;
		public Viewport Viewport { get; private set; }

		public event Action PaletteInvalidated = null;

		readonly HardwarePalette palette = new HardwarePalette();
		readonly Dictionary<string, PaletteReference> palettes = new Dictionary<string, PaletteReference>();
		readonly TerrainRenderer terrainRenderer;
		readonly Lazy<DeveloperMode> devTrait;
		readonly Func<string, PaletteReference> createPaletteReference;
		readonly bool enableDepthBuffer;

		internal WorldRenderer(ModData modData, World world)
		{
			World = world;
			TileSize = World.Map.Grid.TileSize;
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
			terrainRenderer = new TerrainRenderer(world, this);

			devTrait = Exts.Lazy(() => world.LocalPlayer != null ? world.LocalPlayer.PlayerActor.Trait<DeveloperMode>() : null);
		}

		public void UpdatePalettesForPlayer(string internalName, HSLColor color, bool replaceExisting)
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

		List<IFinalizedRenderable> GenerateRenderables()
		{
			var actors = World.ScreenMap.ActorsInBox(Viewport.TopLeft, Viewport.BottomRight).Append(World.WorldActor);
			if (World.RenderPlayer != null)
				actors = actors.Append(World.RenderPlayer.PlayerActor);

			var worldRenderables = actors.SelectMany(a => a.Render(this));
			if (World.OrderGenerator != null)
				worldRenderables = worldRenderables.Concat(World.OrderGenerator.Render(this, World));

			worldRenderables = worldRenderables.Concat(World.Effects.SelectMany(e => e.Render(this)));
			worldRenderables = worldRenderables.OrderBy(RenderableScreenZPositionComparisonKey);

			if (World.OrderGenerator != null)
				worldRenderables = worldRenderables.Concat(World.OrderGenerator.RenderAfterWorld(this, World));

			Game.Renderer.WorldVoxelRenderer.BeginFrame();
			var renderables = worldRenderables.Select(r => r.PrepareRender(this)).ToList();
			Game.Renderer.WorldVoxelRenderer.EndFrame();

			return renderables;
		}

		public void Draw()
		{
			if (World.WorldActor.Disposed)
				return;

			if (devTrait.Value != null)
			{
				Game.Renderer.WorldSpriteRenderer.SetDepthPreviewEnabled(devTrait.Value.ShowDepthPreview);
				Game.Renderer.WorldRgbaSpriteRenderer.SetDepthPreviewEnabled(devTrait.Value.ShowDepthPreview);
			}

			RefreshPalette();

			if (World.Type == WorldType.Shellmap && !Game.Settings.Game.ShowShellmap)
				return;

			var renderables = GenerateRenderables();
			var bounds = Viewport.GetScissorBounds(World.Type != WorldType.Editor);
			Game.Renderer.EnableScissor(bounds);

			if (enableDepthBuffer)
				Game.Renderer.Device.EnableDepthBuffer();

			terrainRenderer.Draw(this, Viewport);
			Game.Renderer.Flush();

			for (var i = 0; i < renderables.Count; i++)
				renderables[i].Render(this);

			if (enableDepthBuffer)
				Game.Renderer.ClearDepthBuffer();

			foreach (var a in World.ActorsWithTrait<IPostRender>())
				if (a.Actor.IsInWorld && !a.Actor.Disposed)
					a.Trait.RenderAfterWorld(this, a.Actor);

			var renderShroud = World.RenderPlayer != null ? World.RenderPlayer.Shroud : null;

			if (enableDepthBuffer)
				Game.Renderer.ClearDepthBuffer();

			foreach (var a in World.ActorsWithTrait<IRenderShroud>())
				a.Trait.RenderShroud(this, renderShroud);

			if (devTrait.Value != null && devTrait.Value.ShowDebugGeometry)
				for (var i = 0; i < renderables.Count; i++)
					renderables[i].RenderDebugGeometry(this);

			if (enableDepthBuffer)
				Game.Renderer.Device.DisableDepthBuffer();

			Game.Renderer.DisableScissor();

			var overlayRenderables = World.Selection.Actors.Where(a => !a.Disposed)
				.SelectMany(a => a.TraitsImplementing<IPostRenderSelection>())
				.SelectMany(t => t.RenderAfterWorld(this));

			Game.Renderer.WorldVoxelRenderer.BeginFrame();
			var finalOverlayRenderables = overlayRenderables.Select(r => r.PrepareRender(this));
			Game.Renderer.WorldVoxelRenderer.EndFrame();

			// HACK: Keep old grouping behaviour
			foreach (var g in finalOverlayRenderables.GroupBy(prs => prs.GetType()))
				foreach (var r in g)
					r.Render(this);

			if (devTrait.Value != null && devTrait.Value.ShowDebugGeometry)
				foreach (var g in finalOverlayRenderables.GroupBy(prs => prs.GetType()))
					foreach (var r in g)
						r.RenderDebugGeometry(this);

			if (World.Type == WorldType.Regular)
			{
				foreach (var g in World.ScreenMap.ActorsInBox(Viewport.TopLeft, Viewport.BottomRight)
					.Where(a =>
						!a.Disposed &&
						!World.Selection.Contains(a) &&
						a.Info.HasTraitInfo<SelectableInfo>() &&
						!World.FogObscures(a)))
				{
					if (Game.Settings.Game.StatusBars == StatusBarsType.Standard)
						new SelectionBarsRenderable(g, false, false).Render(this);

					if (Game.Settings.Game.StatusBars == StatusBarsType.AlwaysShow)
						new SelectionBarsRenderable(g, true, true).Render(this);

					if (Game.Settings.Game.StatusBars == StatusBarsType.DamageShow)
					{
						if (g.GetDamageState() != DamageState.Undamaged)
							new SelectionBarsRenderable(g, true, true).Render(this);
						else
							new SelectionBarsRenderable(g, false, true).Render(this);
					}
				}
			}

			Game.Renderer.Flush();
		}

		public void RefreshPalette()
		{
			palette.ApplyModifiers(World.WorldActor.TraitsImplementing<IPaletteModifier>());
			Game.Renderer.SetPalette(palette);
		}

		// Conversion between world and screen coordinates
		public float2 ScreenPosition(WPos pos)
		{
			return new float2(TileSize.Width * pos.X / 1024f, TileSize.Height * (pos.Y - pos.Z) / 1024f);
		}

		public float3 Screen3DPosition(WPos pos)
		{
			var z = ZPosition(pos, 0) * TileSize.Height / 1024f;
			return new float3(TileSize.Width * pos.X / 1024f, TileSize.Height * (pos.Y - pos.Z) / 1024f, z);
		}

		public int2 ScreenPxPosition(WPos pos)
		{
			// Round to nearest pixel
			var px = ScreenPosition(pos);
			return new int2((int)Math.Round(px.X), (int)Math.Round(px.Y));
		}

		// For scaling vectors to pixel sizes in the voxel renderer
		public void ScreenVectorComponents(WVec vec, out float x, out float y, out float z)
		{
			x = TileSize.Width * vec.X / 1024f;
			y = TileSize.Height * (vec.Y - vec.Z) / 1024f;
			z = TileSize.Height * vec.Z / 1024f;
		}

		// For scaling vectors to pixel sizes in the voxel renderer
		public float[] ScreenVector(WVec vec)
		{
			float x, y, z;
			ScreenVectorComponents(vec, out x, out y, out z);
			return new[] { x, y, z, 1f };
		}

		public int2 ScreenPxOffset(WVec vec)
		{
			// Round to nearest pixel
			float x, y, z;
			ScreenVectorComponents(vec, out x, out y, out z);
			return new int2((int)Math.Round(x), (int)Math.Round(y));
		}

		public float ScreenZPosition(WPos pos, int offset)
		{
			return ZPosition(pos, offset) * TileSize.Height / 1024f;
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
			return new WPos(1024 * screenPx.X / TileSize.Width, 1024 * screenPx.Y / TileSize.Height, 0);
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
			terrainRenderer.Dispose();
		}
	}
}
