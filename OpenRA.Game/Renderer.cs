#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using System.Reflection;
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

		Size? lastResolution;
		int2? lastScroll;
		float? lastZoom;
		ITexture currentPaletteTexture;
		IBatchRenderer currentBatchRenderer;

		public Renderer(GraphicSettings graphicSettings, ServerSettings serverSettings)
		{
			var resolution = GetResolution(graphicSettings);

			var rendererName = serverSettings.Dedicated ? "Null" : graphicSettings.Renderer;
			var rendererPath = Platform.ResolvePath(".", "OpenRA.Platforms." + rendererName + ".dll");

			Device = CreateDevice(Assembly.LoadFile(rendererPath), resolution.Width, resolution.Height, graphicSettings.Mode);

			if (!serverSettings.Dedicated)
			{
				TempBufferSize = graphicSettings.BatchSize;
				SheetSize = graphicSettings.SheetSize;
			}

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

		static IGraphicsDevice CreateDevice(Assembly platformDll, int width, int height, WindowMode window)
		{
			foreach (PlatformAttribute r in platformDll.GetCustomAttributes(typeof(PlatformAttribute), false))
			{
				var factory = (IDeviceFactory)r.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
				return factory.CreateGraphics(new Size(width, height), window);
			}

			throw new InvalidOperationException("Renderer DLL is missing RendererAttribute to tell us what type to use!");
		}

		public void InitializeFonts(Manifest m)
		{
			using (new Support.PerfTimer("SpriteFonts"))
			{
				if (fontSheetBuilder != null)
					fontSheetBuilder.Dispose();
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA);
				Fonts = m.Fonts.ToDictionary(x => x.Key,
					x => new SpriteFont(Platform.ResolvePath(x.Value.First), x.Value.Second, fontSheetBuilder)).AsReadOnly();
			}
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
				RgbaSpriteRenderer.SetViewportParams(Resolution, 1f, int2.Zero);
				SpriteRenderer.SetViewportParams(Resolution, 1f, int2.Zero);
				RgbaColorRenderer.SetViewportParams(Resolution, 1f, int2.Zero);
			}

			// If zoom evaluates as different due to floating point weirdness that's OK, setting the parameters again is harmless.
			if (resolutionChanged || lastScroll != scroll || lastZoom != zoom)
			{
				lastScroll = scroll;
				lastZoom = zoom;
				WorldRgbaSpriteRenderer.SetViewportParams(Resolution, zoom, scroll);
				WorldSpriteRenderer.SetViewportParams(Resolution, zoom, scroll);
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

		public interface IBatchRenderer { void Flush();	}

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
		}

		public string GetClipboardText()
		{
			return Device.GetClipboardText();
		}

		public bool SetClipboardText(string text)
		{
			return Device.SetClipboardText(text);
		}
	}
}
