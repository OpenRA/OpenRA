#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
		readonly Action renderAction;

		readonly Vertex[] vertices;
		Sheet currentSheet;
		BlendMode currentBlend = BlendMode.Alpha;
		int nv = 0;

		public SpriteRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
			vertices = new Vertex[renderer.TempBufferSize];
			renderAction = () => renderer.DrawBatch(vertices, nv, PrimitiveType.TriangleList);
		}

		public void Flush()
		{
			if (nv > 0)
			{
				shader.SetTexture("DiffuseTexture", currentSheet.GetTexture());

				renderer.Device.SetBlendMode(currentBlend);
				shader.Render(renderAction);
				renderer.Device.SetBlendMode(BlendMode.None);

				nv = 0;
				currentSheet = null;
			}
		}

		void SetRenderStateForSprite(Sprite s)
		{
			renderer.CurrentBatchRenderer = this;

			if (s.BlendMode != currentBlend || s.Sheet != currentSheet || nv + 6 > renderer.TempBufferSize)
				Flush();

			currentBlend = s.BlendMode;
			currentSheet = s.Sheet;
		}

		public void DrawSprite(Sprite s, float3 location, PaletteReference pal)
		{
			DrawSprite(s, location, pal.TextureIndex, s.Size);
		}

		public void DrawSprite(Sprite s, float3 location, PaletteReference pal, float3 size)
		{
			DrawSprite(s, location, pal.TextureIndex, size);
		}

		void DrawSprite(Sprite s, float3 location, float paletteTextureIndex, float3 size)
		{
			SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + s.FractionalOffset * size, s, paletteTextureIndex, nv, size);
			nv += 6;
		}

		// For RGBASpriteRenderer, which doesn't use palettes
		public void DrawSprite(Sprite s, float3 location)
		{
			DrawSprite(s, location, 0, s.Size);
		}

		public void DrawSprite(Sprite s, float3 location, float3 size)
		{
			DrawSprite(s, location, 0, size);
		}

		public void DrawSprite(Sprite s, float3 a, float3 b, float3 c, float3 d)
		{
			SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, a, b, c, d, s, 0, nv);
			nv += 6;
		}

		public void DrawSprite(Sprite s, Vertex[] sourceVertices, int offset)
		{
			SetRenderStateForSprite(s);
			Array.Copy(sourceVertices, offset, vertices, nv, 6);
			nv += 6;
		}

		public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, Sheet sheet, BlendMode blendMode)
		{
			shader.SetTexture("DiffuseTexture", sheet.GetTexture());
			renderer.Device.SetBlendMode(blendMode);
			shader.Render(() => renderer.DrawBatch(buffer, start, length, type));
			renderer.Device.SetBlendMode(BlendMode.None);
		}

		public void SetPalette(ITexture palette)
		{
			shader.SetTexture("Palette", palette);
		}

		public void SetViewportParams(Size screen, float depthScale, float depthOffset, float zoom, int2 scroll)
		{
			shader.SetVec("Scroll", scroll.X, scroll.Y, scroll.Y);
			shader.SetVec("r1",
				zoom * 2f / screen.Width,
				-zoom * 2f / screen.Height,
				-depthScale * zoom / screen.Height);
			shader.SetVec("r2", -1, 1, 1 - depthOffset);

			// Texture index is sampled as a float, so convert to pixels then scale
			shader.SetVec("DepthTextureScale", 128 * depthScale * zoom / screen.Height);
		}

		public void SetDepthPreviewEnabled(bool enabled)
		{
			shader.SetBool("EnableDepthPreview", enabled);
		}
	}
}
