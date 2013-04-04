#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		Renderer renderer;
		IShader shader;

		Vertex[] vertices = new Vertex[Renderer.TempBufferSize];
		Sheet currentSheet = null;
		int nv = 0;

		public SpriteRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
		}

		public void Flush()
		{
			if (nv > 0)
			{
				shader.SetTexture("DiffuseTexture", currentSheet.Texture);
				shader.Render(() =>
				{
					var vb = renderer.GetTempVertexBuffer();
					vb.SetData(vertices, nv);
					renderer.DrawBatch(vb, 0, nv, PrimitiveType.QuadList);
				});

				nv = 0;
				currentSheet = null;
			}
		}

		public void DrawSprite(Sprite s, float2 location, WorldRenderer wr, string palette)
		{
			DrawSprite(s, location, wr.Palette(palette).Index, s.size);
		}

		public void DrawSprite(Sprite s, float2 location, WorldRenderer wr, string palette, float2 size)
		{
			DrawSprite(s, location, wr.Palette(palette).Index, size);
		}

		public void DrawSprite(Sprite s, float2 location, int paletteIndex, float2 size)
		{
			Renderer.CurrentBatchRenderer = this;

			if (s.sheet != currentSheet)
				Flush();

			if( nv + 4 > Renderer.TempBufferSize )
				Flush();

			currentSheet = s.sheet;
			Util.FastCreateQuad(vertices, location.ToInt2(), s, paletteIndex, nv, size);
			nv += 4;
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

		public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, Sheet sheet)
		{
			shader.SetTexture("DiffuseTexture", sheet.Texture);
			shader.Render(() => renderer.DrawBatch(buffer, start, length, type));
		}

		public void SetPalette(ITexture palette)
		{
			shader.SetTexture("Palette", palette);
		}

		public void SetViewportParams(Size screen, float zoom, float2 scroll)
		{
			shader.SetVec("Scroll", (int)scroll.X, (int)scroll.Y);
			shader.SetVec("r1", zoom*2f/screen.Width, -zoom*2f/screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
