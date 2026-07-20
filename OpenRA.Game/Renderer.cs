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
		public IRenderer[] WorldRenderers = [];
		public RgbaColorRenderer RgbaColorRenderer { get; }
		public SpriteRenderer SpriteRenderer { get; }
		public RgbaSpriteRenderer RgbaSpriteRenderer { get; }

		public bool WindowHasInputFocus => Window.HasInputFocus;
		public bool WindowIsSuspended => Window.IsSuspended;

		public IReadOnlyDictionary<string, SpriteFont> Fonts;

		internal IPlatformWindow Window { get; }
		internal IGraphicsContext Context { get; }

		internal int TempVertexBufferSize { get; }
		internal int TempIndexBufferSize { get; }

		readonly IVertexBuffer<Vertex> tempVertexBuffer;
		readonly IIndexBuffer quadIndexBuffer;
		readonly Stack<Rectangle> scissorState = [];
		readonly ITexture bufferSnapshot;
		readonly FrameBufferBlitter frameBufferBlitter;

		IFrameBuffer screenBuffer;
		Sprite screenSprite;
		bool captureScreenBuffer;
		bool renderToScreenBuffer;
		int activeFrameRenderScale = 1;

		IFrameBuffer worldBuffer;
		Sheet worldSheet;
		Sprite worldSprite;
		Size lastMaximumViewportSize;
		Size lastWorldViewportSize;

		public Size WorldFrameBufferSize => worldSheet.Size;
		public int WorldDownscaleFactor { get; private set; } = 1;
		public int MinimumWorldDownscaleFactor { get; private set; }
		public int FrameRenderScale { get; private set; }
		public bool UseNearestNeighborScaling { get; private set; }
		public bool UseSimpleWorldScaling { get; private set; }
		public bool DirectRenderToDisplay { get; private set; }
		public bool UseWorldNativeBlit { get; private set; }
		public bool DrawWorldShadows { get; private set; }
		public bool EnableWorldPostProcessing { get; private set; }
		public bool AntialiasWorldOverlays { get; private set; }
		public bool PrioritizeGameTick { get; private set; }

		/// <summary>
		/// Copies and returns the currently rendered state as a temporary texture.
		/// </summary>
		public ITexture GetRenderBufferSnapshot()
		{
			var size = renderType == RenderType.World
				? worldSheet.Size
				: renderToScreenBuffer ? screenSprite.Sheet.Size : Window.SurfaceSize;
			bufferSnapshot.SetDataFromReadBuffer(new Rectangle(int2.Zero, size));
			return bufferSnapshot;
		}

		SheetBuilder fontSheetBuilder;
		readonly IPlatform platform;

		float depthMargin;

		Size lastBufferSize = new(-1, -1);
		bool lastBufferFlipped;

		Rectangle lastWorldViewport;
		float2 lastViewportLocation;
		ITexture currentPaletteTexture;
		int currentPaletteHeight = 0;
		IBatchRenderer currentBatchRenderer;
		RenderType renderType = RenderType.None;

		public Renderer(IPlatform platform, GraphicSettings graphicSettings, int vertexBatchSize)
		{
			this.platform = platform;
			MinimumWorldDownscaleFactor = graphicSettings.WorldRenderScale.Clamp(1, 8);
			FrameRenderScale = graphicSettings.FrameRenderScale.Clamp(1, 4);
			UseNearestNeighborScaling = graphicSettings.WorldRenderNearestNeighbor;
			UseSimpleWorldScaling = graphicSettings.WorldRenderSimpleScaling;
			DirectRenderToDisplay = graphicSettings.DirectRenderToDisplay;
			UseWorldNativeBlit = graphicSettings.WorldRenderNativeBlit;
			DrawWorldShadows = graphicSettings.WorldRenderShadows;
			EnableWorldPostProcessing = graphicSettings.WorldRenderPostProcessing;
			AntialiasWorldOverlays = graphicSettings.WorldRenderOverlayAntialiasing;
			PrioritizeGameTick = graphicSettings.PrioritizeGameTick;
			var resolution = GetResolution(graphicSettings);

			TempVertexBufferSize = vertexBatchSize - vertexBatchSize % 4;
			TempIndexBufferSize = TempVertexBufferSize / 4 * 6;

			Window = platform.CreateWindow(new Size(resolution.Width, resolution.Height),
				graphicSettings.Mode, graphicSettings.UIScale, TempVertexBufferSize, TempIndexBufferSize,
				graphicSettings.VideoDisplay, graphicSettings.GLProfile);

			Context = Window.Context;

			var combinedBindings = new CombinedShaderBindings();
			WorldSpriteRenderer = new SpriteRenderer(this, Context.CreateShader(combinedBindings));
			WorldRgbaSpriteRenderer = new RgbaSpriteRenderer(WorldSpriteRenderer);
			WorldRgbaColorRenderer = new RgbaColorRenderer(WorldSpriteRenderer);
			SpriteRenderer = new SpriteRenderer(this, Context.CreateShader(combinedBindings));
			RgbaSpriteRenderer = new RgbaSpriteRenderer(SpriteRenderer);
			RgbaColorRenderer = new RgbaColorRenderer(SpriteRenderer);

			tempVertexBuffer = Context.CreateEmptyVertexBuffer<Vertex>(TempVertexBufferSize);
			quadIndexBuffer = Context.CreateIndexBuffer(Util.CreateQuadIndices(TempIndexBufferSize / 6));
			bufferSnapshot = Context.CreateTexture();
			frameBufferBlitter = new FrameBufferBlitter(this);
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

		public void ApplyPerformanceSettings(GraphicSettings settings)
		{
			var downscaleFactor = settings.WorldRenderScale.Clamp(1, 8);
			var recreateWorldBuffer = MinimumWorldDownscaleFactor != downscaleFactor ||
				UseNearestNeighborScaling != settings.WorldRenderNearestNeighbor;

			MinimumWorldDownscaleFactor = downscaleFactor;
			FrameRenderScale = settings.FrameRenderScale.Clamp(1, 4);
			UseNearestNeighborScaling = settings.WorldRenderNearestNeighbor;
			UseSimpleWorldScaling = settings.WorldRenderSimpleScaling;
			DirectRenderToDisplay = settings.DirectRenderToDisplay;
			UseWorldNativeBlit = settings.WorldRenderNativeBlit;
			DrawWorldShadows = settings.WorldRenderShadows;
			EnableWorldPostProcessing = settings.WorldRenderPostProcessing;
			AntialiasWorldOverlays = settings.WorldRenderOverlayAntialiasing;
			PrioritizeGameTick = settings.PrioritizeGameTick;

			if (recreateWorldBuffer && lastMaximumViewportSize.Width > 0 && lastMaximumViewportSize.Height > 0)
			{
				worldSprite = null;
				lastWorldViewport = Rectangle.Empty;
				SetMaximumViewportSize(lastMaximumViewportSize);
			}
		}

		public void InitializeFonts(ModData modData)
		{
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
			using (new PerfTimer("SpriteFonts"))
			{
				fontSheetBuilder?.Dispose();
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA, modData.Manifest.RendererConstants.FontSheetSize);
				Fonts = modData.GetOrCreate<Fonts>().FontList.ToDictionary(x => x.Key,
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

		public void SetDepthMargin(float depthMargin)
		{
			this.depthMargin = depthMargin;
		}

		void BeginFrame(bool worldWillRender)
		{
			activeFrameRenderScale = captureScreenBuffer ? 1 : FrameRenderScale;
			renderToScreenBuffer = !DirectRenderToDisplay || activeFrameRenderScale > 1 || captureScreenBuffer;
			captureScreenBuffer = false;
			if (worldWillRender && !renderToScreenBuffer)
				Context.Synchronize();
			else
				Context.Clear();

			var surfaceSize = Window.SurfaceSize;

			var contentSize = new Size(
				(surfaceSize.Width + activeFrameRenderScale - 1) / activeFrameRenderScale,
				(surfaceSize.Height + activeFrameRenderScale - 1) / activeFrameRenderScale);
			var surfaceBufferSize = contentSize.NextPowerOf2();

			if (screenSprite == null || screenSprite.Sheet.Size != surfaceBufferSize)
			{
				screenBuffer?.Dispose();

				// Render the screen into a frame buffer to simplify reading back screenshots
				screenBuffer = Context.CreateFrameBuffer(surfaceBufferSize, Color.FromArgb(0xFF, 0, 0, 0));
			}

			if (screenSprite == null || contentSize.Width != screenSprite.Bounds.Width || -contentSize.Height != screenSprite.Bounds.Height)
			{
				var screenSheet = new Sheet(SheetType.BGRA, screenBuffer.Texture);

				// Flip sprite in Y to match OpenGL's bottom-left origin
				var screenBounds = Rectangle.FromLTRB(0, contentSize.Height, contentSize.Width, 0);
				screenSprite = new Sprite(screenSheet, screenBounds, TextureChannel.RGBA);
			}

			// In HiDPI windows we follow Apple's convention of defining window coordinates as for standard resolution windows
			// but to have a higher resolution backing surface with more than 1 texture pixel per viewport pixel.
			// We must convert the surface buffer size to a viewport size - in general this is NOT just the window size
			// rounded to the next power of two, as the NextPowerOf2 calculation is done in the surface pixel coordinates
			var scale = Window.EffectiveWindowScale;
			var bufferSize = renderToScreenBuffer
				? new Size(
					(int)(activeFrameRenderScale * surfaceBufferSize.Width / scale),
					(int)(activeFrameRenderScale * surfaceBufferSize.Height / scale))
				: Window.EffectiveWindowSize;
			if (lastBufferSize != bufferSize || lastBufferFlipped == renderToScreenBuffer)
			{
				SpriteRenderer.SetViewportParams(bufferSize, 1, 0f, int2.Zero, !renderToScreenBuffer);
				lastBufferSize = bufferSize;
				lastBufferFlipped = !renderToScreenBuffer;
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
			var scaledSize = new Size(
				size.Width / MinimumWorldDownscaleFactor + 1,
				size.Height / MinimumWorldDownscaleFactor + 1);

			Size worldBufferSize;
			if (depthMargin == 0)
			{
				var surfaceSize = Window.SurfaceSize;
				var maximumSize = new Size(
					2 * surfaceSize.Width / MinimumWorldDownscaleFactor + 1,
					2 * surfaceSize.Height / MinimumWorldDownscaleFactor + 1);
				worldBufferSize = new Size(
					Math.Min(scaledSize.Width, maximumSize.Width),
					Math.Min(scaledSize.Height, maximumSize.Height)).NextPowerOf2();
			}
			else
				worldBufferSize = scaledSize.NextPowerOf2();

			if (worldSprite == null || worldSheet.Size != worldBufferSize)
			{
				worldBuffer?.Dispose();

				// If enableWorldFrameBufferDownscale and the world is more than twice the size of the final output size do we allow it to be downsampled!
				worldBuffer = Context.CreateFrameBuffer(worldBufferSize);

				// Nearest-neighbour mode uses the cheapest possible upscale. Filtered
				// rendering uses a customized bilinear pixel-art filter in BeginUI.
				worldBuffer.Texture.ScaleFilter = UseNearestNeighborScaling ? TextureScaleFilter.Nearest : TextureScaleFilter.Linear;
				worldSheet = new Sheet(SheetType.BGRA, worldBuffer.Texture);

				// Invalidate cached state to force a shader update
				lastWorldViewport = Rectangle.Empty;
				worldSprite = null;
			}

			lastMaximumViewportSize = size;
		}

		public void BeginWorld(float2 viewportLocation, Size viewportSize)
		{
			if (renderType != RenderType.None)
				throw new InvalidOperationException($"BeginWorld called with renderType = {renderType}, expected RenderType.None.");

			BeginFrame(true);

			if (worldSheet == null)
				throw new InvalidOperationException("BeginWorld called before SetMaximumViewportSize has been set.");

			var centerLocation = viewportLocation.ToInt2();
			if (worldSprite == null || viewportSize != lastWorldViewportSize || viewportLocation != lastViewportLocation)
			{
				lastViewportLocation = viewportLocation;
				lastWorldViewportSize = viewportSize;

				// Downscale world rendering if needed to fit within the framebuffer
				var vw = viewportSize.Width;
				var vh = viewportSize.Height;
				var bw = worldSheet.Size.Width;
				var bh = worldSheet.Size.Height;
				WorldDownscaleFactor = MinimumWorldDownscaleFactor;
				while (vw / WorldDownscaleFactor > bw || vh / WorldDownscaleFactor > bh)
					WorldDownscaleFactor++;

				// We need to add 1 to scroll in order to handle interpixel 0-0.99 fractionalOffset.
				var s = new Size(vw / WorldDownscaleFactor + 1, vh / WorldDownscaleFactor + 1);
				var fractionalOffset = centerLocation - viewportLocation;

				// If scaling by an integer factor (including 1:1) we must round the offset
				// to an integer number of screen-space pixels to preserve sharp pixel edges
				var renderScale = screenSprite.Size.X / (s.Width - 1f);
				if (float.IsInteger(renderScale))
					fractionalOffset = (fractionalOffset * renderScale).Round() / renderScale;

				worldSprite = new Sprite(worldSheet, new Rectangle(int2.Zero, s), 0, fractionalOffset,
					TextureChannel.RGBA, BlendMode.None);
			}

			worldBuffer.Bind();
			var rect = new Rectangle(centerLocation, viewportSize);
			if (lastWorldViewport != rect)
			{
				var topLeft = centerLocation - viewportSize.ToInt2() / 2;
				WorldSpriteRenderer.SetViewportParams(worldSheet.Size, WorldDownscaleFactor, depthMargin, topLeft);
				lastWorldViewport = rect;
			}

			renderType = RenderType.World;
		}

		void DrawWorldBufferToScreen()
		{
			if (UseWorldNativeBlit && (UseNearestNeighborScaling || UseSimpleWorldScaling) && !renderToScreenBuffer)
			{
				// The low-quality path can snap fractional camera offsets to framebuffer
				// pixels and let the driver perform the upscale without a fragment shader.
				var sx = ((int)Math.Round(-worldSprite.Offset.X)).Clamp(0, worldSheet.Size.Width - 1);
				var sy = ((int)Math.Round(-worldSprite.Offset.Y)).Clamp(0, worldSheet.Size.Height - 1);
				var sw = ((int)worldSprite.Size.X - 1).Clamp(1, worldSheet.Size.Width - sx);
				var sh = ((int)worldSprite.Size.Y - 1).Clamp(1, worldSheet.Size.Height - sy);
				var source = new Rectangle(sx, sy, sw, sh);
				var surfaceSize = Window.SurfaceSize;
				var destination = Rectangle.FromLTRB(0, surfaceSize.Height, surfaceSize.Width, 0);
				var filter = UseNearestNeighborScaling ? TextureScaleFilter.Nearest : TextureScaleFilter.Linear;
				worldBuffer.BlitToDefault(source, destination, filter);
				return;
			}

			if (renderToScreenBuffer)
				screenBuffer.Bind();

			// We added 1 to worldSprite now we need to subtract.
			var resolution = Window.EffectiveWindowSize;
			var bufferScale = new float2(
				resolution.Width / (worldSprite.Size.X - 1),
				resolution.Height / (worldSprite.Size.Y - 1));

			if (UseNearestNeighborScaling || UseSimpleWorldScaling)
			{
				var location = bufferScale * worldSprite.Offset.XY;
				var size = bufferScale * worldSprite.Size.XY;
				frameBufferBlitter.Draw(worldSprite, location, size, lastBufferSize, !renderToScreenBuffer);
			}
			else
			{
				SpriteRenderer.EnablePixelArtScaling(true);
				RgbaSpriteRenderer.DrawSprite(worldSprite, float3.Zero, new float3(bufferScale, 1f));
				Flush();
				SpriteRenderer.EnablePixelArtScaling(false);
			}
		}

		public void BeginUI()
		{
			if (renderType == RenderType.World)
			{
				// Complete world rendering
				Flush();
				worldBuffer.Unbind();
				DrawWorldBufferToScreen();
			}
			else
			{
				// World rendering was skipped
				BeginFrame(false);
				if (renderToScreenBuffer)
					screenBuffer.Bind();
			}

			renderType = RenderType.UI;
		}

		public void SetPalette(HardwarePalette palette)
		{
			// Note: palette.Texture and palette.ColorShifts are updated at the same time
			// so we only need to check one of the two to know whether we must update the textures
			// also compare heights in case new palettes have been added
			if (palette.Texture == currentPaletteTexture && palette.Height == currentPaletteHeight)
				return;

			Flush();
			currentPaletteTexture = palette.Texture;
			currentPaletteHeight = palette.Height;

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

			if (renderToScreenBuffer)
			{
				screenBuffer.Unbind();

				// Drivers can optimize framebuffer copies (especially software renderers)
				// much more aggressively than a full-screen fragment shader.
				var surfaceSize = Window.SurfaceSize;
				var source = new Rectangle(0, 0, screenSprite.Bounds.Width, -screenSprite.Bounds.Height);
				var destination = Rectangle.FromLTRB(0, surfaceSize.Height, surfaceSize.Width, 0);
				var filter = UseNearestNeighborScaling ? TextureScaleFilter.Nearest : TextureScaleFilter.Linear;
				screenBuffer.BlitToDefault(source, destination, filter);
			}

			Window.PumpInput(inputHandler);
			Context.Present();

			renderType = RenderType.None;
		}

		public bool CurrentFrameUsesScreenBuffer => renderToScreenBuffer;

		public void CaptureScreenBufferForNextFrame()
		{
			captureScreenBuffer = true;
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

		public IVertexBuffer<T> CreateVertexBuffer<T>(T[] data, bool dynamic) where T : struct
		{
			return Context.CreateVertexBuffer(data, dynamic);
		}

		Rectangle ScaleUIScissor(Rectangle rect)
		{
			var scale = Window.EffectiveWindowScale / activeFrameRenderScale;
			return Rectangle.FromLTRB(
				(int)Math.Floor(scale * rect.Left),
				(int)Math.Floor(scale * rect.Top),
				(int)Math.Ceiling(scale * rect.Right),
				(int)Math.Ceiling(scale * rect.Bottom));
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
			else if (renderToScreenBuffer)
				screenBuffer.EnableScissor(ScaleUIScissor(rect));
			else
				Context.EnableScissor(rect.X, Resolution.Height - rect.Bottom, rect.Width, rect.Height);

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
					if (renderToScreenBuffer)
						screenBuffer.EnableScissor(ScaleUIScissor(rect));
					else
						Context.EnableScissor(rect.X, Resolution.Height - rect.Bottom, rect.Width, rect.Height);
				}
				else
				{
					if (renderToScreenBuffer)
						screenBuffer.DisableScissor();
					else
						Context.DisableScissor();
				}
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
			SpriteRenderer.EnablePixelArtScaling(true);
		}

		public void DisableAntialiasingFilter()
		{
			if (renderType != RenderType.UI)
				throw new InvalidOperationException($"EndFrame called with renderType = {renderType}, expected RenderType.UI.");

			Flush();
			SpriteRenderer.EnablePixelArtScaling(false);
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
			screenBuffer?.Dispose();
			bufferSnapshot.Dispose();
			frameBufferBlitter.Dispose();
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

		public bool TryOpenUrl(string url)
		{
			return Window.TryOpenUrl(url);
		}

		public string GLVersion => Context.GLVersion;

		public int DisplayCount => Window.DisplayCount;

		public int CurrentDisplay => Window.CurrentDisplay;
	}
}
