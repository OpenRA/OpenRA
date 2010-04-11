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

using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	class SpriteRenderer
	{
		IVertexBuffer<Vertex> vertexBuffer;
		IIndexBuffer indexBuffer;
		Renderer renderer;
		IShader shader;

		const int spritesPerBatch = 1024;

		Vertex[] vertices = new Vertex[4 * spritesPerBatch];
		ushort[] indices = new ushort[6 * spritesPerBatch];
		Sheet currentSheet = null;
		int sprites = 0;
		int nv = 0, ni = 0;

		public SpriteRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;

			vertexBuffer = renderer.Device.CreateVertexBuffer( vertices.Length );
			indexBuffer = renderer.Device.CreateIndexBuffer( indices.Length );
		}

		public SpriteRenderer(Renderer renderer)
			: this(renderer, renderer.SpriteShader) { }

		public void Flush()
		{
			if (sprites > 0)
			{
				shader.SetValue( "DiffuseTexture", currentSheet.Texture );
				shader.Render(() =>
				{
					vertexBuffer.SetData(vertices);
					indexBuffer.SetData(indices);
					renderer.DrawBatch(vertexBuffer, indexBuffer,
						new Range<int>(0, nv),
						new Range<int>(0, ni),
						PrimitiveType.TriangleList,
						shader);
				});

				nv = 0; ni = 0;
				currentSheet = null;
				sprites = 0;
			}
		}

		public void DrawSprite(Sprite s, float2 location, string palette)
		{
			DrawSprite(s, location, palette, s.size);
		}

		public void DrawSprite(Sprite s, float2 location, string palette, float2 size)
		{
			if (s.sheet != currentSheet)
				Flush();

			currentSheet = s.sheet;
			Util.FastCreateQuad(vertices, indices, location.ToInt2(), s, Game.world.WorldRenderer.GetPaletteIndex(palette), nv, ni, size);
			nv += 4; ni += 6;
			if (++sprites >= spritesPerBatch)
				Flush();
		}
	}
}
