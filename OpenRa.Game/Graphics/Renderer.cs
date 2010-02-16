using System.Drawing;
using System.Windows.Forms;
using Ijw.DirectX;
using OpenRa.FileFormats;
using OpenRa.Support;
using System.Drawing.Drawing2D;

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
		readonly Font fDebug, fTitle;

		Sheet textSheet;
		SpriteRenderer rgbaRenderer;
		Sprite textSprite;

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

			//sh = new SpriteHelper(device);
			fDebug = new Font("Tahoma", 10, FontStyle.Regular);
			fTitle = new Font("Tahoma", 10, FontStyle.Bold);
			textSheet = new Sheet(this, new Size(256, 256));
			rgbaRenderer = new SpriteRenderer(this, true, RgbaSpriteShader);
			textSprite = new Sprite(textSheet, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
		}

		Bitmap RenderTextToBitmap(string s, Font f, Color c)
		{
			Bitmap b = new Bitmap(256, 256);
			System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b);
			g.DrawString(s, f, new SolidBrush(c), 0, 0);
			g.Flush();
			g.Dispose();
			return b;
		}

		int2 GetTextSize(string s, Font f)
		{
			Bitmap b = new Bitmap(1,1);
			System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b);
			return new int2(g.MeasureString(s, f).ToSize());
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
			Bitmap b = RenderTextToBitmap(text, fDebug, c);
			textSheet.Texture.SetData(b);
			rgbaRenderer.DrawSprite(textSprite, pos.ToFloat2(), "chrome");
		}

		public void DrawText2(string text, int2 pos, Color c)
		{
			Bitmap b = RenderTextToBitmap(text, fTitle, c);
			textSheet.Texture.SetData(b);
			rgbaRenderer.DrawSprite(textSprite, pos.ToFloat2(), "chrome");
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
