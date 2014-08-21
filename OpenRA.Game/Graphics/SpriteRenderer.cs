#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		Renderer renderer;
		IShader shader;

		Vertex[] vertices = new Vertex[Renderer.TempBufferSize];
		Sheet currentSheet = null;
		BlendMode currentBlend = BlendMode.Alpha;
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

				renderer.Device.SetBlendMode(currentBlend);
				shader.Render(() =>
				{
					var vb = renderer.GetTempVertexBuffer();
					vb.SetData(vertices, nv);
					renderer.DrawBatch(vb, 0, nv, PrimitiveType.QuadList);
				});
				renderer.Device.SetBlendMode(BlendMode.None);

				nv = 0;
				currentSheet = null;
			}
		}

		public void DrawSprite(Sprite s, float2 location, PaletteReference pal)
		{
			DrawSprite(s, location, pal.Index, s.size);
		}

		public void DrawSprite(Sprite s, float2 location, PaletteReference pal, float2 size)
		{
			DrawSprite(s, location, pal.Index, size);
		}

		void DrawSprite(Sprite s, float2 location, int paletteIndex, float2 size)
		{
			Renderer.CurrentBatchRenderer = this;

			if (s.sheet != currentSheet)
				Flush();

			if (s.blendMode != currentBlend)
				Flush();

			if (nv + 4 > Renderer.TempBufferSize)
				Flush();

			currentBlend = s.blendMode;
			currentSheet = s.sheet;
			Util.FastCreateQuad(vertices, location + s.offset, s, paletteIndex, nv, size);
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

		public void DrawSprite(Sprite s, float2 a, float2 b, float2 c, float2 d)
		{
			Renderer.CurrentBatchRenderer = this;

			if (s.sheet != currentSheet)
				Flush();

			if (s.blendMode != currentBlend)
				Flush();

			if (nv + 4 > Renderer.TempBufferSize)
				Flush();

			currentSheet = s.sheet;
			currentBlend = s.blendMode;
			Util.FastCreateQuad(vertices, a, b, c, d, s, 0, nv);
			nv += 4;
		}

		public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, Sheet sheet)
		{
			shader.SetTexture("DiffuseTexture", sheet.Texture);
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
			shader.SetVec("r1", zoom*2f/screen.Width, -zoom*2f/screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
