#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

		public SpriteRenderer WorldSpriteRenderer { get; }
		public RgbaSpriteRenderer WorldRgbaSpriteRenderer { get; }
		public RgbaColorRenderer WorldRgbaColorRenderer { get; }
		public IRenderer[] WorldRenderers = Array.Empty<IRenderer>();
		public RgbaColorRenderer RgbaColorRenderer { get; }
		public SpriteRenderer SpriteRenderer { get; }
		public RgbaSpriteRenderer RgbaSpriteRenderer { get; }

		public bool WindowHasInputFocus => Window.HasInputFocus;
		public bool WindowIsSuspended => Window.IsSuspended;

		public IReadOnlyDictionary<string, SpriteFont> Fonts;

		internal IPlatformWindow Window { get; }
		internal IGraphicsContext Context { get; }

		internal int SheetSize { get; }
		internal int TempVertexBufferSize { get; }
		internal int TempIndexBufferSize { get; }

		readonly IVertexBuffer<Vertex> tempVertexBuffer;
		readonly IIndexBuffer quadIndexBuffer;
		readonly Stack<Rectangle> scissorState = new();
		readonly ITexture worldBufferSnapshot;

		IFrameBuffer screenBuffer;
		Sprite screenSprite;

		IFrameBuffer worldBuffer;
		Sheet worldSheet;
		Sprite worldSprite;
		Size lastMaximumViewportSize;
		Size lastWorldViewportSize;

		public Size WorldFrameBufferSize => worldSheet.Size;
		public int WorldDownscaleFactor { get; private set; } = 1;

		/// <summary>
		/// Copies and returns the currently rendered world state as a temporary texture.
		/// </summary>
		public ITexture WorldBufferSnapshot()
		{
			worldBufferSnapshot.SetDataFromReadBuffer(new Rectangle(int2.Zero, worldSheet.Size));
			return worldBufferSnapshot;
		}

		SheetBuilder fontSheetBuilder;
		readonly IPlatform platform;

		float depthMargin;

		Size lastBufferSize = new(-1, -1);

		Rectangle lastWorldViewport = Rectangle.Empty;
		ITexture currentPaletteTexture;
		IBatchRenderer currentBatchRenderer;
		RenderType renderType = RenderType.None;

		public Renderer(IPlatform platform, GraphicSettings graphicSettings)
		{
			this.platform = platform;
			var resolution = GetResolution(graphicSettings);

			TempVertexBufferSize = graphicSettings.BatchSize - graphicSettings.BatchSize % 4;
			TempIndexBufferSize = TempVertexBufferSize / 4 * 6;

			Window = platform.CreateWindow(new Size(resolution.Width, resolution.Height),
				graphicSettings.Mode, graphicSettings.UIScale, TempVertexBufferSize, TempIndexBufferSize,
				graphicSettings.VideoDisplay, graphicSettings.GLProfile);

			Context = Window.Context;

			SheetSize = graphicSettings.SheetSize;

			var combinedBindings = new CombinedShaderBindings();
			WorldSpriteRenderer = new SpriteRenderer(this, Context.CreateShader(combinedBindings));
			WorldRgbaSpriteRenderer = new RgbaSpriteRenderer(WorldSpriteRenderer);
			WorldRgbaColorRenderer = new RgbaColorRenderer(WorldSpriteRenderer);
			SpriteRenderer = new SpriteRenderer(this, Context.CreateShader(combinedBindings));
			RgbaSpriteRenderer = new RgbaSpriteRenderer(SpriteRenderer);
			RgbaColorRenderer = new RgbaColorRenderer(SpriteRenderer);

			tempVertexBuffer = Context.CreateVertexBuffer<Vertex>(TempVertexBufferSize);
			quadIndexBuffer = Context.CreateIndexBuffer(Util.CreateQuadIndices(TempIndexBufferSize / 6));
			worldBufferSnapshot = Context.CreateTexture();
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
					x => new SpriteFont(
						platform, x.Value.Font, modData.DefaultFileSystem.Open(x.Value.Font).ReadAllBytes(),
						x.Value.Size, x.Value.Ascender, Window.EffectiveWindowScale, fontSheetBuilder));
			}

			Window.OnWindowScaleChanged += (oldNative, oldEffective, newNative, newEffective) =>
			{
				Game.RunAfterTick(() =>
				{
					// Recalculate downscaling factor for the new window scale
					SetMaximumViewportSize(lastMaximumViewportSize);

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
				SpriteRenderer.SetViewportParams(bufferSize, 1, 0f, int2.Zero);
				lastBufferSize = bufferSize;
			}
		}

		public void SetMaximumViewportSize(Size size)
		{
			// Aim to render the world into a framebuffer at 1:1 scaling which is then up/downscaled using a custom
			// filter to provide crisp scaling and avoid rendering glitches when the depth buffer is used and samples don't match.
			// This approach does not scale well to large sizes, first saturating GPU fill rate and then crashing when
			// reaching the framebuffer size limits (typically 16k). We therefore clamp the maximum framebuffer size to
			// twice the window surface size, which strikes a reasonable balance between rendering quality and performance.
			// Mods that use the depth buffer must instead limit their artwork resolution or maximum zoom-out levels.
			Size worldBufferSize;
			if (depthMargin == 0)
			{
				var surfaceSize = Window.SurfaceSize;
				worldBufferSize = new Size(Math.Min(size.Width, 2 * surfaceSize.Width), Math.Min(size.Height, 2 * surfaceSize.Height)).NextPowerOf2();
			}
			else
				worldBufferSize = size.NextPowerOf2();

			if (worldSprite == null || worldSheet.Size != worldBufferSize)
			{
				worldBuffer?.Dispose();

				// If enableWorldFrameBufferDownscale and the world is more than twice the size of the final output size do we allow it to be downsampled!
				worldBuffer = Context.CreateFrameBuffer(worldBufferSize);

				// Pixel art scaling mode is a customized bilinear sampling
				worldBuffer.Texture.ScaleFilter = TextureScaleFilter.Linear;
				worldSheet = new Sheet(SheetType.BGRA, worldBuffer.Texture);

				// Invalidate cached state to force a shader update
				lastWorldViewport = Rectangle.Empty;
				worldSprite = null;
			}

			lastMaximumViewportSize = size;
		}

		public void BeginWorld(Rectangle worldViewport)
		{
			if (renderType != RenderType.None)
				throw new InvalidOperationException($"BeginWorld called with renderType = {renderType}, expected RenderType.None.");

			BeginFrame();

			if (worldSheet == null)
				throw new InvalidOperationException("BeginWorld called before SetMaximumViewportSize has been set.");

			if (worldSprite == null || worldViewport.Size != lastWorldViewportSize)
			{
				// Downscale world rendering if needed to fit within the framebuffer
				var vw = worldViewport.Size.Width;
				var vh = worldViewport.Size.Height;
				var bw = worldSheet.Size.Width;
				var bh = worldSheet.Size.Height;
				WorldDownscaleFactor = 1;
				while (vw / WorldDownscaleFactor > bw || vh / WorldDownscaleFactor > bh)
					WorldDownscaleFactor++;

				var s = new Size(vw / WorldDownscaleFactor, vh / WorldDownscaleFactor);
				worldSprite = new Sprite(worldSheet, new Rectangle(int2.Zero, s), TextureChannel.RGBA);
				lastWorldViewportSize = worldViewport.Size;
			}

			worldBuffer.Bind();

			if (lastWorldViewport != worldViewport)
			{
				WorldSpriteRenderer.SetViewportParams(worldSheet.Size, WorldDownscaleFactor, depthMargin, worldViewport.Location);
				lastWorldViewport = worldViewport;
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
				var bufferScale = new float3((int)(screenSprite.Bounds.Width / scale) / worldSprite.Size.X, (int)(-screenSprite.Bounds.Height / scale) / worldSprite.Size.Y, 1f);

				SpriteRenderer.SetAntialiasingPixelsPerTexel(Window.SurfaceSize.Height * 1f / worldSprite.Bounds.Height);
				RgbaSpriteRenderer.DrawSprite(worldSprite, float3.Zero, bufferScale);
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
			// Note: palette.Texture and palette.ColorShifts are updated at the same time
			// so we only need to check one of the two to know whether we must update the textures
			if (palette.Texture == currentPaletteTexture)
				return;

			Flush();
			currentPaletteTexture = palette.Texture;

			SpriteRenderer.SetPalette(palette);
			WorldSpriteRenderer.SetPalette(palette);

			foreach (var r in WorldRenderers)
				r.SetPalette(palette);
		}

		public void EndFrame(IInputHandler inputHandler)
		{
			if (renderType != RenderType.UI)
				throw new InvalidOperationException($"EndFrame called with renderType = {renderType}, expected RenderType.UI.");

			Flush();

			screenBuffer.Unbind();

			// Render the compositor buffers to the screen
			// HACK / PERF: Fudge the coordinates to cover the actual window while keeping the buffer viewport parameters
			// This saves us two redundant (and expensive) SetViewportParams each frame
			RgbaSpriteRenderer.DrawSprite(screenSprite, new float3(0, lastBufferSize.Height, 0), new float3(lastBufferSize.Width / screenSprite.Size.X, -lastBufferSize.Height / screenSprite.Size.Y, 1f));
			Flush();

			Window.PumpInput(inputHandler);
			Context.Present();

			renderType = RenderType.None;
		}

		public void DrawBatch<T>(IVertexBuffer<T> vertices, IShader shader,
			int firstVertex, int numVertices, PrimitiveType type)
			where T : struct
		{
			vertices.Bind();
			shader.Bind();
			Context.DrawPrimitives(type, firstVertex, numVertices);
			PerfHistory.Increment("batches", 1);
		}

		public void DrawQuadBatch(ref Vertex[] vertices, IShader shader, int numVertices)
		{
			tempVertexBuffer.SetData(ref vertices, numVertices);
			DrawQuadBatch(tempVertexBuffer, quadIndexBuffer, shader, numVertices / 4 * 6, 0);
		}

		public void DrawQuadBatch<T>(IVertexBuffer<T> vertices, IIndexBuffer indices, IShader shader, int numIndices, int start)
			where T : struct
		{
			vertices.Bind();
			indices.Bind();
			shader.Bind();
			Context.DrawElements(numIndices, start);
			PerfHistory.Increment("batches", 1);
		}

		public void Flush()
		{
			CurrentBatchRenderer = null;
		}

		public Size Resolution => Window.EffectiveWindowSize;
		public Size NativeResolution => Window.NativeWindowSize;
		public float WindowScale => Window.EffectiveWindowScale;
		public float NativeWindowScale => Window.NativeWindowScale;
		public GLProfile GLProfile => Window.GLProfile;
		public GLProfile[] SupportedGLProfiles => Window.SupportedGLProfiles;

		public interface IBatchRenderer { void Flush(); }

		public IBatchRenderer CurrentBatchRenderer
		{
			get => currentBatchRenderer;

			set
			{
				if (currentBatchRenderer == value)
					return;
				currentBatchRenderer?.Flush();
				currentBatchRenderer = value;
			}
		}

		public IFrameBuffer CreateFrameBuffer(Size s)
		{
			return Context.CreateFrameBuffer(s);
		}

		public IShader CreateShader(IShaderBindings bindings)
		{
			return Context.CreateShader(bindings);
		}

		public IVertexBuffer<T> CreateVertexBuffer<T>(int length) where T : struct
		{
			return Context.CreateVertexBuffer<T>(length);
		}

		public void EnableScissor(Rectangle rect)
		{
			// Must remain inside the current scissor rect
			if (scissorState.Count > 0)
				rect = Rectangle.Intersect(rect, scissorState.Peek());

			Flush();

			if (renderType == RenderType.World)
			{
				var r = Rectangle.FromLTRB(
					rect.Left / WorldDownscaleFactor,
					rect.Top / WorldDownscaleFactor,
					(rect.Right + WorldDownscaleFactor - 1) / WorldDownscaleFactor,
					(rect.Bottom + WorldDownscaleFactor - 1) / WorldDownscaleFactor);
				worldBuffer.EnableScissor(r);
			}
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
				if (scissorState.Count > 0)
				{
					var rect = scissorState.Peek();
					var r = Rectangle.FromLTRB(
						rect.Left / WorldDownscaleFactor,
						rect.Top / WorldDownscaleFactor,
						(rect.Right + WorldDownscaleFactor - 1) / WorldDownscaleFactor,
						(rect.Bottom + WorldDownscaleFactor - 1) / WorldDownscaleFactor);
					worldBuffer.EnableScissor(r);
				}
				else
					worldBuffer.DisableScissor();
			}
			else
			{
				// Restore previous scissor rect
				if (scissorState.Count > 0)
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
				throw new InvalidOperationException($"EndFrame called with renderType = {renderType}, expected RenderType.UI.");

			Flush();
			SpriteRenderer.SetAntialiasingPixelsPerTexel(Window.EffectiveWindowScale);
		}

		public void DisableAntialiasingFilter()
		{
			if (renderType != RenderType.UI)
				throw new InvalidOperationException($"EndFrame called with renderType = {renderType}, expected RenderType.UI.");

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
			worldBuffer?.Dispose();
			screenBuffer.Dispose();
			worldBufferSnapshot.Dispose();
			tempVertexBuffer.Dispose();
			quadIndexBuffer.Dispose();
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

		public string GLVersion => Context.GLVersion;

		public int DisplayCount => Window.DisplayCount;

		public int CurrentDisplay => Window.CurrentDisplay;
	}
}
