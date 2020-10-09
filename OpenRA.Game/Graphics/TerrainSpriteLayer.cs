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
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class TerrainSpriteLayer : IDisposable
	{
		static readonly int[] CornerVertexMap = { 0, 1, 2, 2, 3, 0 };

		public readonly Sheet Sheet;
		public readonly BlendMode BlendMode;

		readonly Sprite emptySprite;

		readonly IVertexBuffer<Vertex> vertexBuffer;
		readonly Vertex[] vertices;
		readonly bool[] ignoreTint;
		readonly HashSet<int> dirtyRows = new HashSet<int>();
		readonly int rowStride;
		readonly bool restrictToBounds;

		readonly WorldRenderer worldRenderer;
		readonly Map map;

		readonly PaletteReference palette;

		public TerrainSpriteLayer(World world, WorldRenderer wr, Sheet sheet, BlendMode blendMode, PaletteReference palette, bool restrictToBounds)
		{
			worldRenderer = wr;
			this.restrictToBounds = restrictToBounds;
			Sheet = sheet;
			BlendMode = blendMode;
			this.palette = palette;

			map = world.Map;
			rowStride = 6 * map.MapSize.X;

			vertices = new Vertex[rowStride * map.MapSize.Y];
			vertexBuffer = Game.Renderer.Context.CreateVertexBuffer(vertices.Length);
			emptySprite = new Sprite(sheet, Rectangle.Empty, TextureChannel.Alpha);

			wr.PaletteInvalidated += UpdatePaletteIndices;

			if (wr.TerrainLighting != null)
			{
				ignoreTint = new bool[rowStride * map.MapSize.Y];
				wr.TerrainLighting.CellChanged += UpdateTint;
			}
		}

		void UpdatePaletteIndices()
		{
			// Everything in the layer uses the same palette,
			// so we can fix the indices in one pass
			for (var i = 0; i < vertices.Length; i++)
			{
				var v = vertices[i];
				vertices[i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, palette.TextureIndex, v.C, v.R, v.G, v.B, v.A);
			}

			for (var row = 0; row < map.MapSize.Y; row++)
				dirtyRows.Add(row);
		}

		public void Clear(CPos cell)
		{
			Update(cell, null, true);
		}

		public void Update(CPos cell, ISpriteSequence sequence, int frame)
		{
			Update(cell, sequence.GetSprite(frame), sequence.IgnoreWorldTint);
		}

		public void Update(CPos cell, Sprite sprite, bool ignoreTint)
		{
			var xyz = float3.Zero;
			if (sprite != null)
			{
				var cellOrigin = map.CenterOfCell(cell) - new WVec(0, 0, map.Grid.Ramps[map.Ramp[cell]].CenterHeightOffset);
				xyz = worldRenderer.Screen3DPosition(cellOrigin) + sprite.Offset - 0.5f * sprite.Size;
			}

			Update(cell.ToMPos(map.Grid.Type), sprite, xyz, ignoreTint);
		}

		void UpdateTint(MPos uv)
		{
			var offset = rowStride * uv.V + 6 * uv.U;
			if (ignoreTint[offset])
			{
				var noTint = float3.Ones;
				for (var i = 0; i < 6; i++)
				{
					var v = vertices[offset + i];
					vertices[offset + i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, palette.TextureIndex, v.C, noTint, 1f);
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
				vertices[offset + i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, palette.TextureIndex, v.C, weights[CornerVertexMap[i]], 1f);
			}

			dirtyRows.Add(uv.V);
		}

		public void Update(MPos uv, Sprite sprite, in float3 pos, bool ignoreTint)
		{
			if (sprite != null)
			{
				if (sprite.Sheet != Sheet)
					throw new InvalidDataException("Attempted to add sprite from a different sheet");

				if (sprite.BlendMode != BlendMode)
					throw new InvalidDataException("Attempted to add sprite with a different blend mode");
			}
			else
				sprite = emptySprite;

			// The vertex buffer does not have geometry for cells outside the map
			if (!map.Tiles.Contains(uv))
				return;

			var offset = rowStride * uv.V + 6 * uv.U;
			Util.FastCreateQuad(vertices, pos, sprite, int2.Zero, palette.TextureIndex, offset, sprite.Size, float3.Ones, 1f);

			if (worldRenderer.TerrainLighting != null)
			{
				this.ignoreTint[offset] = ignoreTint;
				UpdateTint(uv);
			}

			dirtyRows.Add(uv.V);
		}

		public void Draw(Viewport viewport)
		{
			var cells = restrictToBounds ? viewport.VisibleCellsInsideBounds : viewport.AllVisibleCells;

			// Only draw the rows that are visible.
			var firstRow = cells.CandidateMapCoords.TopLeft.V.Clamp(0, map.MapSize.Y);
			var lastRow = (cells.CandidateMapCoords.BottomRight.V + 1).Clamp(firstRow, map.MapSize.Y);

			Game.Renderer.Flush();

			// Flush any visible changes to the GPU
			for (var row = firstRow; row <= lastRow; row++)
			{
				if (!dirtyRows.Remove(row))
					continue;

				var rowOffset = rowStride * row;
				vertexBuffer.SetData(vertices, rowOffset, rowOffset, rowStride);
			}

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, rowStride * firstRow, rowStride * (lastRow - firstRow),
				PrimitiveType.TriangleList, Sheet, BlendMode);

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
