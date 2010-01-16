using System.Drawing;
using System.Windows.Forms;
using Ijw.DirectX;
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

		readonly SpriteHelper sh;
		readonly FontHelper fhDebug, fhTitle;

		public Renderer(Control host, Size resolution, bool windowed)
		{
			host.ClientSize = resolution;
			device = GraphicsDevice.Create(host,
				resolution.Width, resolution.Height, windowed, false);

			SpriteShader = new Shader(device, FileSystem.Open("world-shp.fx"));
			SpriteShader.Quality = ShaderQuality.Low;
			LineShader = new Shader(device, FileSystem.Open("line.fx"));
			LineShader.Quality = ShaderQuality.High;
			RgbaSpriteShader = new Shader(device, FileSystem.Open("chrome-rgba.fx"));
			RgbaSpriteShader.Quality = ShaderQuality.High;
			WorldSpriteShader = new Shader(device, FileSystem.Open("chrome-shp.fx"));
			WorldSpriteShader.Quality = ShaderQuality.High;

			sh = new SpriteHelper(device);
			fhDebug = new FontHelper(device, "Tahoma", 10, false);
			fhTitle = new FontHelper(device, "Tahoma", 10, true);
		}

		public GraphicsDevice Device { get { return device; } }

		public void BeginFrame(float2 r1, float2 r2, float2 scroll)
		{
			device.Begin();
			device.Clear(0, Surfaces.Color);

			SpriteShader.SetValue("Palette", PaletteTexture);
			SpriteShader.SetValue("Scroll", scroll);
			SpriteShader.SetValue("r1", r1);
			SpriteShader.SetValue("r2", r2);
			SpriteShader.Commit();
		}

		public void EndFrame()
		{
			device.End();
			device.Present();
		}

		public void DrawBatch<T>(FvfVertexBuffer<T> vertices, IndexBuffer indices,
			Range<int> vertexRange, Range<int> indexRange, Texture texture, PrimitiveType type, Shader shader)
			where T : struct
		{
			shader.SetValue("DiffuseTexture", texture);
			shader.Commit();

			vertices.Bind(0);
			indices.Bind();

			device.DrawIndexedPrimitives(type,
				vertexRange, indexRange);

			PerfHistory.Increment("batches", 1);
		}

		public void DrawBatch<T>(FvfVertexBuffer<T> vertices, IndexBuffer indices,
			int vertexPool, int numPrimitives, Texture texture, PrimitiveType type)
			where T : struct
		{
			SpriteShader.SetValue("DiffuseTexture", texture);
			SpriteShader.Commit();

			vertices.Bind(0);
			indices.Bind();

			device.DrawIndexedPrimitives(type,
				vertexPool, numPrimitives);

			PerfHistory.Increment("batches", 1);
		}

		public void DrawText(string text, int2 pos, Color c)
		{
			sh.Begin();
			fhDebug.Draw(sh, text, pos.X, pos.Y, c.ToArgb());
			sh.End();
		}

		public void DrawText2(string text, int2 pos, Color c)
		{
			sh.Begin();
			fhTitle.Draw(sh, text, pos.X, pos.Y, c.ToArgb());
			sh.End();
		}

		public int2 MeasureText(string text)
		{
			return new int2(fhDebug.MeasureText(sh, text));
		}

		public int2 MeasureText2(string text)
		{
			return new int2(fhTitle.MeasureText(sh, text));
		}
	}
}
