#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		enum RenderType { None, World, UI }

		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public RgbaSpriteRenderer WorldRgbaSpriteRenderer { get; private set; }
		public RgbaColorRenderer WorldRgbaColorRenderer { get; private set; }
		public ModelRenderer WorldModelRenderer { get; private set; }
		public RgbaColorRenderer RgbaColorRenderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }
		public RgbaSpriteRenderer RgbaSpriteRenderer { get; private set; }

		public bool WindowHasInputFocus
		{
			get
			{
				return Window.HasInputFocus;
			}
		}

		public IReadOnlyDictionary<string, SpriteFont> Fonts;

		internal IPlatformWindow Window { get; private set; }
		internal IGraphicsContext Context { get; private set; }

		internal int SheetSize { get; private set; }
		internal int TempBufferSize { get; private set; }

		readonly IVertexBuffer<Vertex> tempBuffer;
		readonly Stack<Rectangle> scissorState = new Stack<Rectangle>();

		IFrameBuffer screenBuffer;
		Sprite screenSprite;

		IFrameBuffer worldBuffer;
		Sprite worldSprite;

		SheetBuilder fontSheetBuilder;
		readonly IPlatform platform;

		float depthMargin;

		Size lastBufferSize = new Size(-1, -1);

		Size lastWorldBufferSize = new Size(-1, -1);
		Rectangle lastWorldViewport = Rectangle.Empty;
		ITexture currentPaletteTexture;
		IBatchRenderer currentBatchRenderer;
		RenderType renderType = RenderType.None;

		public Renderer(IPlatform platform, GraphicSettings graphicSettings)
		{
			this.platform = platform;
			var resolution = GetResolution(graphicSettings);

			Window = platform.CreateWindow(new Size(resolution.Width, resolution.Height),
				graphicSettings.Mode, graphicSettings.UIScale, graphicSettings.BatchSize,
				graphicSettings.VideoDisplay, graphicSettings.GLProfile, !graphicSettings.DisableLegacyGL);

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

		public void SetUIScale(float scale)
		{
			Window.SetScaleModifier(scale);
		}

		public void InitializeFonts(ModData modData)
		{
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
			using (new PerfTimer("SpriteFonts"))
			{
				fontSheetBuilder?.Dispose();
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA, 512);
				Fonts = modData.Manifest.Get<Fonts>().FontList.ToDictionary(x => x.Key,
					x => new SpriteFont(x.Value.Font, modData.DefaultFileSystem.Open(x.Value.Font).ReadAllBytes(),
										x.Value.Size, x.Value.Ascender, Window.EffectiveWindowScale, fontSheetBuilder)).AsReadOnly();
			}

			Window.OnWindowScaleChanged += (oldNative, oldEffective, newNative, newEffective) =>
			{
				Game.RunAfterTick(() =>
				{
					ChromeProvider.SetDPIScale(newEffective);

					foreach (var f in Fonts)
						f.Value.SetScale(newEffective);
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
			depthMargin = mapGrid == null || !mapGrid.EnableDepthBuffer ? 0 : mapGrid.TileSize.Height * mapGrid.MaximumTerrainHeight;
		}

		void BeginFrame()
		{
			Context.Clear();

			var surfaceSize = Window.SurfaceSize;
			var surfaceBufferSize = surfaceSize.NextPowerOf2();

			if (screenSprite == null || screenSprite.Sheet.Size != surfaceBufferSize)
			{
				screenBuffer?.Dispose();

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

			// In HiDPI windows we follow Apple's convention of defining window coordinates as for standard resolution windows
			// but to have a higher resolution backing surface with more than 1 texture pixel per viewport pixel.
			// We must convert the surface buffer size to a viewport size - in general this is NOT just the window size
			// rounded to the next power of two, as the NextPowerOf2 calculation is done in the surface pixel coordinates
			var scale = Window.EffectiveWindowScale;
			var bufferSize = new Size((int)(surfaceBufferSize.Width / scale), (int)(surfaceBufferSize.Height / scale));
			if (lastBufferSize != bufferSize)
			{
				SpriteRenderer.SetViewportParams(bufferSize, 0f, 0f, int2.Zero);
				lastBufferSize = bufferSize;
			}
		}

		public void BeginWorld(Rectangle worldViewport)
		{
			if (renderType != RenderType.None)
				throw new InvalidOperationException("BeginWorld called with renderType = {0}, expected RenderType.None.".F(renderType));

			BeginFrame();

			var worldBufferSize = worldViewport.Size.NextPowerOf2();
			if (worldSprite == null || worldSprite.Sheet.Size != worldBufferSize)
			{
				worldBuffer?.Dispose();

				// Render the world into a framebuffer at 1:1 scaling to allow the depth buffer to match the artwork at all zoom levels
				worldBuffer = Context.CreateFrameBuffer(worldBufferSize);

				// Pixel art scaling mode is a customized bilinear sampling
				worldBuffer.Texture.ScaleFilter = TextureScaleFilter.Linear;
			}

			if (worldSprite == null || worldViewport.Size != worldSprite.Bounds.Size)
			{
				var worldSheet = new Sheet(SheetType.BGRA, worldBuffer.Texture);
				worldSprite = new Sprite(worldSheet, new Rectangle(int2.Zero, worldViewport.Size), TextureChannel.RGBA);
			}

			worldBuffer.Bind();

			if (worldBufferSize != lastWorldBufferSize || lastWorldViewport != worldViewport)
			{
				var depthScale = worldBufferSize.Height / (worldBufferSize.Height + depthMargin);
				WorldSpriteRenderer.SetViewportParams(worldBufferSize, depthScale, depthScale / 2, worldViewport.Location);
				WorldModelRenderer.SetViewportParams(worldBufferSize, worldViewport.Location);

				lastWorldViewport = worldViewport;
				lastWorldBufferSize = worldBufferSize;
			}

			renderType = RenderType.World;
		}

		public void BeginUI()
		{
			if (renderType == RenderType.World)
			{
				// Complete world rendering
				Flush();
				worldBuffer.Unbind();

				// Render the world buffer into the UI buffer
				screenBuffer.Bind();

				var scale = Window.EffectiveWindowScale;
				var bufferSize = new Size((int)(screenSprite.Bounds.Width / scale), (int)(-screenSprite.Bounds.Height / scale));

				SpriteRenderer.SetAntialiasingPixelsPerTexel(Window.SurfaceSize.Height * 1f / worldSprite.Bounds.Height);
				RgbaSpriteRenderer.DrawSprite(worldSprite, float3.Zero, new float2(bufferSize));
				Flush();
				SpriteRenderer.SetAntialiasingPixelsPerTexel(0);
			}
			else
			{
				// World rendering was skipped
				BeginFrame();
				screenBuffer.Bind();
			}

			renderType = RenderType.UI;
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
			if (renderType != RenderType.UI)
				throw new InvalidOperationException("EndFrame called with renderType = {0}, expected RenderType.UI.".F(renderType));

			Flush();

			screenBuffer.Unbind();

			// Render the compositor buffers to the screen
			// HACK / PERF: Fudge the coordinates to cover the actual window while keeping the buffer viewport parameters
			// This saves us two redundant (and expensive) SetViewportParams each frame
			RgbaSpriteRenderer.DrawSprite(screenSprite, new float3(0, lastBufferSize.Height, 0), new float3(lastBufferSize.Width, -lastBufferSize.Height, 0));
			Flush();

			Window.PumpInput(inputHandler);
			Context.Present();

			renderType = RenderType.None;
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

		public Size Resolution { get { return Window.EffectiveWindowSize; } }
		public Size NativeResolution { get { return Window.NativeWindowSize; } }
		public float WindowScale { get { return Window.EffectiveWindowScale; } }
		public float NativeWindowScale { get { return Window.NativeWindowScale; } }
		public GLProfile GLProfile { get { return Window.GLProfile; } }
		public GLProfile[] SupportedGLProfiles { get { return Window.SupportedGLProfiles; } }

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
				currentBatchRenderer?.Flush();
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

			if (renderType == RenderType.World)
				worldBuffer.EnableScissor(rect);
			else
				Context.EnableScissor(rect.X, rect.Y, rect.Width, rect.Height);

			scissorState.Push(rect);
		}

		public void DisableScissor()
		{
			scissorState.Pop();
			Flush();

			if (renderType == RenderType.World)
			{
				// Restore previous scissor rect
				if (scissorState.Any())
					worldBuffer.EnableScissor(scissorState.Peek());
				else
					worldBuffer.DisableScissor();
			}
			else
			{
				// Restore previous scissor rect
				if (scissorState.Any())
				{
					var rect = scissorState.Peek();
					Context.EnableScissor(rect.X, rect.Y, rect.Width, rect.Height);
				}
				else
					Context.DisableScissor();
			}
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

		public void EnableAntialiasingFilter()
		{
			if (renderType != RenderType.UI)
				throw new InvalidOperationException("EndFrame called with renderType = {0}, expected RenderType.UI.".F(renderType));

			Flush();
			SpriteRenderer.SetAntialiasingPixelsPerTexel(Window.EffectiveWindowScale);
		}

		public void DisableAntialiasingFilter()
		{
			if (renderType != RenderType.UI)
				throw new InvalidOperationException("EndFrame called with renderType = {0}, expected RenderType.UI.".F(renderType));

			Flush();
			SpriteRenderer.SetAntialiasingPixelsPerTexel(0);
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

			ThreadPool.QueueUserWorkItem(_ =>
			{
				// Extract the screen rect from the (larger) backing surface
				var dest = new byte[4 * destWidth * destHeight];
				for (var y = 0; y < destHeight; y++)
					Array.Copy(src, 4 * y * srcWidth, dest, 4 * y * destWidth, 4 * destWidth);

				new Png(dest, SpriteFrameType.Bgra32, destWidth, destHeight).Save(path);
			});
		}

		public void Dispose()
		{
			WorldModelRenderer.Dispose();
			tempBuffer.Dispose();
			fontSheetBuilder?.Dispose();
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
			Window.Dispose();
		}

		public void SetVSyncEnabled(bool enabled)
		{
			Window.Context.SetVSyncEnabled(enabled);
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

		public int DisplayCount
		{
			get { return Window.DisplayCount; }
		}

		public int CurrentDisplay
		{
			get { return Window.CurrentDisplay; }
		}
	}
}
