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
        public Shader SpriteShader { get; private set; }    /* note: shared shader params */
        public Shader LineShader { get; private set; }

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
		}

		public GraphicsDevice Device { get { return device; } }

		public void BeginFrame( float2 r1, float2 r2, float2 scroll )
		{
			device.Begin();

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
	}
}
