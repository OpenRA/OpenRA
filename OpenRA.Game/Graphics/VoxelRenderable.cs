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
	}
}
