#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Support;
using System.IO;

namespace OpenRA.Graphics
{
	internal class Renderer
	{
		internal static int SheetSize;

		readonly IGraphicsDevice device;

		public IShader SpriteShader { get; private set; }    /* note: shared shader params */
		public IShader LineShader { get; private set; }
		public IShader RgbaSpriteShader { get; private set; }
		public IShader WorldSpriteShader { get; private set; }

		public SpriteRenderer SpriteRenderer { get; private set; }
		public SpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer WorldSpriteRenderer { get; private set; }

		public ITexture PaletteTexture;

		public readonly SpriteFont RegularFont, BoldFont, TitleFont;

		public Size Resolution { get { return device.WindowSize; } }

		public Renderer(Size resolution, bool windowed)
		{
			device = CreateDevice( Assembly.LoadFile( Path.GetFullPath( "OpenRA.Gl.dll" ) ), resolution.Width, resolution.Height, windowed, false );

			SpriteShader = device.CreateShader(FileSystem.Open("shaders/world-shp.fx"));
			LineShader = device.CreateShader(FileSystem.Open("shaders/line.fx"));
			RgbaSpriteShader = device.CreateShader(FileSystem.Open("shaders/chrome-rgba.fx"));
			WorldSpriteShader = device.CreateShader(FileSystem.Open("shaders/chrome-shp.fx"));

			SpriteRenderer = new SpriteRenderer( this, SpriteShader );
			RgbaSpriteRenderer = new SpriteRenderer( this, RgbaSpriteShader );
			WorldSpriteRenderer = new SpriteRenderer( this, WorldSpriteShader );

			RegularFont = new SpriteFont(this, "FreeSans.ttf", 14);
			BoldFont = new SpriteFont(this, "FreeSansBold.ttf", 14);
			TitleFont = new SpriteFont(this, "titles.ttf", 48);
		}

		IGraphicsDevice CreateDevice( Assembly rendererDll, int width, int height, bool windowed, bool vsync )
		{
			foreach( RendererAttribute r in rendererDll.GetCustomAttributes( typeof( RendererAttribute ), false ) )
			{
				return (IGraphicsDevice)r.Type.GetConstructor( new Type[] { typeof( int ), typeof( int ), typeof( bool ), typeof( bool ) } )
					.Invoke( new object[] { width, height, windowed, vsync } );
			}
			throw new NotImplementedException();
		}

		public IGraphicsDevice Device { get { return device; } }

		public void BeginFrame(float2 r1, float2 r2, float2 scroll)
		{
			device.Begin();
			device.Clear(Color.Black);

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
	}
}
