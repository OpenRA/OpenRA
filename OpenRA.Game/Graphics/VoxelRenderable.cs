#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Graphics
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

		public VoxelRenderable(IEnumerable<VoxelAnimation> voxels, WPos pos, int zOffset, WRot camera, float scale,
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
			this.palette = color;
			this.normalsPalette = normals;
			this.shadowPalette = shadow;
		}

		public WPos Pos { get { return pos; } }
		public float Scale { get { return scale; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }

		public IRenderable WithScale(float newScale)
		{
			return new VoxelRenderable(voxels, pos, zOffset, camera, newScale,
			                           lightSource, lightAmbientColor, lightDiffuseColor,
			                           palette, normalsPalette, shadowPalette);
		}

		public IRenderable WithPalette(PaletteReference newPalette)
		{
			return new VoxelRenderable(voxels, pos, zOffset, camera, scale,
			                           lightSource, lightAmbientColor, lightDiffuseColor,
			                           newPalette, normalsPalette, shadowPalette);
		}

		public IRenderable WithZOffset(int newOffset)
		{
			return new VoxelRenderable(voxels, pos, newOffset, camera, scale,
			                           lightSource, lightAmbientColor, lightDiffuseColor,
			                           palette, normalsPalette, shadowPalette);
		}

		public IRenderable WithPos(WPos newPos)
		{
			return new VoxelRenderable(voxels, newPos, zOffset, camera, scale,
			                           lightSource, lightAmbientColor, lightDiffuseColor,
			                           palette, normalsPalette, shadowPalette);
		}

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			// Depth and shadow buffers are cleared between actors so that
			// overlapping units and shadows behave like overlapping sprites.
			var vr = Game.Renderer.WorldVoxelRenderer;
			var draw = voxels.Where(v => v.DisableFunc == null || !v.DisableFunc());

			foreach (var v in draw)
				v.Voxel.PrepareForDraw(wr, pos + v.OffsetFunc(), v.RotationFunc(), camera,
				                       v.FrameFunc(), scale, lightSource);

			Game.Renderer.EnableDepthBuffer();
			Game.Renderer.EnableStencilBuffer();
			foreach (var v in draw)
				v.Voxel.DrawShadow(vr, shadowPalette.Index);
			Game.Renderer.DisableStencilBuffer();
			Game.Renderer.DisableDepthBuffer();

			Game.Renderer.EnableDepthBuffer();
			foreach (var v in draw)
				v.Voxel.Draw(vr, lightAmbientColor, lightDiffuseColor, palette.Index, normalsPalette.Index);
			Game.Renderer.DisableDepthBuffer();
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var draw = voxels.Where(v => v.DisableFunc == null || !v.DisableFunc());
			var scaleTransform = Util.ScaleMatrix(scale, scale, scale);
			var pxOrigin = wr.ScreenPosition(pos);

			// Correct for bogus light source definition
			var shadowTransform = Util.MakeFloatMatrix(new WRot(new WAngle(256) - lightSource.Pitch,
				WAngle.Zero, lightSource.Yaw + new WAngle(512)).AsMatrix());

			var invShadowTransform = Util.MatrixInverse(shadowTransform);
			var cameraTransform = Util.MakeFloatMatrix(camera.AsMatrix());

			// TODO: Generalize this once we support sloped terrain
			var groundNormal = new float[] {0,0,1,1};
			var groundPos = new float[] {0, 0, 0.5f*(wr.ScreenPosition(pos).Y - wr.ScreenZPosition(pos, 0)), 1};
			var shadowGroundNormal = Util.MatrixVectorMultiply(shadowTransform, groundNormal);
			var shadowGroundPos = Util.MatrixVectorMultiply(shadowTransform, groundPos);

			// Sprite rectangle
			var tl = new float2(float.MaxValue, float.MaxValue);
			var br = new float2(float.MinValue, float.MinValue);

			// Shadow sprite rectangle
			var stl = new float2(float.MaxValue, float.MaxValue);
			var sbr = new float2(float.MinValue, float.MinValue);

			foreach (var v in draw)
			{
				var bounds = v.Voxel.Bounds(v.FrameFunc());
				var worldTransform = v.RotationFunc().Reverse().Aggregate(scaleTransform,
					(x,y) => Util.MatrixMultiply(x, Util.MakeFloatMatrix(y.AsMatrix())));

				var worldBounds = Util.MatrixAABBMultiply(worldTransform, bounds);
				var screenBounds = Util.MatrixAABBMultiply(cameraTransform, worldBounds);

				// Aggregate bounds rect
				var pxOffset = wr.ScreenVector(v.OffsetFunc());
				var pxPos = pxOrigin + new float2(pxOffset[0], pxOffset[1]);
				tl = float2.Min(tl, pxPos + new float2(screenBounds[0], screenBounds[1]));
				br = float2.Max(br, pxPos + new float2(screenBounds[3], screenBounds[4]));

				// Box to render the shadow image from
				var shadowBounds = Util.MatrixAABBMultiply(shadowTransform, worldBounds);
				var shadowPxOffset = Util.MatrixVectorMultiply(shadowTransform, pxOffset);

				stl = float2.Min(stl, new float2(shadowPxOffset[0] + shadowBounds[0], shadowPxOffset[1] + shadowBounds[1]));
				sbr = float2.Max(sbr, new float2(shadowPxOffset[0] + shadowBounds[3], shadowPxOffset[1] + shadowBounds[4]));

				// Draw voxel bounding box
				var screenTransform = Util.MatrixMultiply(cameraTransform, worldTransform);
				DrawBoundsBox(pxPos, screenTransform, bounds, Color.Yellow);
			}

			// Inflate rects by 1px each side to ensure rendering is within bounds
			var pad = new float2(1,1);
			tl -= pad;
			br += pad;
			stl -= pad;
			sbr += pad;

			// Corners of the shadow quad, in shadow-space
			var corners = new float[][]
			{
				new float[] {stl.X, stl.Y, 0, 1},
				new float[] {sbr.X, sbr.Y, 0, 1},
				new float[] {sbr.X, stl.Y, 0, 1},
				new float[] {stl.X, sbr.Y, 0, 1}
			};

			var shadowScreenTransform = Util.MatrixMultiply(cameraTransform, invShadowTransform);
			var screenCorners = new float2[4];
			for (var j = 0; j < 4; j++)
			{
				// Project to ground plane
				corners[j][2] -= (corners[j][2] - shadowGroundPos[2]) +
						(corners[j][1] - shadowGroundPos[1])*shadowGroundNormal[1]/shadowGroundNormal[2] +
						(corners[j][0] - shadowGroundPos[0])*shadowGroundNormal[0]/shadowGroundNormal[2];

				// Rotate to camera-space
				corners[j] = Util.MatrixVectorMultiply(shadowScreenTransform, corners[j]);
				screenCorners[j] = pxOrigin + new float2(corners[j][0], corners[j][1]);
			}

			// Draw transformed shadow sprite rect
			var c = Color.Purple;
			Game.Renderer.WorldLineRenderer.DrawLine(screenCorners[1], screenCorners[3], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(screenCorners[3], screenCorners[0], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(screenCorners[0], screenCorners[2], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(screenCorners[2], screenCorners[1], c, c);

			// Draw sprite rect
			Game.Renderer.WorldLineRenderer.DrawRect(tl, br, Color.Red);
		}

		static void DrawBoundsBox(float2 pxPos, float[] transform, float[] bounds, Color c)
		{
			// Corner offsets
			var ix = new uint[] {0,0,0,0,3,3,3,3};
			var iy = new uint[] {1,1,4,4,1,1,4,4};
			var iz = new uint[] {2,5,2,5,2,5,2,5};

			var corners = new float2[8];
			for (var i = 0; i < 8; i++)
			{
				var vec = new float[] {bounds[ix[i]], bounds[iy[i]], bounds[iz[i]], 1};
				var screen = Util.MatrixVectorMultiply(transform, vec);
				corners[i] = pxPos + new float2(screen[0], screen[1]);
			}

			Game.Renderer.WorldLineRenderer.DrawLine(corners[0], corners[1], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[1], corners[3], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[3], corners[2], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[2], corners[0], c, c);

			Game.Renderer.WorldLineRenderer.DrawLine(corners[4], corners[5], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[5], corners[7], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[7], corners[6], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[6], corners[4], c, c);

			Game.Renderer.WorldLineRenderer.DrawLine(corners[0], corners[4], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[1], corners[5], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[2], corners[6], c, c);
			Game.Renderer.WorldLineRenderer.DrawLine(corners[3], corners[7], c, c);
		}
	}
}
