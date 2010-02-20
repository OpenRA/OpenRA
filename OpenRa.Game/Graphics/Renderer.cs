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
using OpenRa.FileFormats;
using OpenRa.FileFormats.Graphics;
using OpenRa.Support;
using System.IO;
using ISE;
using Tao.OpenGl;

namespace OpenRa.Graphics
{
	internal class Renderer
	{
		internal static int SheetSize;

		readonly IGraphicsDevice device;

		public IShader SpriteShader { get; private set; }    /* note: shared shader params */
		public IShader LineShader { get; private set; }
		public IShader RgbaSpriteShader { get; private set; }
		public IShader WorldSpriteShader { get; private set; }

		public ITexture PaletteTexture;

		readonly FTFontGL regularFont, boldFont;
		const int RenderedFontSize = 48;

		public Size Resolution { get { return device.WindowSize; } }

		public Renderer(Size resolution, bool windowed)
		{
			device = CreateDevice( Assembly.LoadFile( Path.GetFullPath( "OpenRa.Gl.dll" ) ), resolution.Width, resolution.Height, windowed, false );

			SpriteShader = device.CreateShader(FileSystem.Open("shaders/world-shp.fx"));
			LineShader = device.CreateShader(FileSystem.Open("shaders/line.fx"));
			RgbaSpriteShader = device.CreateShader(FileSystem.Open("shaders/chrome-rgba.fx"));
			WorldSpriteShader = device.CreateShader(FileSystem.Open("shaders/chrome-shp.fx"));

			int Errors;
			regularFont = new FTFontGL("FreeSans.ttf", out Errors);

			if (Errors > 0)
				throw new InvalidOperationException("Error(s) loading font");

			regularFont.ftRenderToTexture(RenderedFontSize, 192);
			regularFont.FT_ALIGN = FTFontAlign.FT_ALIGN_LEFT;

			boldFont = new FTFontGL("FreeSansBold.ttf", out Errors);
			if (Errors > 0)
				throw new InvalidOperationException("Error(s) loading font");

			boldFont.ftRenderToTexture(RenderedFontSize, 192);
			boldFont.FT_ALIGN = FTFontAlign.FT_ALIGN_LEFT;
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
			Range<int> vertexRange, Range<int> indexRange, ITexture texture, PrimitiveType type, IShader shader)
			where T : struct
		{
			shader.SetValue("DiffuseTexture", texture);
			shader.Commit();

			vertices.Bind();
			indices.Bind();

			device.DrawIndexedPrimitives(type, vertexRange, indexRange);

			PerfHistory.Increment("batches", 1);
		}

		public void DrawBatch<T>(IVertexBuffer<T> vertices, IIndexBuffer indices,
			int vertexPool, int numPrimitives, ITexture texture, PrimitiveType type)
			where T : struct
		{
			SpriteShader.SetValue("DiffuseTexture", texture);
			SpriteShader.Commit();

			vertices.Bind();
			indices.Bind();

			device.DrawIndexedPrimitives(type, vertexPool, numPrimitives);

			PerfHistory.Increment("batches", 1);
		}

		static void CheckError()
		{
			var e = Gl.glGetError();
			if (e != Gl.GL_NO_ERROR)
				throw new InvalidOperationException("GL Error: " + Gl.glGetString(e));
		}

		const float emHeight = 14f;	/* px */

		void DrawTextInner(FTFontGL f, string text, int2 pos, Color c)
		{
			using (new PerfSample("text"))
			{
				pos.Y += (int)(emHeight);

				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glPushMatrix();
				Gl.glLoadIdentity();

				Gl.glMatrixMode(Gl.GL_PROJECTION);
				Gl.glPushMatrix();
				Gl.glLoadIdentity();

				Gl.glOrtho(0, Resolution.Width, 0, Resolution.Height, 0, 1);

				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glTranslatef(pos.X, Resolution.Height - pos.Y, 0);
				Gl.glScalef(emHeight / RenderedFontSize, emHeight / RenderedFontSize, 1);

				f.ftBeginFont(false);
				Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
				f.ftWrite(text);
				f.ftEndFont();

				Gl.glMatrixMode(Gl.GL_PROJECTION);
				Gl.glPopMatrix();

				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glPopMatrix();

				CheckError();
			}
		}

		public void DrawText(string text, int2 pos, Color c) { DrawTextInner(regularFont, text, pos, c); }
		public void DrawText2(string text, int2 pos, Color c) { DrawTextInner(boldFont, text, pos, c); }

		public int2 MeasureText(string text)
		{
			return new int2((int)(regularFont.ftExtent(ref text) / 3), (int)emHeight);
		}

		public int2 MeasureText2(string text)
		{
			return new int2((int)(boldFont.ftExtent(ref text) / 3), (int)emHeight);
		}
	}
}
