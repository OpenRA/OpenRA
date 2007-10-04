using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using Ijw.DirectX;

namespace OpenRa.Game
{
	class SpriteRenderer
	{
		FvfVertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;
		Renderer renderer;

		const int spritesPerBatch = 1024;

		List<Vertex> vertices = new List<Vertex>();
		List<ushort> indices = new List<ushort>();
		Sheet currentSheet = null;
		int sprites = 0;
		ShaderQuality quality;

		public SpriteRenderer(Renderer renderer, bool allowAlpha)
		{
			this.renderer = renderer;

			vertexBuffer = new FvfVertexBuffer<Vertex>(renderer.Device, 4 * spritesPerBatch, Vertex.Format);
			indexBuffer = new IndexBuffer(renderer.Device, 6 * spritesPerBatch);

			quality = allowAlpha ? ShaderQuality.High : ShaderQuality.Low;
		}

		public void Flush()
		{
			if (sprites > 0)
			{
				renderer.DrawWithShader(quality, delegate
				{
					vertexBuffer.SetData(vertices.ToArray());
					indexBuffer.SetData(indices.ToArray());
					renderer.DrawBatch(vertexBuffer, indexBuffer,
						new Range<int>(0, vertices.Count),
						new Range<int>(0, indices.Count),
						currentSheet.Texture);
				});

				vertices = new List<Vertex>();
				indices = new List<ushort>();
				currentSheet = null;
				sprites = 0;
			}
		}

		public void DrawSprite(Sprite s, float2 location, int palette)
		{
			if (s.sheet != currentSheet)
				Flush();

			currentSheet = s.sheet;
			Util.CreateQuad(vertices, indices, location, s, palette);

			if (++sprites >= spritesPerBatch)
				Flush();
		}
	}
}
