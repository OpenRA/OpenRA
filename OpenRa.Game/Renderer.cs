using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Ijw.DirectX;
using System.IO;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	class Renderer
	{
		readonly GraphicsDevice device;
		readonly Shader shader;

		const string shaderName = "diffuse.fx";

		public void SetPalette(HardwarePalette hp)
		{
			shader.SetValue("Palette", hp.Texture);
		}

		public Renderer(Control host, Size resolution, bool windowed)
		{
			host.ClientSize = resolution;
			device = GraphicsDevice.Create(host, 
				resolution.Width, resolution.Height, windowed, false);

			shader = new Shader(device, FileSystem.Open(shaderName));
			shader.Quality = ShaderQuality.Low;
		}

		public GraphicsDevice Device { get { return device; } }

		public void BeginFrame( float2 r1, float2 r2, float2 scroll )
		{
			device.Begin();

			shader.SetValue("Scroll", scroll);
			shader.SetValue("r1", r1);
			shader.SetValue("r2", r2);
		}

		public void EndFrame()
		{
			device.End();
			device.Present();
		}

		public void DrawWithShader(ShaderQuality quality, Action task)
		{
			shader.Quality = quality;
			shader.Render(() => task());
		}

		public void DrawBatch<T>(FvfVertexBuffer<T> vertices, IndexBuffer indices,
			Range<int> vertexRange, Range<int> indexRange, Texture texture)
			where T : struct
		{
			shader.SetValue("DiffuseTexture", texture);
			shader.Commit();

			vertices.Bind(0);
			indices.Bind();

			device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				vertexRange, indexRange);
		}
	}
}
