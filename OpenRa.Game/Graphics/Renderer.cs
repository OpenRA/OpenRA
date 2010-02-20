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

		readonly Font fDebug, fTitle;
		readonly FTFontBitmap testFont;
		
		Sheet textSheet;
		SpriteRenderer rgbaRenderer;
		Sprite textSprite;

		public Size Resolution { get { return device.WindowSize; } }

		public Renderer(Size resolution, bool windowed)
		{
			device = CreateDevice( Assembly.LoadFile( Path.GetFullPath( "OpenRa.Gl.dll" ) ), resolution.Width, resolution.Height, windowed, false );

			SpriteShader = device.CreateShader(FileSystem.Open("world-shp.fx"));
			LineShader = device.CreateShader(FileSystem.Open("line.fx"));
			RgbaSpriteShader = device.CreateShader(FileSystem.Open("chrome-rgba.fx"));
			WorldSpriteShader = device.CreateShader(FileSystem.Open("chrome-shp.fx"));

			//fDebug = new Font("Tahoma", 10.0f, FontStyle.Regular);
			//fTitle = new Font("Tahoma", 10, FontStyle.Bold);
			int Errors;
			testFont = new FTFontBitmap("FreeSans.ttf", out Errors);
			testFont.ftRenderToTexture(2, 48);
			testFont.FT_ALIGN = FTFontAlign.FT_ALIGN_CENTERED;
			
			textSheet = new Sheet(this, new Size(256, 256));
			rgbaRenderer = new SpriteRenderer(this, true, RgbaSpriteShader);
			textSprite = new Sprite(textSheet, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
		}

		IGraphicsDevice CreateDevice( Assembly rendererDll, int width, int height, bool fullscreen, bool vsync )
		{
			foreach( RendererAttribute r in rendererDll.GetCustomAttributes( typeof( RendererAttribute ), false ) )
			{
				return (IGraphicsDevice)r.Type.GetConstructor( new Type[] { typeof( int ), typeof( int ), typeof( bool ), typeof( bool ) } )
					.Invoke( new object[] { width, height, fullscreen, vsync } );
			}
			throw new NotImplementedException();
		}

		Bitmap RenderTextToBitmap(string s, Font f, Color c)
		{
			Bitmap b = new Bitmap(256, 256);
			testFont.ftBeginFont(0.9f,0.0f,0.0f,0.5f);
			testFont.ftWrite("Test",b);
			testFont.ftEndFont();
			
            /*using (var g = System.Drawing.Graphics.FromImage(b))
            {
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                g.DrawString(s, f, new SolidBrush(c), 0, 0);
                g.Flush();
            }*/
			return b;
		}

		int2 GetTextSize(string s, Font f)
		{
			return new int2(50,100);
			/*Bitmap b = new Bitmap(1,1);
			System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b);
			return new int2(g.MeasureString(s, f).ToSize());*/
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

		public void DrawText(string text, int2 pos, Color c)
		{
			using (new PerfSample("text"))
			{											
				Bitmap b = RenderTextToBitmap(text, fDebug, c);
				textSheet.Texture.SetData(b);
				rgbaRenderer.DrawSprite(textSprite, pos.ToFloat2(), "chrome");
				rgbaRenderer.Flush();
			}
		}

		public void DrawText2(string text, int2 pos, Color c)
		{
			using (new PerfSample("text"))
			{
				Bitmap b = RenderTextToBitmap(text, fTitle, c);
				textSheet.Texture.SetData(b);
				rgbaRenderer.DrawSprite(textSprite, pos.ToFloat2(), "chrome");
				rgbaRenderer.Flush();
			}
		}

		public int2 MeasureText(string text)
		{
			return GetTextSize(text, fDebug);
		}

		public int2 MeasureText2(string text)
		{
			return GetTextSize(text, fTitle);
		}
	}
}
