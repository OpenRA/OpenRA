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
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class Renderer : IDisposable
	{
		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public SpriteRenderer WorldRgbaSpriteRenderer { get; private set; }
		public RgbaColorRenderer WorldRgbaColorRenderer { get; private set; }
		public VoxelRenderer WorldVoxelRenderer { get; private set; }
		public RgbaColorRenderer RgbaColorRenderer { get; private set; }
		public SpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }
		public IReadOnlyDictionary<string, SpriteFont> Fonts;

		internal IGraphicsDevice Device { get; private set; }
		internal int SheetSize { get; private set; }
		internal int TempBufferSize { get; private set; }

		readonly IVertexBuffer<Vertex> tempBuffer;
		readonly Stack<Rectangle> scissorState = new Stack<Rectangle>();

		SheetBuilder fontSheetBuilder;

		float depthScale;
		float depthOffset;

		Size? lastResolution;
		int2? lastScroll;
		float? lastZoom;
		ITexture currentPaletteTexture;
		IBatchRenderer currentBatchRenderer;

		public Renderer(IPlatform platform, GraphicSettings graphicSettings)
		{
			var resolution = GetResolution(graphicSettings);

			Device = platform.CreateGraphics(new Size(resolution.Width, resolution.Height), graphicSettings.Mode);

			TempBufferSize = graphicSettings.BatchSize;
			SheetSize = graphicSettings.SheetSize;

			WorldSpriteRenderer = new SpriteRenderer(this, Device.CreateShader("shp"));
			WorldRgbaSpriteRenderer = new SpriteRenderer(this, Device.CreateShader("rgba"));
			WorldRgbaColorRenderer = new RgbaColorRenderer(this, Device.CreateShader("color"));
			WorldVoxelRenderer = new VoxelRenderer(this, Device.CreateShader("vxl"));
			RgbaColorRenderer = new RgbaColorRenderer(this, Device.CreateShader("color"));
			RgbaSpriteRenderer = new SpriteRenderer(this, Device.CreateShader("rgba"));
			SpriteRenderer = new SpriteRenderer(this, Device.CreateShader("shp"));

			tempBuffer = Device.CreateVertexBuffer(TempBufferSize);
		}

		static Size GetResolution(GraphicSettings graphicsSettings)
		{
			var size = (graphicsSettings.Mode == WindowMode.Windowed)
				? graphicsSettings.WindowedSize
				: graphicsSettings.FullscreenSize;
			return new Size(size.X, size.Y);
		}

		public void InitializeFonts(ModData modData)
		{
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
			using (new PerfTimer("SpriteFonts"))
			{
				if (fontSheetBuilder != null)
					fontSheetBuilder.Dispose();
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA);
				Fonts = modData.Manifest.Fonts.ToDictionary(x => x.Key,
					x => new SpriteFont(x.Value.First, modData.DefaultFileSystem.Open(x.Value.First).ReadAllBytes(), x.Value.Second, fontSheetBuilder)).AsReadOnly();
			}
		}

		public void InitializeDepthBuffer(MapGrid mapGrid)
		{
			// The depth buffer needs to be initialized with enough range to cover:
			//  - the height of the screen
			//  - the z-offset of tiles from MaxTerrainHeight below the bottom of the screen (pushed into view)
			//  - additional z-offset from actors on top of MaxTerrainHeight terrain
			//  - a small margin so that tiles rendered partially above the top edge of the screen aren't pushed behind the clip plane
			// We need an offset of mapGrid.MaximumTerrainHeight * mapGrid.TileSize.Height / 2 to cover the terrain height
			// and choose to use mapGrid.MaximumTerrainHeight * mapGrid.TileSize.Height / 4 for each of the actor and top-edge cases
			this.depthScale = mapGrid == null || !mapGrid.EnableDepthBuffer ? 0 :
				(float)Resolution.Height / (Resolution.Height + mapGrid.TileSize.Height * mapGrid.MaximumTerrainHeight);
			this.depthOffset = this.depthScale / 2;
		}

		public void BeginFrame(int2 scroll, float zoom)
		{
			Device.Clear();
			SetViewportParams(scroll, zoom);
		}

		public void SetViewportParams(int2 scroll, float zoom)
		{
			// PERF: Calling SetViewportParams on each renderer is slow. Only call it when things change.
			var resolutionChanged = lastResolution != Resolution;
			if (resolutionChanged)
			{
				lastResolution = Resolution;
				RgbaSpriteRenderer.SetViewportParams(Resolution, 0f, 0f, 1f, int2.Zero);
				SpriteRenderer.SetViewportParams(Resolution, 0f, 0f, 1f, int2.Zero);
				RgbaColorRenderer.SetViewportParams(Resolution, 1f, int2.Zero);
			}

			// If zoom evaluates as different due to floating point weirdness that's OK, setting the parameters again is harmless.
			if (resolutionChanged || lastScroll != scroll || lastZoom != zoom)
			{
				lastScroll = scroll;
				lastZoom = zoom;
				WorldRgbaSpriteRenderer.SetViewportParams(Resolution, depthScale, depthOffset, zoom, scroll);
				WorldSpriteRenderer.SetViewportParams(Resolution, depthScale, depthOffset, zoom, scroll);
				WorldVoxelRenderer.SetViewportParams(Resolution, zoom, scroll);
				WorldRgbaColorRenderer.SetViewportParams(Resolution, zoom, scroll);
			}
		}

		public void SetPalette(HardwarePalette palette)
		{
			if (palette.Texture == currentPaletteTexture)
				return;

			Flush();
			currentPaletteTexture = palette.Texture;

			RgbaSpriteRenderer.SetPalette(currentPaletteTexture);
			SpriteRenderer.SetPalette(currentPaletteTexture);
			WorldSpriteRenderer.SetPalette(currentPaletteTexture);
			WorldRgbaSpriteRenderer.SetPalette(currentPaletteTexture);
			WorldVoxelRenderer.SetPalette(currentPaletteTexture);
		}

		public void EndFrame(IInputHandler inputHandler)
		{
			Flush();
			Device.PumpInput(inputHandler);
			Device.Present();
		}

		public void DrawBatch(Vertex[] vertices, int numVertices, PrimitiveType type)
		{
			tempBuffer.SetData(vertices, numVertices);
			DrawBatch(tempBuffer, 0, numVertices, type);
		}

		public void DrawBatch<T>(IVertexBuffer<T> vertices,
			int firstVertex, int numVertices, PrimitiveType type)
			where T : struct
		{
			vertices.Bind();
			Device.DrawPrimitives(type, firstVertex, numVertices);
			PerfHistory.Increment("batches", 1);
		}

		public void Flush()
		{
			CurrentBatchRenderer = null;
		}

		public Size Resolution { get { return Device.WindowSize; } }

		public interface IBatchRenderer { void Flush(); }

		public IBatchRenderer CurrentBatchRenderer
		{
			get
			{
				return currentBatchRenderer;
			}

			set
			{
				if (currentBatchRenderer == value)
					return;
				if (currentBatchRenderer != null)
					currentBatchRenderer.Flush();
				currentBatchRenderer = value;
			}
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer(int length)
		{
			return Device.CreateVertexBuffer(length);
		}

		public void EnableScissor(Rectangle rect)
		{
			// Must remain inside the current scissor rect
			if (scissorState.Any())
				rect.Intersect(scissorState.Peek());

			Flush();
			Device.EnableScissor(rect.Left, rect.Top, rect.Width, rect.Height);
			scissorState.Push(rect);
		}

		public void DisableScissor()
		{
			scissorState.Pop();
			Flush();

			// Restore previous scissor rect
			if (scissorState.Any())
			{
				var rect = scissorState.Peek();
				Device.EnableScissor(rect.Left, rect.Top, rect.Width, rect.Height);
			}
			else
				Device.DisableScissor();
		}

		public void EnableDepthBuffer()
		{
			Flush();
			Device.EnableDepthBuffer();
		}

		public void DisableDepthBuffer()
		{
			Flush();
			Device.DisableDepthBuffer();
		}

		public void ClearDepthBuffer()
		{
			Flush();
			Device.ClearDepthBuffer();
		}

		public void GrabWindowMouseFocus()
		{
			Device.GrabWindowMouseFocus();
		}

		public void ReleaseWindowMouseFocus()
		{
			Device.ReleaseWindowMouseFocus();
		}

		public void Dispose()
		{
			Device.Dispose();
			WorldVoxelRenderer.Dispose();
			tempBuffer.Dispose();
			if (fontSheetBuilder != null)
				fontSheetBuilder.Dispose();
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
		}

		public string GetClipboardText()
		{
			return Device.GetClipboardText();
		}

		public bool SetClipboardText(string text)
		{
			return Device.SetClipboardText(text);
		}

		public string GLVersion
		{
			get { return Device.GLVersion; }
		}
	}
}
