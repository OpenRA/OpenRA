#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Support;
using System.Windows.Forms;

namespace OpenRA.Graphics
{
	public class Renderer
	{
		internal static int SheetSize;

		public IShader SpriteShader { get; private set; }    /* note: shared shader params */
		public IShader LineShader { get; private set; }
		public IShader RgbaSpriteShader { get; private set; }
		public IShader WorldSpriteShader { get; private set; }

		public SpriteRenderer SpriteRenderer { get; private set; }
		public SpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public LineRenderer LineRenderer { get; private set; }

		public ITexture PaletteTexture;

		public readonly SpriteFont RegularFont, BoldFont, TitleFont;

		public Renderer()
		{
			SpriteShader = device.CreateShader(FileSystem.Open("shaders/world-shp.fx"));
			LineShader = device.CreateShader(FileSystem.Open("shaders/line.fx"));
			RgbaSpriteShader = device.CreateShader(FileSystem.Open("shaders/chrome-rgba.fx"));
			WorldSpriteShader = device.CreateShader(FileSystem.Open("shaders/chrome-shp.fx"));

			SpriteRenderer = new SpriteRenderer( this, SpriteShader );
			RgbaSpriteRenderer = new SpriteRenderer( this, RgbaSpriteShader );
			WorldSpriteRenderer = new SpriteRenderer( this, WorldSpriteShader );
			LineRenderer = new LineRenderer(this);

			RegularFont = new SpriteFont("FreeSans.ttf", 14);
			BoldFont = new SpriteFont("FreeSansBold.ttf", 14);
			TitleFont = new SpriteFont("titles.ttf", 48);
		}

		public IGraphicsDevice Device { get { return device; } }

		public void BeginFrame(float2 scroll)
		{
			device.Begin();
			device.Clear(Color.Black);

			float2 r1 = new float2(2f/Resolution.Width, -2f/Resolution.Height);
			float2 r2 = new float2(-1, 1);

			SetShaderParams( SpriteShader, r1, r2, scroll );
			SetShaderParams( LineShader, r1, r2, scroll );
			SetShaderParams( RgbaSpriteShader, r1, r2, scroll );
			SetShaderParams( WorldSpriteShader, r1, r2, scroll );
		}

		private void SetShaderParams( IShader s, float2 r1, float2 r2, float2 scroll )
		{
			s.SetValue( "Palette", PaletteTexture );
			s.SetValue( "Scroll", scroll.X, scroll.Y );
			s.SetValue( "r1", r1.X, r1.Y );
			s.SetValue( "r2", r2.X, r2.Y );
			s.Commit();
		}

		public void EndFrame()
		{
			device.End();
			device.Present();
		}

		public void DrawBatch<T>(IVertexBuffer<T> vertices, IIndexBuffer indices,
			Range<int> vertexRange, Range<int> indexRange, PrimitiveType type, IShader shader)
			where T : struct
		{
			vertices.Bind();
			indices.Bind();

			device.DrawIndexedPrimitives(type, vertexRange, indexRange);

			PerfHistory.Increment("batches", 1);
		}

		public void DrawBatch<T>(IVertexBuffer<T> vertices, IIndexBuffer indices,
			int vertexPool, int numPrimitives, PrimitiveType type)
			where T : struct
		{
			vertices.Bind();
			indices.Bind();

			device.DrawIndexedPrimitives(type, vertexPool, numPrimitives);

			PerfHistory.Increment("batches", 1);
		}
		
		public void Flush()
		{
			WorldSpriteRenderer.Flush();
			RgbaSpriteRenderer.Flush();
			LineRenderer.Flush();
		}





		static IGraphicsDevice device;

		public static Size Resolution { get { return device.WindowSize; } }

		internal static void Initialize( OpenRA.FileFormats.Graphics.WindowMode windowMode )
		{
			var resolution = GetResolution( windowMode );
			device = CreateDevice( Assembly.LoadFile( Path.GetFullPath( "OpenRA.Gl.dll" ) ), resolution.Width, resolution.Height, windowMode, false );
		}

		static Size GetResolution(WindowMode windowmode)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
			var customSize = (windowmode == WindowMode.Windowed) ? Game.Settings.Graphics.WindowedSize : Game.Settings.Graphics.FullscreenSize;
			
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
			foreach( RendererAttribute r in rendererDll.GetCustomAttributes( typeof( RendererAttribute ), false ) )
			{
				return (IGraphicsDevice)r.Type.GetConstructor( new Type[] { typeof( int ), typeof( int ), typeof( WindowMode ), typeof( bool ) } )
					.Invoke( new object[] { width, height, window, vsync } );
			}
			throw new NotImplementedException();
		}
	}
}
