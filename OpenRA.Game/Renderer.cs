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
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class Renderer : IDisposable
	{
		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public RgbaSpriteRenderer WorldRgbaSpriteRenderer { get; private set; }
		public RgbaColorRenderer WorldRgbaColorRenderer { get; private set; }
		public ModelRenderer WorldModelRenderer { get; private set; }
		public RgbaColorRenderer RgbaColorRenderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }
		public RgbaSpriteRenderer RgbaSpriteRenderer { get; private set; }
		public IReadOnlyDictionary<string, SpriteFont> Fonts;

		internal IPlatformWindow Window { get; private set; }
		internal IGraphicsContext Context { get; private set; }

		internal int SheetSize { get; private set; }
		internal int TempBufferSize { get; private set; }

		readonly IVertexBuffer<Vertex> tempBuffer;
		readonly Stack<Rectangle> scissorState = new Stack<Rectangle>();

		IFrameBuffer screenBuffer;
		Sprite screenSprite;

		SheetBuilder fontSheetBuilder;
		readonly IPlatform platform;

		float depthScale;
		float depthOffset;

		Size lastBufferSize = new Size(-1, -1);
		int2 lastScroll = new int2(-1, -1);
		float lastZoom = -1f;
		ITexture currentPaletteTexture;
		IBatchRenderer currentBatchRenderer;

		public Renderer(IPlatform platform, GraphicSettings graphicSettings)
		{
			this.platform = platform;
			var resolution = GetResolution(graphicSettings);

			Window = platform.CreateWindow(new Size(resolution.Width, resolution.Height), graphicSettings.Mode, graphicSettings.BatchSize);
			Context = Window.Context;

			TempBufferSize = graphicSettings.BatchSize;
			SheetSize = graphicSettings.SheetSize;

			WorldSpriteRenderer = new SpriteRenderer(this, Context.CreateShader("combined"));
			WorldRgbaSpriteRenderer = new RgbaSpriteRenderer(WorldSpriteRenderer);
			WorldRgbaColorRenderer = new RgbaColorRenderer(WorldSpriteRenderer);
			WorldModelRenderer = new ModelRenderer(this, Context.CreateShader("model"));
			SpriteRenderer = new SpriteRenderer(this, Context.CreateShader("combined"));
			RgbaSpriteRenderer = new RgbaSpriteRenderer(SpriteRenderer);
			RgbaColorRenderer = new RgbaColorRenderer(SpriteRenderer);

			tempBuffer = Context.CreateVertexBuffer(TempBufferSize);
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
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA, 512);
				Fonts = modData.Manifest.Get<Fonts>().FontList.ToDictionary(x => x.Key,
					x => new SpriteFont(x.Value.Font, modData.DefaultFileSystem.Open(x.Value.Font).ReadAllBytes(),
										x.Value.Size, x.Value.Ascender, Window.WindowScale, fontSheetBuilder)).AsReadOnly();
			}

			Window.OnWindowScaleChanged += (before, after) =>
			{
				Game.RunAfterTick(() =>
				{
					foreach (var f in Fonts)
						f.Value.SetScale(after);
				});
			};
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
			depthScale = mapGrid == null || !mapGrid.EnableDepthBuffer ? 0 :
				(float)Resolution.Height / (Resolution.Height + mapGrid.TileSize.Height * mapGrid.MaximumTerrainHeight);

			depthOffset = depthScale / 2;
		}

		public void BeginFrame(int2 scroll, float zoom)
		{
			Context.Clear();

			var surfaceSize = Window.SurfaceSize;
			var surfaceBufferSize = surfaceSize.NextPowerOf2();

			if (screenSprite == null || screenSprite.Sheet.Size != surfaceBufferSize)
			{
				if (screenBuffer != null)
					screenBuffer.Dispose();

				// Render the screen into a frame buffer to simplify reading back screenshots
				screenBuffer = Context.CreateFrameBuffer(surfaceBufferSize, Color.FromArgb(0xFF, 0, 0, 0));
			}

			if (screenSprite == null || surfaceSize.Width != screenSprite.Bounds.Width || -surfaceSize.Height != screenSprite.Bounds.Height)
			{
				var screenSheet = new Sheet(SheetType.BGRA, screenBuffer.Texture);

				// Flip sprite in Y to match OpenGL's bottom-left origin
				var screenBounds = Rectangle.FromLTRB(0, surfaceSize.Height, surfaceSize.Width, 0);
				screenSprite = new Sprite(screenSheet, screenBounds, TextureChannel.RGBA);
			}

			screenBuffer.Bind();
			SetViewportParams(scroll, zoom);
		}

		public void SetViewportParams(int2 scroll, float zoom)
		{
			// In HiDPI windows we follow Apple's convention of defining window coordinates as for standard resolution windows
			// but to have a higher resolution backing surface with more than 1 texture pixel per viewport pixel.
			// We must convert the surface buffer size to a viewport size - in general this is NOT just the window size
			// rounded to the next power of two, as the NextPowerOf2 calculation is done in the surface pixel coordinates
			var scale = Window.WindowScale;
			var surfaceBufferSize = Window.SurfaceSize.NextPowerOf2();
			var bufferSize = new Size((int)(surfaceBufferSize.Width / scale), (int)(surfaceBufferSize.Height / scale));

			// PERF: Calling SetViewportParams on each renderer is slow. Only call it when things change.
			// If zoom evaluates as different due to floating point weirdness that's OK, it will be going away soon
			if (lastBufferSize != bufferSize || lastScroll != scroll || lastZoom != zoom)
			{
				if (lastBufferSize != bufferSize)
					SpriteRenderer.SetViewportParams(bufferSize, 0f, 0f, 1f, int2.Zero);

				WorldSpriteRenderer.SetViewportParams(bufferSize, depthScale, depthOffset, zoom, scroll);
				WorldModelRenderer.SetViewportParams(bufferSize, zoom, scroll);

				lastBufferSize = bufferSize;
				lastScroll = scroll;
				lastZoom = zoom;
			}
		}

		public void SetPalette(HardwarePalette palette)
		{
			if (palette.Texture == currentPaletteTexture)
				return;

			Flush();
			currentPaletteTexture = palette.Texture;

			SpriteRenderer.SetPalette(currentPaletteTexture);
			WorldSpriteRenderer.SetPalette(currentPaletteTexture);
			WorldModelRenderer.SetPalette(currentPaletteTexture);
		}

		public void EndFrame(IInputHandler inputHandler)
		{
			Flush();

			screenBuffer.Unbind();

			// Render the compositor buffer to the screen
			// HACK / PERF: Fudge the coordinates to cover the actual window while keeping the buffer viewport parameters
			// This saves us two redundant (and expensive) SetViewportParams each frame
			RgbaSpriteRenderer.DrawSprite(screenSprite, new float3(0, lastBufferSize.Height, 0), new float3(lastBufferSize.Width, -lastBufferSize.Height, 0));
			Flush();

			Window.PumpInput(inputHandler);
			Context.Present();
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
			Context.DrawPrimitives(type, firstVertex, numVertices);
			PerfHistory.Increment("batches", 1);
		}

		public void Flush()
		{
			CurrentBatchRenderer = null;
		}

		public Size Resolution { get { return Window.WindowSize; } }
		public float WindowScale { get { return Window.WindowScale; } }

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
			return Context.CreateVertexBuffer(length);
		}

		public void EnableScissor(Rectangle rect)
		{
			// Must remain inside the current scissor rect
			if (scissorState.Any())
				rect = Rectangle.Intersect(rect, scissorState.Peek());

			Flush();
			Context.EnableScissor(rect.Left, rect.Top, rect.Width, rect.Height);
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
				Context.EnableScissor(rect.Left, rect.Top, rect.Width, rect.Height);
			}
			else
				Context.DisableScissor();
		}

		public void EnableDepthBuffer()
		{
			Flush();
			Context.EnableDepthBuffer();
		}

		public void DisableDepthBuffer()
		{
			Flush();
			Context.DisableDepthBuffer();
		}

		public void ClearDepthBuffer()
		{
			Flush();
			Context.ClearDepthBuffer();
		}

		public void GrabWindowMouseFocus()
		{
			Window.GrabWindowMouseFocus();
		}

		public void ReleaseWindowMouseFocus()
		{
			Window.ReleaseWindowMouseFocus();
		}

		public void SaveScreenshot(string path)
		{
			// Pull the data from the Texture directly to prevent the sheet from buffering it
			var src = screenBuffer.Texture.GetData();
			var srcWidth = screenSprite.Sheet.Size.Width;
			var destWidth = screenSprite.Bounds.Width;
			var destHeight = -screenSprite.Bounds.Height;
			var channelOrder = new[] { 2, 1, 0, 3 };

			ThreadPool.QueueUserWorkItem(_ =>
			{
				// Convert BGRA to RGBA
				var dest = new byte[4 * destWidth * destHeight];
				for (var y = 0; y < destHeight; y++)
				{
					for (var x = 0; x < destWidth; x++)
					{
						var destOffset = 4 * (y * destWidth + x);
						var srcOffset = 4 * (y * srcWidth + x);
						for (var i = 0; i < 4; i++)
							dest[destOffset + i] = src[srcOffset + channelOrder[i]];
					}
				}

				new Png(dest, destWidth, destHeight).Save(path);
			});
		}

		public void Dispose()
		{
			WorldModelRenderer.Dispose();
			tempBuffer.Dispose();
			if (fontSheetBuilder != null)
				fontSheetBuilder.Dispose();
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
			Window.Dispose();
		}

		public string GetClipboardText()
		{
			return Window.GetClipboardText();
		}

		public bool SetClipboardText(string text)
		{
			return Window.SetClipboardText(text);
		}

		public string GLVersion
		{
			get { return Context.GLVersion; }
		}

		public IFont CreateFont(byte[] data)
		{
			return platform.CreateFont(data);
		}
	}
}
