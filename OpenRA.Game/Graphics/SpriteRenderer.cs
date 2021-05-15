#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		public const int SheetCount = 8;
		static readonly string[] SheetIndexToTextureName = Exts.MakeArray(SheetCount, i => $"Texture{i}");

		readonly Renderer renderer;
		readonly IShader shader;

		readonly Vertex[] vertices;
		readonly Sheet[] sheets = new Sheet[SheetCount];

		BlendMode currentBlend = BlendMode.Alpha;
		int nv = 0;
		int ns = 0;

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
				for (var i = 0; i < ns; i++)
				{
					shader.SetTexture(SheetIndexToTextureName[i], sheets[i].GetTexture());
					sheets[i] = null;
				}

				renderer.Context.SetBlendMode(currentBlend);
				shader.PrepareRender();
				renderer.DrawBatch(vertices, nv, PrimitiveType.TriangleList);
				renderer.Context.SetBlendMode(BlendMode.None);

				nv = 0;
				ns = 0;
			}
		}

		int2 SetRenderStateForSprite(Sprite s)
		{
			renderer.CurrentBatchRenderer = this;

			if (s.BlendMode != currentBlend || nv + 6 > renderer.TempBufferSize)
				Flush();

			currentBlend = s.BlendMode;

			// Check if the sheet (or secondary data sheet) have already been mapped
			var sheet = s.Sheet;
			var sheetIndex = 0;
			for (; sheetIndex < ns; sheetIndex++)
				if (sheets[sheetIndex] == sheet)
					break;

			var secondarySheetIndex = 0;
			var ss = s as SpriteWithSecondaryData;
			if (ss != null)
			{
				var secondarySheet = ss.SecondarySheet;
				for (; secondarySheetIndex < ns; secondarySheetIndex++)
					if (sheets[secondarySheetIndex] == secondarySheet)
						break;
			}

			// Make sure that we have enough free samplers to map both if needed, otherwise flush
			var needSamplers = (sheetIndex == ns ? 1 : 0) + (secondarySheetIndex == ns ? 1 : 0);
			if (ns + needSamplers >= sheets.Length)
			{
				Flush();
				sheetIndex = 0;
				if (ss != null)
					secondarySheetIndex = 1;
			}

			if (sheetIndex >= ns)
			{
				sheets[sheetIndex] = sheet;
				ns += 1;
			}

			if (secondarySheetIndex >= ns && ss != null)
			{
				sheets[secondarySheetIndex] = ss.SecondarySheet;
				ns += 1;
			}

			return new int2(sheetIndex, secondarySheetIndex);
		}

		internal void DrawSprite(Sprite s, in float3 location, float paletteTextureIndex, in float3 size)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + s.FractionalOffset * size, s, samplers, paletteTextureIndex, nv, size, float3.Ones, 1f);
			nv += 6;
		}

		float ResolveTextureIndex(Sprite s, PaletteReference pal)
		{
			if (pal == null)
				return 0;

			// PERF: Remove useless palette assignments for RGBA sprites
			// HACK: This is working around the limitation that palettes are defined on traits rather than on sequences,
			// and can be removed once this has been fixed
			if (s.Channel == TextureChannel.RGBA && !pal.HasColorShift)
				return 0;

			return pal.TextureIndex;
		}

		public void DrawSprite(Sprite s, in float3 location, PaletteReference pal)
		{
			DrawSprite(s, location, ResolveTextureIndex(s, pal), s.Size);
		}

		public void DrawSprite(Sprite s, in float3 location, PaletteReference pal, float3 size)
		{
			DrawSprite(s, location, ResolveTextureIndex(s, pal), size);
		}

		public void DrawSprite(Sprite s, in float3 a, in float3 b, in float3 c, in float3 d)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, a, b, c, d, s, samplers, 0, float3.Ones, 1f, nv);
			nv += 6;
		}

		internal void DrawSprite(Sprite s, in float3 location, float paletteTextureIndex, in float3 size, in float3 tint, float alpha)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + s.FractionalOffset * size, s, samplers, paletteTextureIndex, nv, size, tint, alpha);
			nv += 6;
		}

		public void DrawSprite(Sprite s, in float3 location, PaletteReference pal, in float3 size, in float3 tint, float alpha)
		{
			DrawSprite(s, location, ResolveTextureIndex(s, pal), size, tint, alpha);
		}

		public void DrawSprite(Sprite s, in float3 a, in float3 b, in float3 c, in float3 d, in float3 tint, float alpha)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, a, b, c, d, s, samplers, 0, tint, alpha, nv);
			nv += 6;
		}

		public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, IEnumerable<Sheet> sheets, BlendMode blendMode)
		{
			var i = 0;
			foreach (var s in sheets)
			{
				if (i >= SheetCount)
					ThrowSheetOverflow(nameof(sheets));

				if (s != null)
					shader.SetTexture(SheetIndexToTextureName[i++], s.GetTexture());
			}

			renderer.Context.SetBlendMode(blendMode);
			shader.PrepareRender();
			renderer.DrawBatch(buffer, start, length, type);
			renderer.Context.SetBlendMode(BlendMode.None);
		}

		// PERF: methods that throw won't be inlined by the JIT, so extract a static helper for use on hot paths
		static void ThrowSheetOverflow(string paramName)
		{
			throw new ArgumentException($"SpriteRenderer only supports {SheetCount} simultaneous textures", paramName);
		}

		// For RGBAColorRenderer
		internal void DrawRGBAVertices(Vertex[] v, BlendMode blendMode)
		{
			renderer.CurrentBatchRenderer = this;

			if (currentBlend != blendMode || nv + v.Length > renderer.TempBufferSize)
				Flush();

			currentBlend = blendMode;
			Array.Copy(v, 0, vertices, nv, v.Length);
			nv += v.Length;
		}

		public void SetPalette(ITexture palette, ITexture colorShifts)
		{
			shader.SetTexture("Palette", palette);
			shader.SetTexture("ColorShifts", colorShifts);
		}

		public void SetViewportParams(Size sheetSize, int downscale, float depthMargin, int2 scroll)
		{
			// Calculate the effective size of the render surface in viewport pixels
			var width = downscale * sheetSize.Width;
			var height = downscale * sheetSize.Height;
			var depthScale = height / (height + depthMargin);
			var depthOffset = depthScale / 2;
			shader.SetVec("Scroll", scroll.X, scroll.Y, scroll.Y);
			shader.SetVec("r1",
				2f / width,
				2f / height,
				-depthScale / height);
			shader.SetVec("r2", -1, -1, 1 - depthOffset);

			// Texture index is sampled as a float, so convert to pixels then scale
			shader.SetVec("DepthTextureScale", 128 * depthScale / height);
		}

		public void SetDepthPreviewEnabled(bool enabled)
		{
			shader.SetBool("EnableDepthPreview", enabled);
		}

		public void SetAntialiasingPixelsPerTexel(float pxPerTx)
		{
			shader.SetVec("AntialiasPixelsPerTexel", pxPerTx);
		}
	}
}
