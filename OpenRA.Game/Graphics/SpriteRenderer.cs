#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		struct BlendSpan
		{
			public readonly int Start;
			public int Length;
			public readonly BlendMode Mode;

			public BlendSpan(int start, int length, BlendMode mode)
			{
				Start = start;
				Length = length;
				Mode = mode;
			}
		}

		public const int SheetCount = 8;
		static readonly string[] SheetIndexToTextureName = Exts.MakeArray(SheetCount, i => $"Texture{i}");
		static readonly int UintSize = Marshal.SizeOf<uint>();

		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<Vertex> vertexBuffer;
		readonly IIndexBuffer indexBuffer;

		Vertex[] vertices;
		readonly Sheet[] sheets = new Sheet[SheetCount];

		readonly List<BlendSpan> blendSpans = [];
		int vertexCount = 0;
		int sheetCount = 0;

		public SpriteRenderer(Renderer renderer, IVertexBuffer<Vertex> tempVertexBuffer, IIndexBuffer tempIndexBuffer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
			vertexBuffer = tempVertexBuffer;
			indexBuffer = tempIndexBuffer;
			vertices = renderer.Context.CreateVertices<Vertex>(renderer.TempVertexBufferSize);
		}

		public void Flush()
		{
			if (vertexCount <= 0)
				return;

			for (var i = 0; i < sheetCount; i++)
				shader.SetTexture(SheetIndexToTextureName[i], sheets[i].GetTexture());

			shader.PrepareRender();
			vertexBuffer.SetData(ref vertices, vertexCount);
			vertexBuffer.Bind();
			indexBuffer.Bind();
			shader.Bind();

			// PERF: this allows us to batch render sprites with interleaved blend modes
			// without expensive binding and data writing between draw calls.
			foreach (var span in blendSpans)
			{
				if (span.Length <= 0)
					continue;

				renderer.Context.SetBlendMode(span.Mode);
				renderer.Context.DrawElements(span.Length / 4 * 6, span.Start * 6);
			}

			if (blendSpans[^1].Mode != BlendMode.None)
				renderer.Context.SetBlendMode(BlendMode.None);

			PerfHistory.Increment("batches", 1);
			blendSpans.Clear();
			Array.Clear(sheets, 0, SheetCount);
			vertexCount = 0;
			sheetCount = 0;
		}

		void TrackQuad(BlendMode blendMode)
		{
			if (blendSpans.Count == 0 || blendSpans[^1].Mode != blendMode)
				blendSpans.Add(new BlendSpan(vertexCount, 4, blendMode));
			else
			{
				// PERF: modify in-place.
				var span = CollectionsMarshal.AsSpan(blendSpans);
				span[^1].Length += 4;
			}

			vertexCount += 4;
		}

		int2 SetRenderStateForSprite(Sprite s)
		{
			renderer.CurrentBatchRenderer = this;

			if (vertexCount + 4 > renderer.TempVertexBufferSize)
				Flush();

			// Check if the sheet (or secondary data sheet) have already been mapped
			var sheet = s.Sheet;
			var sheetIndex = 0;
			for (; sheetIndex < sheetCount; sheetIndex++)
				if (sheets[sheetIndex] == sheet)
					break;

			var secondarySheetIndex = 0;
			var ss = s as SpriteWithSecondaryData;
			if (ss != null)
			{
				var secondarySheet = ss.SecondarySheet;
				for (; secondarySheetIndex < sheetCount; secondarySheetIndex++)
					if (sheets[secondarySheetIndex] == secondarySheet)
						break;

				// If neither sheet has been mapped both index values will be set to ns.
				// This is fine if they both reference the same texture, but if they don't
				// we must increment the secondary sheet index to the next free sampler.
				if (secondarySheetIndex == sheetIndex && secondarySheet != sheet)
					secondarySheetIndex++;
			}

			// Make sure that we have enough free samplers to map both if needed, otherwise flush
			if (Math.Max(sheetIndex, secondarySheetIndex) >= sheets.Length)
			{
				Flush();
				sheetIndex = 0;
				secondarySheetIndex = ss != null && ss.SecondarySheet != sheet ? 1 : 0;
			}

			if (sheetIndex >= sheetCount)
			{
				sheets[sheetIndex] = sheet;
				sheetCount++;
			}

			if (secondarySheetIndex >= sheetCount && ss != null)
			{
				sheets[secondarySheetIndex] = ss.SecondarySheet;
				sheetCount++;
			}

			return new int2(sheetIndex, secondarySheetIndex);
		}

		static int ResolveTextureIndex(Sprite s, PaletteReference pal)
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

		internal void DrawSprite(Sprite s, int paletteTextureIndex, in Vector3 location, in Vector3 scale, float rotation = 0f)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + scale * s.Offset, s, samplers, paletteTextureIndex, vertexCount, scale * s.Size, Vector3.One,
								1f, rotation);
			TrackQuad(s.BlendMode);
		}

		internal void DrawSprite(Sprite s, int paletteTextureIndex, in Vector3 location, float scale, float rotation = 0f)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + scale * s.Offset, s, samplers, paletteTextureIndex, vertexCount, scale * s.Size, Vector3.One,
								1f, rotation);
			TrackQuad(s.BlendMode);
		}

		public void DrawSprite(Sprite s, PaletteReference pal, in Vector3 location, float scale = 1f, float rotation = 0f)
		{
			DrawSprite(s, ResolveTextureIndex(s, pal), location, scale, rotation);
		}

		internal void DrawSprite(Sprite s, int paletteTextureIndex, in Vector3 location, float scale, in Vector3 tint, float alpha,
			float rotation = 0f)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, location + scale * s.Offset, s, samplers, paletteTextureIndex, vertexCount, scale * s.Size, tint, alpha,
								rotation);
			TrackQuad(s.BlendMode);
		}

		public void DrawSprite(Sprite s, PaletteReference pal, in Vector3 location, float scale, in Vector3 tint, float alpha,
			float rotation = 0f)
		{
			DrawSprite(s, ResolveTextureIndex(s, pal), location, scale, tint, alpha, rotation);
		}

		internal void DrawSprite(Sprite s, int paletteTextureIndex, in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d, in Vector3 tint, float alpha)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, a, b, c, d, s, samplers, paletteTextureIndex, tint, alpha, vertexCount);
			TrackQuad(s.BlendMode);
		}

		public void DrawVertexBuffer(IVertexBuffer<Vertex> buffer, IIndexBuffer indices, int start, int length, IEnumerable<Sheet> sheets, BlendMode blendMode)
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
			renderer.DrawQuadBatch(buffer, indices, shader, length, UintSize * start);
			renderer.Context.SetBlendMode(BlendMode.None);
		}

		// PERF: methods that throw won't be inlined by the JIT, so extract a static helper for use on hot paths
		static void ThrowSheetOverflow(string paramName)
		{
			throw new ArgumentException($"SpriteRenderer only supports {SheetCount} simultaneous textures", paramName);
		}

		// For RGBAColorRenderer
		internal void DrawRGBAQuad(Vertex[] v, BlendMode blendMode)
		{
			renderer.CurrentBatchRenderer = this;

			if (vertexCount + 4 > renderer.TempVertexBufferSize)
				Flush();

			Array.Copy(v, 0, vertices, vertexCount, v.Length);
			TrackQuad(blendMode);
		}

		public void SetPalette(HardwarePalette palette)
		{
			shader.SetTexture("Palette", palette.Texture);
			shader.SetTexture("ColorShifts", palette.ColorShifts);
			shader.SetVec("PaletteRows", palette.Height);
		}

		public void SetViewportParams(Size sheetSize, int downscale, float depthMargin, int2 scroll)
		{
			// OpenGL only renders x and y coordinates inside [-1, 1] range. We project world coordinates
			// using p1 to values [0, 2] and then subtract by 1 using p2, where p stands for projection. It's
			// standard practice for shaders to use a projection matrix, but as we project orthographically
			// we are able to send less data to the GPU.
			var width = 2f / (downscale * sheetSize.Width);
			var height = 2f / (downscale * sheetSize.Height);

			// Depth is more complicated:
			// * The OpenGL z axis is inverted (negative is closer) relative to OpenRA (positive is closer).
			// * We want to avoid clipping pixels that are behind the nominal z == y plane at the
			//   top of the map, or above the nominal z == y plane at the bottom of the map.
			//   We therefore expand the depth range by an extra margin that is calculated based on
			//   the maximum expected world height (see Renderer.InitializeDepthBuffer).
			// * Sprites can specify an additional per-pixel depth offset map, which is applied in the
			//   fragment shader. The fragment shader operates in OpenGL window coordinates, not NDC,
			//   with a depth range [0, 1] corresponding to the NDC [-1, 1]. We must therefore multiply the
			//   sprite channel value [0, 1] by 255 to find the pixel depth offset, then by our depth scale
			//   to find the equivalent NDC offset, then divide by 2 to find the window coordinate offset.
			// * If depthMargin == 0 (which indicates per-pixel depth testing is disabled) sprites that
			//   extend beyond the top of bottom edges of the screen may be pushed outside [-1, 1] and
			//   culled by the GPU. We avoid this by forcing everything into the z = 0 plane.
			var depth = depthMargin != 0f ? 2f / (downscale * (sheetSize.Height + depthMargin)) : 0;
			shader.SetVec("DepthTextureScale", 128 * depth);
			shader.SetVec("Scroll", scroll.X, scroll.Y, depthMargin != 0f ? scroll.Y : 0);
			shader.SetVec("p1", width, height, -depth);
			shader.SetVec("p2", -1, -1, depthMargin != 0f ? 1 : 0);
		}

		public void SetDepthPreview(bool enabled, float contrast, float offset)
		{
			shader.SetBool("EnableDepthPreview", enabled);
			shader.SetVec("DepthPreviewParams", contrast, offset);
		}

		public void EnablePixelArtScaling(bool enabled)
		{
			shader.SetBool("EnablePixelArtScaling", enabled);
		}
	}
}
