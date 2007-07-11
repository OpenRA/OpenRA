using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using System.IO;

namespace OpenRa.Game
{
	class Renderer
	{
		readonly GraphicsDevice device;
		readonly Effect shader;

		readonly IntPtr r1Handle, r2Handle, baseTextureHandle, scrollHandle;

		const string shaderName = "diffuse.fx";

		public Renderer(Control host, Size resolution, bool windowed)
		{
			host.ClientSize = resolution;
			device = GraphicsDevice.Create(host, 
				resolution.Width, resolution.Height, windowed, false);

			shader = new Effect(device, File.OpenRead("../../../" + shaderName));
			shader.Quality = ShaderQuality.Low;

			baseTextureHandle = shader.GetHandle("DiffuseTexture");
			scrollHandle = shader.GetHandle("Scroll");
			r1Handle = shader.GetHandle("r1");
			r2Handle = shader.GetHandle("r2");
		}

		public GraphicsDevice Device { get { return device; } }

		public void BeginFrame( PointF r1, PointF r2, PointF scroll )
		{
			device.Begin();
			device.Clear(0, Surfaces.Color);

			shader.SetValue(scrollHandle, scroll);
			shader.SetValue(r1Handle, r1);
			shader.SetValue(r2Handle, r2);
		}

		public void EndFrame()
		{
			device.End();
			device.Present();
		}

		public void DrawWithShader(ShaderQuality quality, MethodInvoker task)
		{
			shader.Quality = quality;

			int passes = shader.Begin();
			for (int pass = 0; pass < passes; pass++)
			{
				shader.BeginPass(pass);
				task();
				shader.EndPass();
			}

			shader.End();
		}

		public void DrawBatch<T>(FvfVertexBuffer<T> vertices, IndexBuffer indices,
			Range<int> vertexRange, Range<int> indexRange, Texture texture)
			where T : struct
		{
			shader.SetTexture(baseTextureHandle, texture);
			shader.Commit();

			vertices.Bind(0);
			indices.Bind();

			device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				vertexRange, indexRange);
		}
	}
}
