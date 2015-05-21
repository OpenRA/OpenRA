#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		readonly Renderer renderer;
		readonly IShader shader;

		readonly Vertex[] vertices;
		Sheet currentSheet;
		BlendMode currentBlend = BlendMode.Alpha;
		float currentAlpha = 1f;
		int nv = 0;

		public SpriteRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
			vertices = new Vertex[renderer.TempBufferSize];
		}

		public void Flush()
		{
			if (nv > 0)
			{
				shader.SetTexture("DiffuseTexture", currentSheet.GetTexture());

				renderer.Device.SetBlendMode(currentBlend, currentAlpha);
				shader.Render(() =>
				{
					var vb = renderer.GetTempVertexBuffer();
					vb.SetData(vertices, nv);
					renderer.DrawBatch(vb, 0, nv, PrimitiveType.QuadList);
				});
				renderer.Device.SetBlendMode(BlendMode.None, currentAlpha);

				nv = 0;
				currentSheet = null;
			}
		}

		void SetRenderStateForSprite(Sprite s)
		{
			renderer.CurrentBatchRenderer = this;

			if (s.Alpha != currentAlpha || s.BlendMode != currentBlend || s.Sheet != currentSheet || nv + 4 > renderer.TempBufferSize)
				Flush();

			currentAlpha = s.Alpha;
			currentBlend = s.BlendMode;
			currentSheet = s.Sheet;
		}

		public void DrawSprite(Sprite s, float2 location, PaletteReference pal)
		{
			DrawSprite(s, location, pal.TextureIndex, s.Size);
		}

		public void DrawSprite(Sprite s, float2 location, PaletteReference pal, float2 size)
		{
			DrawSprite(s, location, pal.TextureIndex, size);
		}

		void DrawSprite(Sprite s, float2 location, float paletteTextureIndex, float2 size)
		{
			SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + s.FractionalOffset * size, s, paletteTextureIndex, nv, size);
			nv += 4;
		}

		// For RGBASpriteRenderer, which doesn't use palettes
		public void DrawSprite(Sprite s, float2 location)
		{
			DrawSprite(s, location, 0, s.Size);
		}

		public void DrawSprite(Sprite s, float2 location, float2 size)
		{
			DrawSprite(s, location, 0, size);
		}

		public void DrawSprite(Sprite s, float2 a, float2 b, float2 c, float2 d)
		{
			SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, a, b, c, d, s, 0, nv);
			nv += 4;
		}

		public void DrawSprite(Sprite s, Vertex[] sourceVertices, int offset)
		{
			SetRenderStateForSprite(s);
			Array.Copy(sourceVertices, offset, vertices, nv, 4);
			nv += 4;
		}

		public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, Sheet sheet)
		{
			shader.SetTexture("DiffuseTexture", sheet.GetTexture());
			renderer.Device.SetBlendMode(BlendMode.Alpha);
			shader.Render(() => renderer.DrawBatch(buffer, start, length, type));
			renderer.Device.SetBlendMode(BlendMode.None);
		}

		public void SetPalette(ITexture palette)
		{
			shader.SetTexture("Palette", palette);
		}

		public void SetViewportParams(Size screen, float zoom, int2 scroll)
		{
			shader.SetVec("Scroll", scroll.X, scroll.Y);
			shader.SetVec("r1", zoom * 2f / screen.Width, -zoom * 2f / screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
