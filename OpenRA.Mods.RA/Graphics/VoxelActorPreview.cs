#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA.Graphics
{
	public class VoxelPreview : IActorPreview
	{
		readonly VoxelAnimation[] components;
		readonly RenderVoxelsInfo rvi;
		readonly WRot lightSource;
		readonly WRot camera;

		readonly PaletteReference colorPalette;
		readonly PaletteReference normalsPalette;
		readonly PaletteReference shadowPalette;

		readonly WVec offset;
		readonly int zOffset;

		public VoxelPreview(VoxelAnimation[] components, WVec offset, int zOffset, RenderVoxelsInfo rvi, WAngle cameraPitch,
			PaletteReference colorPalette, PaletteReference normalsPalette, PaletteReference shadowPalette)
		{
			this.components = components;
			this.rvi = rvi;
			lightSource = new WRot(WAngle.Zero,new WAngle(256) - rvi.LightPitch, rvi.LightYaw);
			camera = new WRot(WAngle.Zero, cameraPitch - new WAngle(256), new WAngle(256));

			this.colorPalette = colorPalette;
			this.normalsPalette = normalsPalette;
			this.shadowPalette = shadowPalette;

			this.offset = offset;
			this.zOffset = zOffset;
		}

		public void Tick() { /* not supported */ }

		public IEnumerable<IRenderable> Render(WorldRenderer wr, WPos pos)
		{
			yield return new VoxelRenderable(components, pos + offset, zOffset, camera, rvi.Scale,
				lightSource, rvi.LightAmbientColor, rvi.LightDiffuseColor,
				colorPalette, normalsPalette, shadowPalette);
		}
	}
}
