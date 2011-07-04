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
using System.Reflection;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Support;
using System.Linq;

namespace OpenRA.Graphics
{
	public class Renderer
	{
		internal static int SheetSize;
		internal static int TempBufferSize;
		internal static int TempBufferCount;

		internal IShader SpriteShader { get; private set; }
		internal IShader LineShader { get; private set; }
		internal IShader RgbaSpriteShader { get; private set; }
		internal IShader WorldSpriteShader { get; private set; }

		public SpriteRenderer SpriteRenderer { get; private set; }
		public SpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public LineRenderer LineRenderer { get; private set; }

		public ITexture PaletteTexture;

		Queue<IVertexBuffer<Vertex>> tempBuffers = new Queue<IVertexBuffer<Vertex>>();

		public Dictionary<string, SpriteFont> Fonts;
		
		public Renderer()
		{
			TempBufferSize = Game.Settings.Graphics.BatchSize;
			TempBufferCount = Game.Settings.Graphics.NumTempBuffers;
			SheetSize = Game.Settings.Game.SheetSize;	// TODO: move to Graphics.
			
			SpriteShader = device.CreateShader("world-shp");
			LineShader = device.CreateShader("world-line");
			RgbaSpriteShader = device.CreateShader("chrome-rgba");
			WorldSpriteShader = device.CreateShader("chrome-shp");

			SpriteRenderer = new SpriteRenderer( this, SpriteShader );
			RgbaSpriteRenderer = new SpriteRenderer( this, RgbaSpriteShader );
			WorldSpriteRenderer = new SpriteRenderer( this, WorldSpriteShader );
			LineRenderer = new LineRenderer(this);
			
			for( int i = 0 ; i < TempBufferCount ; i++ )
				tempBuffers.Enqueue( device.CreateVertexBuffer( TempBufferSize ) );
		}
		
		public void InitializeFonts(Manifest m)
		{
			Fonts = m.Fonts.ToDictionary(x => x.Key, x => new SpriteFont(x.Value.First, x.Value.Second));
		}
		
		internal IGraphicsDevice Device { get { return device; } }

		public void BeginFrame(float2 scroll)
		{
			device.Clear(Color.Black);

			float2 r1 = new float2(2f/Resolution.Width, -2f/Resolution.Height);
			float2 r2 = new float2(-1, 1);

			SetShaderParams( SpriteShader, r1, r2, scroll );
			SetShaderParams( LineShader, r1, r2, scroll );
			SetShaderParams( RgbaSpriteShader, r1, r2, scroll );
			SetShaderParams( WorldSpriteShader, r1, r2, scroll );
		}

		void SetShaderParams( IShader s, float2 r1, float2 r2, float2 scroll )
		{
			s.SetValue( "Palette", PaletteTexture );
			s.SetValue( "Scroll", (int) scroll.X, (int) scroll.Y );
			s.SetValue( "r1", r1.X, r1.Y );
			s.SetValue( "r2", r2.X, r2.Y );
		}

		public void EndFrame( IInputHandler inputHandler )
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

		static IGraphicsDevice device;

		public static Size Resolution { get { return device.WindowSize; } }

		internal static void Initialize( WindowMode windowMode )
		{
			var resolution = GetResolution( windowMode );
			var rendererPath = Path.GetFullPath( "OpenRA.Renderer.{0}.dll".F(Game.Settings.Graphics.Renderer) );
			device = CreateDevice( Assembly.LoadFile( rendererPath ), resolution.Width, resolution.Height, windowMode, false );
		}

		static Size GetResolution(WindowMode windowmode)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
			var customSize = (windowmode == WindowMode.Windowed) 
				? Game.Settings.Graphics.WindowedSize 
				: Game.Settings.Graphics.FullscreenSize;
			
			if (customSize.X > 0 && customSize.Y > 0)
			{
				desktopResolution.Width = customSize.X;
				desktopResolution.Height = customSize.Y;
			}
			return new Size(
				desktopResolution.Width,
				desktopResolution.Height);
		}

		static IGraphicsDevice CreateDevice( Assembly rendererDll, int width, int height, WindowMode window, bool vsync )
		{
			var argTypes = new Type[] { typeof( int ), typeof( int ), typeof( WindowMode ), typeof( bool ) };
			var argValues = new object[] { width, height, window, vsync };
			
			foreach( RendererAttribute r in rendererDll.GetCustomAttributes( typeof( RendererAttribute ), false ) )
				return (IGraphicsDevice)r.Type.GetConstructor( argTypes ).Invoke( argValues );
			
			throw new InvalidOperationException("Renderer DLL is missing RendererAttribute to tell us what type to use!");
		}

		internal IVertexBuffer<Vertex> GetTempVertexBuffer()
		{
			var ret = tempBuffers.Dequeue();
			tempBuffers.Enqueue( ret );
			return ret;
		}

		public interface IBatchRenderer	{ void Flush();	}

		static IBatchRenderer currentBatchRenderer;
		public static IBatchRenderer CurrentBatchRenderer
		{
			get { return currentBatchRenderer; }
			set
			{
				if( currentBatchRenderer == value ) return;
				if( currentBatchRenderer != null )
					currentBatchRenderer.Flush();
				currentBatchRenderer = value;
			}
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			Flush();
			Device.EnableScissor( left, top, width, height );
		}

		public void DisableScissor()
		{
			Flush();
			Device.DisableScissor();
		}
	}
}
