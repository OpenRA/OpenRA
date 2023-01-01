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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class ModelPreview : IActorPreview
	{
		readonly ModelAnimation[] components;
		readonly float scale;
		readonly float[] lightAmbientColor;
		readonly float[] lightDiffuseColor;
		readonly WRot lightSource;
		readonly WRot camera;
		readonly PaletteReference colorPalette;
		readonly PaletteReference normalsPalette;
		readonly PaletteReference shadowPalette;
		readonly WVec offset;
		readonly int zOffset;

		public ModelPreview(ModelAnimation[] components, in WVec offset, int zOffset, float scale, WAngle lightPitch, WAngle lightYaw,
			float[] lightAmbientColor, float[] lightDiffuseColor, WAngle cameraPitch,
			PaletteReference colorPalette, PaletteReference normalsPalette, PaletteReference shadowPalette)
		{
			this.components = components;
			this.scale = scale;
			this.lightAmbientColor = lightAmbientColor;
			this.lightDiffuseColor = lightDiffuseColor;

			lightSource = new WRot(WAngle.Zero, new WAngle(256) - lightPitch, lightYaw);
			camera = new WRot(WAngle.Zero, cameraPitch - new WAngle(256), new WAngle(256));

			this.colorPalette = colorPalette;
			this.normalsPalette = normalsPalette;
			this.shadowPalette = shadowPalette;

			this.offset = offset;
			this.zOffset = zOffset;
		}

		void IActorPreview.Tick() { /* not supported */ }

		IEnumerable<IRenderable> IActorPreview.RenderUI(WorldRenderer wr, int2 pos, float scale)
		{
			yield return new UIModelRenderable(components, WPos.Zero + offset, pos, zOffset, camera, scale * this.scale,
				lightSource, lightAmbientColor, lightDiffuseColor,
				colorPalette, normalsPalette, shadowPalette);
		}

		IEnumerable<IRenderable> IActorPreview.Render(WorldRenderer wr, WPos pos)
		{
			yield return new ModelRenderable(components, pos + offset, zOffset, camera, scale,
				lightSource, lightAmbientColor, lightDiffuseColor,
				colorPalette, normalsPalette, shadowPalette);
		}

		IEnumerable<Rectangle> IActorPreview.ScreenBounds(WorldRenderer wr, WPos pos)
		{
			foreach (var c in components)
				yield return c.ScreenBounds(pos, wr, scale);
		}
	}
}
