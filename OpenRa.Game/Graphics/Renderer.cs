using System.Drawing;
using System.Windows.Forms;
using OpenRa.GlRenderer;
using OpenRa.FileFormats;
using OpenRa.Support;

namespace OpenRa.Graphics
{
	internal class Renderer
	{
		internal static int SheetSize;

		readonly GraphicsDevice device;

		public Shader SpriteShader { get; private set; }    /* note: shared shader params */
		public Shader LineShader { get; private set; }
		public Shader RgbaSpriteShader { get; private set; }
		public Shader WorldSpriteShader { get; private set; }

		public Texture PaletteTexture;

		//readonly SpriteHelper sh;
		//readonly FontHelper fhDebug, fhTitle;

		public Renderer(Control control, Size resolution, bool windowed)
		{
            control.ClientSize = resolution;
			device = new GraphicsDevice(control, resolution.Width, resolution.Height, windowed, false);

			SpriteShader = new Shader(device, FileSystem.Open("world-shp.fx"));
			SpriteShader.Quality = ShaderQuality.Low;
			LineShader = new Shader(device, FileSystem.Open("line.fx"));
			LineShader.Quality = ShaderQuality.High;
			RgbaSpriteShader = new Shader(device, FileSystem.Open("chrome-rgba.fx"));
			RgbaSpriteShader.Quality = ShaderQuality.High;
			WorldSpriteShader = new Shader(device, FileSystem.Open("chrome-shp.fx"));
			WorldSpriteShader.Quality = ShaderQuality.High;

            //sh = new SpriteHelper(device);
            //fhDebug = new FontHelper(device, "Tahoma", 10, false);
            //fhTitle = new FontHelper(device, "Tahoma", 10, true);
		}

		public GraphicsDevice Device { get { return device; } }

		public void BeginFrame(float2 r1, float2 r2, float2 scroll)
		{
			device.Begin();
			device.Clear(Color.Black);

			SetShaderParams( SpriteShader, r1, r2, scroll );
			SetShaderParams( LineShader, r1, r2, scroll );
			SetShaderParams( RgbaSpriteShader, r1, r2, scroll );
			SetShaderParams( WorldSpriteShader, r1, r2, scroll );
		}

		private void SetShaderParams( Shader s, float2 r1, float2 r2, float2 scroll )
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

		public void DrawBatch<T>(VertexBuffer<T> vertices, IndexBuffer indices,
			Range<int> vertexRange, Range<int> indexRange, Texture texture, PrimitiveType type, Shader shader)
			where T : struct
		{
			shader.SetValue("DiffuseTexture", texture);
			shader.Commit();

			vertices.Bind();
			indices.Bind();

			device.DrawIndexedPrimitives(type, vertexRange, indexRange);

			PerfHistory.Increment("batches", 1);
		}

		public void DrawBatch<T>(VertexBuffer<T> vertices, IndexBuffer indices,
			int vertexPool, int numPrimitives, Texture texture, PrimitiveType type)
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
            //sh.Begin();
            //fhDebug.Draw(sh, text, pos.X, pos.Y, c.ToArgb());
            //sh.End();
		}

		public void DrawText2(string text, int2 pos, Color c)
		{
            //sh.Begin();
            //fhTitle.Draw(sh, text, pos.X, pos.Y, c.ToArgb());
            //sh.End();
		}

		public int2 MeasureText(string text)
		{
			return new int2(20,20);//fhDebug.MeasureText(sh, text));
		}

		public int2 MeasureText2(string text)
		{
			return new int2(20,20);//fhTitle.MeasureText(sh, text));
		}
	}
}
