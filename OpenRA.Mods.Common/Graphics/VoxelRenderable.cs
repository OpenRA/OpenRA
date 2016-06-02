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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Graphics
{
	public struct VoxelRenderable : IRenderable
	{
		readonly IEnumerable<VoxelAnimation> voxels;
		readonly WPos pos;
		readonly int zOffset;
		readonly WRot camera;
		readonly WRot lightSource;
		readonly float[] lightAmbientColor;
		readonly float[] lightDiffuseColor;
		readonly PaletteReference palette;
		readonly PaletteReference normalsPalette;
		readonly PaletteReference shadowPalette;
		readonly float scale;

		public VoxelRenderable(
			IEnumerable<VoxelAnimation> voxels, WPos pos, int zOffset, WRot camera, float scale,
			WRot lightSource, float[] lightAmbientColor, float[] lightDiffuseColor,
			PaletteReference color, PaletteReference normals, PaletteReference shadow)
		{
			this.voxels = voxels;
			this.pos = pos;
			this.zOffset = zOffset;
			this.scale = scale;
			this.camera = camera;
			this.lightSource = lightSource;
			this.lightAmbientColor = lightAmbientColor;
			this.lightDiffuseColor = lightDiffuseColor;
			palette = color;
			normalsPalette = normals;
			shadowPalette = shadow;
		}

		public WPos Pos { get { return pos; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return false; } }

		public IRenderable WithPalette(PaletteReference newPalette)
		{
			return new VoxelRenderable(
				voxels, pos, zOffset, camera, scale,
				lightSource, lightAmbientColor, lightDiffuseColor,
				newPalette, normalsPalette, shadowPalette);
		}

		public IRenderable WithZOffset(int newOffset)
		{
			return new VoxelRenderable(
				voxels, pos, newOffset, camera, scale,
				lightSource, lightAmbientColor, lightDiffuseColor,
				palette, normalsPalette, shadowPalette);
		}

		public IRenderable OffsetBy(WVec vec)
		{
			return new VoxelRenderable(
				voxels, pos + vec, zOffset, camera, scale,
				lightSource, lightAmbientColor, lightDiffuseColor,
				palette, normalsPalette, shadowPalette);
		}

		public IRenderable AsDecoration() { return this; }

		// This will need generalizing once we support TS/RA2 terrain
		static readonly float[] GroundNormal = new float[] { 0, 0, 1, 1 };
		public IFinalizedRenderable PrepareRender(WorldRenderer wr)
		{
			return new FinalizedVoxelRenderable(wr, this);
		}

		struct FinalizedVoxelRenderable : IFinalizedRenderable
		{
			readonly VoxelRenderable voxel;
			readonly VoxelRenderProxy renderProxy;

			public FinalizedVoxelRenderable(WorldRenderer wr, VoxelRenderable voxel)
			{
				this.voxel = voxel;
				var draw = voxel.voxels.Where(v => v.DisableFunc == null || !v.DisableFunc());

				renderProxy = Game.Renderer.WorldVoxelRenderer.RenderAsync(
					wr, draw, voxel.camera, voxel.scale, GroundNormal, voxel.lightSource,
					voxel.lightAmbientColor, voxel.lightDiffuseColor,
					voxel.palette, voxel.normalsPalette, voxel.shadowPalette);
			}

			public void Render(WorldRenderer wr)
			{
				var groundPos = voxel.pos - new WVec(0, 0, wr.World.Map.DistanceAboveTerrain(voxel.pos).Length);
				var groundZ = wr.World.Map.Grid.TileSize.Height * (groundPos.Z - voxel.pos.Z) / 1024f;
				var pxOrigin = wr.ScreenPosition(voxel.pos);
				var shadowOrigin = pxOrigin - groundZ * (new float2(renderProxy.ShadowDirection, 1));

				var psb = renderProxy.ProjectedShadowBounds;
				var sa = shadowOrigin + psb[0];
				var sb = shadowOrigin + psb[2];
				var sc = shadowOrigin + psb[1];
				var sd = shadowOrigin + psb[3];
				Game.Renderer.WorldRgbaSpriteRenderer.DrawSprite(renderProxy.ShadowSprite, sa, sb, sc, sd);
				Game.Renderer.WorldRgbaSpriteRenderer.DrawSprite(renderProxy.Sprite, pxOrigin - 0.5f * renderProxy.Sprite.Size);
			}

			public void RenderDebugGeometry(WorldRenderer wr)
			{
				var groundPos = voxel.pos - new WVec(0, 0, wr.World.Map.DistanceAboveTerrain(voxel.pos).Length);
				var groundZ = wr.World.Map.Grid.TileSize.Height * (groundPos.Z - voxel.pos.Z) / 1024f;
				var pxOrigin = wr.ScreenPosition(voxel.pos);
				var shadowOrigin = pxOrigin - groundZ * (new float2(renderProxy.ShadowDirection, 1));
				var iz = 1 / wr.Viewport.Zoom;

				// Draw sprite rect
				var offset = pxOrigin + renderProxy.Sprite.Offset - 0.5f * renderProxy.Sprite.Size;
				Game.Renderer.WorldRgbaColorRenderer.DrawRect(offset.XY, (offset + renderProxy.Sprite.Size).XY, iz, Color.Red);

				// Draw transformed shadow sprite rect
				var c = Color.Purple;
				var psb = renderProxy.ProjectedShadowBounds;

				Game.Renderer.WorldRgbaColorRenderer.DrawPolygon(new[]
				{
					shadowOrigin + psb[1],
					shadowOrigin + psb[3],
					shadowOrigin + psb[0],
					shadowOrigin + psb[2]
				}, iz, c);

				// Draw voxel bounding box
				var draw = voxel.voxels.Where(v => v.DisableFunc == null || !v.DisableFunc());
				var scaleTransform = OpenRA.Graphics.Util.ScaleMatrix(voxel.scale, voxel.scale, voxel.scale);
				var cameraTransform = OpenRA.Graphics.Util.MakeFloatMatrix(voxel.camera.AsMatrix());

				foreach (var v in draw)
				{
					var bounds = v.Voxel.Bounds(v.FrameFunc());
					var worldTransform = v.RotationFunc().Reverse().Aggregate(scaleTransform,
						(x, y) => OpenRA.Graphics.Util.MatrixMultiply(x, OpenRA.Graphics.Util.MakeFloatMatrix(y.AsMatrix())));

					float sx, sy, sz;
					wr.ScreenVectorComponents(v.OffsetFunc(), out sx, out sy, out sz);
					var pxPos = pxOrigin + new float2(sx, sy);
					var screenTransform = OpenRA.Graphics.Util.MatrixMultiply(cameraTransform, worldTransform);
					DrawBoundsBox(pxPos, screenTransform, bounds, iz, Color.Yellow);
				}
			}

			static readonly uint[] CornerXIndex = new uint[] { 0, 0, 0, 0, 3, 3, 3, 3 };
			static readonly uint[] CornerYIndex = new uint[] { 1, 1, 4, 4, 1, 1, 4, 4 };
			static readonly uint[] CornerZIndex = new uint[] { 2, 5, 2, 5, 2, 5, 2, 5 };
			static void DrawBoundsBox(float2 pxPos, float[] transform, float[] bounds, float width, Color c)
			{
				var wcr = Game.Renderer.WorldRgbaColorRenderer;
				var corners = new float2[8];
				for (var i = 0; i < 8; i++)
				{
					var vec = new float[] { bounds[CornerXIndex[i]], bounds[CornerYIndex[i]], bounds[CornerZIndex[i]], 1 };
					var screen = OpenRA.Graphics.Util.MatrixVectorMultiply(transform, vec);
					corners[i] = pxPos + new float2(screen[0], screen[1]);
				}

				// Front face
				wcr.DrawPolygon(new[] { corners[0], corners[1], corners[3], corners[2] }, width, c);

				// Back face
				wcr.DrawPolygon(new[] { corners[4], corners[5], corners[7], corners[6] }, width, c);

				// Horizontal edges
				wcr.DrawLine(corners[0], corners[4], width, c);
				wcr.DrawLine(corners[1], corners[5], width, c);
				wcr.DrawLine(corners[2], corners[6], width, c);
				wcr.DrawLine(corners[3], corners[7], width, c);
			}

			public Rectangle ScreenBounds(WorldRenderer wr)
			{
				var pxOrigin = wr.ScreenPosition(voxel.pos);
				var draw = voxel.voxels.Where(v => v.DisableFunc == null || !v.DisableFunc());
				var scaleTransform = OpenRA.Graphics.Util.ScaleMatrix(voxel.scale, voxel.scale, voxel.scale);
				var cameraTransform = OpenRA.Graphics.Util.MakeFloatMatrix(voxel.camera.AsMatrix());

				var minX = float.MaxValue;
				var minY = float.MaxValue;
				var maxX = float.MinValue;
				var maxY = float.MinValue;
				foreach (var v in draw)
				{
					var bounds = v.Voxel.Bounds(v.FrameFunc());
					var worldTransform = v.RotationFunc().Reverse().Aggregate(scaleTransform,
						(x, y) => OpenRA.Graphics.Util.MatrixMultiply(x, OpenRA.Graphics.Util.MakeFloatMatrix(y.AsMatrix())));

					float sx, sy, sz;
					wr.ScreenVectorComponents(v.OffsetFunc(), out sx, out sy, out sz);
					var pxPos = pxOrigin + new float2(sx, sy);
					var screenTransform = OpenRA.Graphics.Util.MatrixMultiply(cameraTransform, worldTransform);

					for (var i = 0; i < 8; i++)
					{
						var vec = new float[] { bounds[CornerXIndex[i]], bounds[CornerYIndex[i]], bounds[CornerZIndex[i]], 1 };
						var screen = OpenRA.Graphics.Util.MatrixVectorMultiply(screenTransform, vec);
						minX = Math.Min(minX, pxPos.X + screen[0]);
						minY = Math.Min(minY, pxPos.Y + screen[1]);
						maxX = Math.Max(maxX, pxPos.X + screen[0]);
						maxY = Math.Max(maxY, pxPos.Y + screen[1]);
					}
				}

				return Rectangle.FromLTRB((int)minX, (int)minY, (int)maxX, (int)maxY);
			}
		}
	}
}
