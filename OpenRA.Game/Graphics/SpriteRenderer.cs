#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		Renderer renderer;
		IShader shader;

		Vertex[] vertices = new Vertex[Renderer.TempBufferSize];
		ushort[] indices = new ushort[Renderer.TempBufferSize];
		Sheet currentSheet = null;
		int nv = 0, ni = 0;

		public SpriteRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
		}

		public SpriteRenderer(Renderer renderer)
			: this(renderer, renderer.SpriteShader) { }

		public void Flush()
		{
			if (ni > 0)
			{
				shader.SetValue( "DiffuseTexture", currentSheet.Texture );
				shader.Render(() =>
				{
					var vb = renderer.GetTempVertexBuffer();
					var ib = renderer.GetTempIndexBuffer();
					vb.SetData(vertices, nv);
					ib.SetData(indices, ni);
					renderer.DrawBatch(vb, ib,
						new Range<int>(0, nv),
						new Range<int>(0, ni),
						PrimitiveType.TriangleList,
						shader);
				});

				nv = 0; ni = 0;
				currentSheet = null;
			}
		}
				
		public void DrawSprite(Sprite s, float2 location, WorldRenderer wr, string palette)
		{
			DrawSprite(s, location, wr.GetPaletteIndex(palette), s.size);
		}
		
		public void DrawSprite(Sprite s, float2 location, WorldRenderer wr, string palette, float2 size)
		{
			DrawSprite(s, location, wr.GetPaletteIndex(palette), size);
		}
		
		public void DrawSprite(Sprite s, float2 location, int paletteIndex, float2 size)
		{
			Renderer.CurrentBatchRenderer = this;

			if (s.sheet != currentSheet)
				Flush();

			if( nv + 4 > Renderer.TempBufferSize )
				Flush();
			if( ni + 6 > Renderer.TempBufferSize )
				Flush();

			currentSheet = s.sheet;
			Util.FastCreateQuad(vertices, indices, location.ToInt2(), s, paletteIndex, nv, ni, size);
			nv += 4; ni += 6;
		}
		
		
		// For RGBASpriteRenderer, which doesn't use palettes
		public void DrawSprite(Sprite s, float2 location)
		{
			DrawSprite(s, location, 0, s.size);
		}
		
		public void DrawSprite(Sprite s, float2 location, float2 size)
		{
			DrawSprite(s, location, 0, size);
		}
	}
}
