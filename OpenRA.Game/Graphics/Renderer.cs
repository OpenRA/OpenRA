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
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	public class Renderer
	{
		internal static int SheetSize;
		internal static int TempBufferSize;
		internal static int TempBufferCount;
		internal IShader WorldSpriteShader { get; private set; }
		internal IShader WorldLineShader { get; private set; }
		internal IShader LineShader { get; private set; }
		internal IShader RgbaSpriteShader { get; private set; }
		internal IShader SpriteShader { get; private set; }

		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public LineRenderer WorldLineRenderer { get; private set; }
		public LineRenderer LineRenderer { get; private set; }
		public SpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }

		public ITexture PaletteTexture;

		Queue<IVertexBuffer<Vertex>> tempBuffers = new Queue<IVertexBuffer<Vertex>>();

		public Dictionary<string, SpriteFont> Fonts;

		public Renderer()
		{
			TempBufferSize = Game.Settings.Graphics.BatchSize;
			TempBufferCount = Game.Settings.Graphics.NumTempBuffers;
			SheetSize = Game.Settings.Graphics.SheetSize;

			WorldSpriteShader = device.CreateShader("shp");
			WorldLineShader = device.CreateShader("line");
			LineShader = device.CreateShader("line");
			RgbaSpriteShader = device.CreateShader("rgba");
			SpriteShader = device.CreateShader("shp");

			WorldSpriteRenderer = new SpriteRenderer(this, WorldSpriteShader);
			WorldLineRenderer = new LineRenderer(this, WorldLineShader);
			LineRenderer = new LineRenderer(this, LineShader);
			RgbaSpriteRenderer = new SpriteRenderer(this, RgbaSpriteShader);
			SpriteRenderer = new SpriteRenderer(this, SpriteShader);

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
			float2 r1 = new float2(2f/Resolution.Width, -2f/Resolution.Height);
			float2 r2 = new float2(-1, 1);
			var zr1 = zoom*r1;

			SetShaderParams(WorldSpriteShader, zr1, r2, scroll);
			SetShaderParams(WorldLineShader, zr1, r2, scroll);
			SetShaderParams(LineShader, r1, r2, float2.Zero);
			SetShaderParams(RgbaSpriteShader, r1, r2, float2.Zero);
			SetShaderParams(SpriteShader, r1, r2, float2.Zero);
		}

		void SetShaderParams(IShader s, float2 r1, float2 r2, float2 scroll)
		{
			s.SetValue("Palette", PaletteTexture);
			s.SetValue("Scroll", (int)scroll.X, (int)scroll.Y);
			s.SetValue("r1", r1.X, r1.Y);
			s.SetValue("r2", r2.X, r2.Y);
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

		public static Size Resolution { get { return device.WindowSize; } }

		// Work around a bug in OSX 10.6.8 / mono 2.10.2 / SDL 1.2.14
		// which makes the window non-interactive in Windowed/Pseudofullscreen mode.
		static Screen FixOSX() { return Screen.PrimaryScreen; }

		internal static void Initialize(WindowMode windowMode)
		{
			if (Platform.CurrentPlatform == PlatformType.OSX)
				FixOSX();

			var resolution = GetResolution(windowMode);
			
			string renderer = Game.Settings.Server.Dedicated ? "Null" : Game.Settings.Graphics.Renderer;
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

		public void EnableScissor(int left, int top, int width, int height)
		{
			Flush();
			Device.EnableScissor(left, top, width, height);
		}

		public void DisableScissor()
		{
			Flush();
			Device.DisableScissor();
		}
	}
}
