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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	public class Renderer
	{
		internal static int SheetSize;
		internal static int TempBufferSize;
		internal static int TempBufferCount;

		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public SpriteRenderer WorldRgbaSpriteRenderer { get; private set; }
		public QuadRenderer WorldQuadRenderer { get; private set; }
		public LineRenderer WorldLineRenderer { get; private set; }
		public VoxelRenderer WorldVoxelRenderer { get; private set; }
		public LineRenderer LineRenderer { get; private set; }
		public SpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }

		Queue<IVertexBuffer<Vertex>> tempBuffers = new Queue<IVertexBuffer<Vertex>>();

		public Dictionary<string, SpriteFont> Fonts;
		Stack<Rectangle> scissorState;

		public Renderer()
		{
			TempBufferSize = Game.Settings.Graphics.BatchSize;
			TempBufferCount = Game.Settings.Graphics.NumTempBuffers;
			SheetSize = Game.Settings.Graphics.SheetSize;
			scissorState = new Stack<Rectangle>();

			WorldSpriteRenderer = new SpriteRenderer(this, device.CreateShader("shp"));
			WorldRgbaSpriteRenderer = new SpriteRenderer(this, device.CreateShader("rgba"));
			WorldLineRenderer = new LineRenderer(this, device.CreateShader("line"));
			WorldVoxelRenderer = new VoxelRenderer(this, device.CreateShader("vxl"));
			LineRenderer = new LineRenderer(this, device.CreateShader("line"));
			WorldQuadRenderer = new QuadRenderer(this, device.CreateShader("line"));
			RgbaSpriteRenderer = new SpriteRenderer(this, device.CreateShader("rgba"));
			SpriteRenderer = new SpriteRenderer(this, device.CreateShader("shp"));

			for (int i = 0; i < TempBufferCount; i++)
				tempBuffers.Enqueue(device.CreateVertexBuffer(TempBufferSize));
		}

		public void InitializeFonts(Manifest m)
		{
			Fonts = m.Fonts.ToDictionary(x => x.Key, x => new SpriteFont(x.Value.First, x.Value.Second));
		}

		internal IGraphicsDevice Device { get { return device; } }

		public void BeginFrame(float2 scroll, float zoom)
		{
			device.Clear();
			WorldSpriteRenderer.SetViewportParams(Resolution, zoom, scroll);
			WorldRgbaSpriteRenderer.SetViewportParams(Resolution, zoom, scroll);
			SpriteRenderer.SetViewportParams(Resolution, 1f, float2.Zero);
			RgbaSpriteRenderer.SetViewportParams(Resolution, 1f, float2.Zero);
			WorldLineRenderer.SetViewportParams(Resolution, zoom, scroll);
			WorldQuadRenderer.SetViewportParams(Resolution, zoom, scroll);
			LineRenderer.SetViewportParams(Resolution, 1f, float2.Zero);
			WorldVoxelRenderer.SetViewportParams(Resolution, zoom, scroll);
		}

		ITexture currentPaletteTexture;
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
			device.PumpInput(inputHandler);
			device.Present();
		}

		public void DrawBatch<T>(IVertexBuffer<T> vertices,
			int firstVertex, int numVertices, PrimitiveType type)
			where T : struct
		{
			vertices.Bind();
			device.DrawPrimitives(type, firstVertex, numVertices);
			PerfHistory.Increment("batches", 1);
		}

		public void Flush()
		{
			CurrentBatchRenderer = null;
		}

		public void SetLineWidth(float width)
		{
			device.SetLineWidth(width);
		}

		static IGraphicsDevice device;

		public Size Resolution { get { return device.WindowSize; } }

		// Work around a bug in OSX 10.6.8 / mono 2.10.2 / SDL 1.2.14
		// which makes the window non-interactive in Windowed/Pseudofullscreen mode.
		static Screen FixOSX() { return Screen.PrimaryScreen; }

		internal static void Initialize(WindowMode windowMode)
		{
			if (Platform.CurrentPlatform == PlatformType.OSX)
				FixOSX();

			var resolution = GetResolution(windowMode);

			var renderer = Game.Settings.Server.Dedicated ? "Null" : Game.Settings.Graphics.Renderer;
			var rendererPath = Path.GetFullPath("OpenRA.Renderer.{0}.dll".F(renderer));

			device = CreateDevice(Assembly.LoadFile(rendererPath), resolution.Width, resolution.Height, windowMode);
		}

		static Size GetResolution(WindowMode windowmode)
		{
			var size = (windowmode == WindowMode.Windowed)
				? Game.Settings.Graphics.WindowedSize
				: Game.Settings.Graphics.FullscreenSize;
			return new Size(size.X, size.Y);
		}

		static IGraphicsDevice CreateDevice(Assembly rendererDll, int width, int height, WindowMode window)
		{
			foreach (RendererAttribute r in rendererDll.GetCustomAttributes(typeof(RendererAttribute), false))
			{
				var factory = (IDeviceFactory)r.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
				return factory.Create(new Size(width, height), window);
			}

			throw new InvalidOperationException("Renderer DLL is missing RendererAttribute to tell us what type to use!");
		}

		internal IVertexBuffer<Vertex> GetTempVertexBuffer()
		{
			var ret = tempBuffers.Dequeue();
			tempBuffers.Enqueue(ret);
			return ret;
		}

		public interface IBatchRenderer	{ void Flush();	}

		static IBatchRenderer currentBatchRenderer;
		public static IBatchRenderer CurrentBatchRenderer
		{
			get { return currentBatchRenderer; }
			set
			{
				if (currentBatchRenderer == value) return;
				if (currentBatchRenderer != null)
					currentBatchRenderer.Flush();
				currentBatchRenderer = value;
			}
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
	}
}
