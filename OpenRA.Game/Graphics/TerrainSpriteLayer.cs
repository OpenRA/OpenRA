#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class TerrainSpriteLayer : IDisposable
	{
		static readonly int[] CornerVertexMap = { 0, 1, 2, 2, 3, 0 };

		public readonly BlendMode BlendMode;

		readonly Sheet[] sheets;
		readonly Sprite emptySprite;

		readonly int binSize;
		readonly int binCols;
		readonly int binStride;

		readonly IVertexBuffer<Vertex> vertexBuffer;
		readonly Vertex[] vertices;
		readonly bool[] ignoreTint;
		readonly bool[] dirtyBins;

		readonly bool restrictToBounds;

		readonly WorldRenderer worldRenderer;
		readonly Map map;

		readonly PaletteReference[] palettes;

		public TerrainSpriteLayer(World world, WorldRenderer wr, int binSize, Sprite emptySprite, BlendMode blendMode, bool restrictToBounds)
		{
			worldRenderer = wr;
			this.binSize = binSize;
			this.restrictToBounds = restrictToBounds;
			this.emptySprite = emptySprite;
			sheets = new Sheet[SpriteRenderer.SheetCount];
			BlendMode = blendMode;

			map = world.Map;
			binCols = Exts.IntegerDivisionRoundingAwayFromZero(map.MapSize.X, binSize);
			var binRows = Exts.IntegerDivisionRoundingAwayFromZero(map.MapSize.Y, binSize);
			binStride = 6 * binSize * binSize;
			vertices = new Vertex[binRows * binCols * binStride];

			palettes = new PaletteReference[map.MapSize.X * map.MapSize.Y];
			vertexBuffer = Game.Renderer.Context.CreateVertexBuffer(vertices.Length);

			dirtyBins = new bool[binRows * binCols];
			Array.Fill(dirtyBins, true, 0, dirtyBins.Length);

			wr.PaletteInvalidated += UpdatePaletteIndices;

			if (wr.TerrainLighting != null)
			{
				ignoreTint = new bool[vertices.Length];
				wr.TerrainLighting.CellChanged += UpdateTint;
			}
		}

		int VertexOffset(MPos uv)
		{
			var binCol = uv.U / binSize;
			var binRow = uv.V / binSize;
			var dx = uv.U - binCol * binSize;
			var dy = uv.V - binRow * binSize;

			return binStride * (binRow * binCols + binCol) + 6 * (dy * binSize + dx);
		}

		void MarkDirty(MPos uv)
		{
			var binCol = uv.U / binSize;
			var binRow = uv.V / binSize;

			dirtyBins[binRow * binCols + binCol] = true;
		}

		void UpdatePaletteIndices()
		{
			for (var i = 0; i < vertices.Length; i++)
			{
				var v = vertices[i];
				var p = palettes[i / 6]?.TextureIndex ?? 0;
				vertices[i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, p, v.C, v.R, v.G, v.B, v.A);
			}

			Array.Fill(dirtyBins, true, 0, dirtyBins.Length);
		}

		public void Clear(CPos cell)
		{
			Update(cell, null, null, 1f, 1f, true);
		}

		public void Update(CPos cell, ISpriteSequence sequence, PaletteReference palette, int frame)
		{
			Update(cell, sequence.GetSprite(frame), palette, sequence.Scale, sequence.GetAlpha(frame), sequence.IgnoreWorldTint);
		}

		public void Update(CPos cell, Sprite sprite, PaletteReference palette, float scale = 1f, float alpha = 1f, bool ignoreTint = false)
		{
			var xyz = float3.Zero;
			if (sprite != null)
			{
				var cellOrigin = map.CenterOfCell(cell) - new WVec(0, 0, map.Grid.Ramps[map.Ramp[cell]].CenterHeightOffset);
				xyz = worldRenderer.Screen3DPosition(cellOrigin) + scale * (sprite.Offset - 0.5f * sprite.Size);
			}

			Update(cell.ToMPos(map.Grid.Type), sprite, palette, xyz, scale, alpha, ignoreTint);
		}

		void UpdateTint(MPos uv)
		{
			var offset = VertexOffset(uv);
			if (ignoreTint[offset])
			{
				for (var i = 0; i < 6; i++)
				{
					var v = vertices[offset + i];
					vertices[offset + i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, v.P, v.C, v.A * float3.Ones, v.A);
				}

				return;
			}

			// Allow the terrain tint to vary linearly across the cell to smooth out the staircase effect
			// This is done by sampling the lighting the corners of the sprite, even though those pixels are
			// transparent for isometric tiles
			var tl = worldRenderer.TerrainLighting;
			var pos = map.CenterOfCell(uv.ToCPos(map));
			var step = map.Grid.Type == MapGridType.RectangularIsometric ? 724 : 512;
			var weights = new[]
			{
				tl.TintAt(pos + new WVec(-step, -step, 0)),
				tl.TintAt(pos + new WVec(step, -step, 0)),
				tl.TintAt(pos + new WVec(step, step, 0)),
				tl.TintAt(pos + new WVec(-step, step, 0))
			};

			// Apply tint directly to the underlying vertices
			// This saves us from having to re-query the sprite information, which has not changed
			for (var i = 0; i < 6; i++)
			{
				var v = vertices[offset + i];
				vertices[offset + i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, v.P, v.C, v.A * weights[CornerVertexMap[i]], v.A);
			}

			MarkDirty(uv);
		}

		int GetOrAddSheetIndex(Sheet sheet)
		{
			if (sheet == null)
				return 0;

			for (var i = 0; i < sheets.Length; i++)
			{
				if (sheets[i] == sheet)
					return i;

				if (sheets[i] == null)
				{
					sheets[i] = sheet;
					return i;
				}
			}

			throw new InvalidDataException("Sheet overflow");
		}

		public void Update(MPos uv, Sprite sprite, PaletteReference palette, in float3 pos, float scale, float alpha, bool ignoreTint)
		{
			int2 samplers;
			if (sprite != null)
			{
				if (sprite.BlendMode != BlendMode)
					throw new InvalidDataException("Attempted to add sprite with a different blend mode");

				samplers = new int2(GetOrAddSheetIndex(sprite.Sheet), GetOrAddSheetIndex((sprite as SpriteWithSecondaryData)?.SecondarySheet));

				// PERF: Remove useless palette assignments for RGBA sprites
				// HACK: This is working around the limitation that palettes are defined on traits rather than on sequences,
				// and can be removed once this has been fixed
				if (sprite.Channel == TextureChannel.RGBA && !(palette?.HasColorShift ?? false))
					palette = null;
			}
			else
			{
				sprite = emptySprite;
				samplers = int2.Zero;
			}

			// The vertex buffer does not have geometry for cells outside the map
			if (!map.Tiles.Contains(uv))
				return;

			var offset = VertexOffset(uv);
			Util.FastCreateQuad(vertices, pos, sprite, samplers, palette?.TextureIndex ?? 0, offset, scale * sprite.Size, alpha * float3.Ones, alpha);
			palettes[uv.V * map.MapSize.X + uv.U] = palette;

			if (worldRenderer.TerrainLighting != null)
			{
				this.ignoreTint[offset] = ignoreTint;
				UpdateTint(uv);
			}

			MarkDirty(uv);
		}

		public void Draw(Viewport viewport)
		{
			var cells = restrictToBounds ? viewport.VisibleCellsInsideBounds : viewport.AllVisibleCells;

			// Only update and draw bins that are visible
			var tl = cells.CandidateMapCoords.TopLeft;
			var br = cells.CandidateMapCoords.BottomRight;
			var minCol = tl.U.Clamp(0, map.MapSize.X) / binSize;
			var maxCol = br.U.Clamp(0, map.MapSize.X) / binSize;
			var minRow = tl.V.Clamp(0, map.MapSize.Y) / binSize;
			var maxRow = br.V.Clamp(0, map.MapSize.Y) / binSize;

			Game.Renderer.WorldSpriteRenderer.SetRenderStateForVertexBuffer(vertexBuffer, sheets, BlendMode);

			// Flush any visible changes to the GPU
			for (var row = minRow; row <= maxRow; row++)
			{
				for (var col = minCol; col <= maxCol;)
				{
					var i = row * binCols + col;
					if (!dirtyBins[i])
					{
						col++;
						continue;
					}

					// Coalesce adjacent bin updates to reduce SetData calls
					var updateStart = i * binStride;
					var updateLength = binStride;
					dirtyBins[i] = false;

					while (++col <= maxCol && dirtyBins[++i])
					{
						updateLength += binStride;
						dirtyBins[i] = false;
					}

					vertexBuffer.SetData(vertices, updateStart, updateStart, updateLength);
				}

				var offset = binStride * (row * binCols + minCol);
				var length = binStride * (maxCol - minCol + 1);

				Game.Renderer.Context.DrawPrimitives(PrimitiveType.TriangleList, offset, length);
			}

			Game.Renderer.Flush();
		}

		public void Dispose()
		{
			worldRenderer.PaletteInvalidated -= UpdatePaletteIndices;
			if (worldRenderer.TerrainLighting != null)
				worldRenderer.TerrainLighting.CellChanged -= UpdateTint;

			vertexBuffer.Dispose();
		}
	}
}
