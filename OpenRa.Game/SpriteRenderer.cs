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

        Vertex[] vertices = new Vertex[4 * spritesPerBatch];
        ushort[] indices = new ushort[6 * spritesPerBatch];
		Sheet currentSheet = null;
		int sprites = 0;
		ShaderQuality quality;
        int nv = 0, ni = 0;

		public SpriteRenderer(Renderer renderer, bool allowAlpha)
		{
			this.renderer = renderer;

			vertexBuffer = new FvfVertexBuffer<Vertex>(renderer.Device, vertices.Length, Vertex.Format);
			indexBuffer = new IndexBuffer(renderer.Device, indices.Length);

			quality = allowAlpha ? ShaderQuality.High : ShaderQuality.Low;
		}

		public void Flush()
		{
			if (sprites > 0)
			{
				renderer.DrawWithShader(quality, () =>
				{
					vertexBuffer.SetData(vertices);
					indexBuffer.SetData(indices);
					renderer.DrawBatch(vertexBuffer, indexBuffer,
						new Range<int>(0, nv),
						new Range<int>(0, ni),
						currentSheet.Texture);
				});

                nv = 0; ni = 0;
				currentSheet = null;
				sprites = 0;
			}
		}

		public void DrawSprite(Sprite s, float2 location, int palette)
		{
			if (s.sheet != currentSheet)
				Flush();

			currentSheet = s.sheet;
			Util.FastCreateQuad(vertices, indices, location, s, palette, nv, ni);
            nv += 4; ni += 6;
			if (++sprites >= spritesPerBatch)
				Flush();
		}
	}
}
