#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRA.Graphics
{
	/// <summary>
	/// Maintains a layer of sprites that can cover any cell on the terrain.
	/// </summary>
	public sealed class TerrainSpriteLayer : IDisposable
	{
		public readonly Sheet Sheet;
		public readonly BlendMode BlendMode;

		readonly Sprite emptySprite;

		readonly IVertexBuffer<Vertex> vertexBuffer;
		readonly Vertex[] vertices;
		readonly HashSet<int> dirtyRows;
		readonly int rowStride;
		readonly bool restrictToBounds;

		readonly WorldRenderer worldRenderer;
		readonly Map map;

		readonly PaletteReference palette;
		readonly bool buffered;

		/// <summary>
		/// Initializes a new <see cref="TerrainSpriteLayer"/>.
		/// </summary>
		/// <param name="world">The world containing the map over which the layer resides.</param>
		/// <param name="wr">The renderer used to draw the world.</param>
		/// <param name="sheet">The sheet which contains all the sprites this layer will render. All the sprites must
		/// reside on this single sheet.</param>
		/// <param name="blendMode">The blend mode to use when drawing the sprites. The blend mode of all sprites must
		/// use this same mode.</param>
		/// <param name="palette">The palette that should be used to render the sprites.</param>
		/// <param name="restrictToBounds">Indicates if only cells within the map bounds should be drawn; otherwise,
		/// all cells including those outside the map bounds will be drawn.</param>
		/// <param name="buffered">Indicates if updates should occur in a buffer, set this when frequent updates are
		/// expected. Using a buffer saves CPU, whereas not using a buffer saves memory.</param>
		public TerrainSpriteLayer(
			World world, WorldRenderer wr, Sheet sheet, BlendMode blendMode, PaletteReference palette,
			bool restrictToBounds, bool buffered)
		{
			worldRenderer = wr;
			this.restrictToBounds = restrictToBounds;
			Sheet = sheet;
			BlendMode = blendMode;
			this.palette = palette;

			map = world.Map;
			rowStride = 6 * map.MapSize.X;

			// We can use a buffer to track updates. We can then only send updates in the visible region to the GPU.
			// This saves CPU by only sending updated rows when they become visible. This requires keeping a full copy
			// of the vertices, so it doubles the memory required.
			this.buffered = buffered;

			if (buffered)
				dirtyRows = new HashSet<int>();

			vertices = new Vertex[buffered ? rowStride * map.MapSize.Y : 6];
			vertexBuffer = Game.Renderer.Device.CreateVertexBuffer(rowStride * map.MapSize.Y);
			emptySprite = new Sprite(sheet, Rectangle.Empty, TextureChannel.Alpha);

			wr.PaletteInvalidated += UpdatePaletteIndices;
		}

		void UpdatePaletteIndices()
		{
			if (buffered)
			{
				// Everything in the layer uses the same palette,
				// so we can fix the indices in one pass
				for (var i = 0; i < vertices.Length; i++)
				{
					var v = vertices[i];
					vertices[i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, palette.TextureIndex, v.C);
				}

				for (var row = 0; row < map.MapSize.Y; row++)
					dirtyRows.Add(row);
			}
			else
			{
				var vertexBufferLength = rowStride * map.MapSize.Y;
				var buffer = new Vertex[Math.Min(2048, vertexBufferLength)];

				var start = 0;
				while (start < vertexBufferLength)
				{
					var length = Math.Min(buffer.Length, vertexBufferLength - start);
					vertexBuffer.GetData(buffer, start, length);

					for (var i = 0; i < length; i++)
					{
						var v = buffer[i];
						buffer[i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, palette.TextureIndex, v.C);
					}

					vertexBuffer.SetData(buffer, start, length);

					start += length;
				}
			}
		}

		public void Update(CPos cell, Sprite sprite)
		{
			var xyz = sprite == null ? float3.Zero :
				worldRenderer.Screen3DPosition(map.CenterOfCell(cell)) + sprite.Offset - 0.5f * sprite.Size;

			Update(cell.ToMPos(map.Grid.Type), sprite, xyz);
		}

		public void Update(MPos uv, Sprite sprite, float3 pos)
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
			if (buffered)
			{
				Util.FastCreateQuad(vertices, pos, sprite, palette.TextureIndex, offset, sprite.Size);
				dirtyRows.Add(uv.V);
			}
			else
			{
				Util.FastCreateQuad(vertices, pos, sprite, palette.TextureIndex, 0, sprite.Size);
				vertexBuffer.SetData(vertices, offset, vertices.Length);
			}
		}

		public void Draw(Viewport viewport)
		{
			var cells = restrictToBounds ? viewport.VisibleCellsInsideBounds : viewport.AllVisibleCells;

			// Only draw the rows that are visible.
			var firstRow = cells.CandidateMapCoords.TopLeft.V.Clamp(0, map.MapSize.Y);
			var lastRow = (cells.CandidateMapCoords.BottomRight.V + 1).Clamp(firstRow, map.MapSize.Y);

			Game.Renderer.Flush();

			// Flush any visible changes to the GPU
			if (buffered)
			{
				for (var row = firstRow; row <= lastRow; row++)
				{
					if (!dirtyRows.Remove(row))
						continue;

					var rowOffset = rowStride * row;

					unsafe
					{
						// The compiler / language spec won't let us calculate a pointer to
						// an offset inside a generic array T[], and so we are forced to
						// calculate the start-of-row pointer here to pass in to SetData.
						fixed (Vertex* vPtr = &vertices[0])
							vertexBuffer.SetData((IntPtr)(vPtr + rowOffset), rowOffset, rowStride);
					}
				}
			}

			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, rowStride * firstRow, rowStride * (lastRow - firstRow),
				PrimitiveType.TriangleList, Sheet, BlendMode);

			Game.Renderer.Flush();
		}

		public void Dispose()
		{
			worldRenderer.PaletteInvalidated -= UpdatePaletteIndices;
			vertexBuffer.Dispose();
		}
	}
}
