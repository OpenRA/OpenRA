using System;
using System.Drawing;
using System.Windows.Forms;
using Ijw.DirectX;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class Renderer
	{
		readonly GraphicsDevice device;
		Texture[] palettes;
        public Shader SpriteShader { get; private set; }    /* note: shared shader params */
        public Shader LineShader { get; private set; }

		readonly SpriteHelper sh;
		readonly FontHelper fhDebug;

		public void BuildPalette(Map map)
		{
			palettes = Util.MakeArray(7, i => new HardwarePalette(this, map, i).Texture);
		}

		public void SetPalette(HardwarePalette hp)
		{
			SpriteShader.SetValue("Palette", hp.Texture);
		}

		public Renderer(Control host, Size resolution, bool windowed)
		{
			host.ClientSize = resolution;
			device = GraphicsDevice.Create(host, 
				resolution.Width, resolution.Height, windowed, false);

			SpriteShader = new Shader(device, FileSystem.Open("sprite.fx"));
			SpriteShader.Quality = ShaderQuality.Low;
            LineShader = new Shader(device, FileSystem.Open("line.fx"));
            LineShader.Quality = ShaderQuality.High;

			sh = new SpriteHelper(device );
			fhDebug = new FontHelper(device, "Tahoma", 10, false );
		}

		public GraphicsDevice Device { get { return device; } }

		public static float waterFrame = 0.0f;

		public void BeginFrame( float2 r1, float2 r2, float2 scroll )
		{
			device.Begin();

			SpriteShader.SetValue("Palette", palettes[ (int)(waterFrame * palettes.Length) % palettes.Length ]);
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
			Range<int> vertexRange, Range<int> indexRange, Texture texture, PrimitiveType type)
			where T : struct
		{
			SpriteShader.SetValue("DiffuseTexture", texture);
			SpriteShader.Commit();

			vertices.Bind(0);
			indices.Bind();

			device.DrawIndexedPrimitives(type,
				vertexRange, indexRange);
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
        }

		public void DrawText(string text, int2 pos, Color c)
		{
			sh.Begin();
			fhDebug.Draw(sh, text, pos.X, pos.Y, c.ToArgb());
			sh.End();
		}

		public int2 MeasureText(string text)
		{
			return new int2(fhDebug.MeasureText(sh, text));
		}
	}
}
