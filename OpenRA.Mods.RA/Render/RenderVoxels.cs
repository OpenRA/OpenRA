#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Graphics;

namespace OpenRA.Mods.RA.Render
{
	public class RenderVoxelsInfo : ITraitInfo, Requires<IBodyOrientationInfo>
	{
		[Desc("Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom PlayerColorPalette: BaseName")]
		public readonly string PlayerPalette = "player";
		public readonly string NormalsPalette = "normals";

		[Desc("Change the image size.")]
		public readonly float Scale = 10;

		public readonly WAngle LightPitch = WAngle.FromDegrees(50);
		public readonly WAngle LightYaw = WAngle.FromDegrees(240);
		public readonly float[] LightAmbientColor = new float[] {0.6f, 0.6f, 0.6f};
		public readonly float[] LightDiffuseColor = new float[] {0.4f, 0.4f, 0.4f};
		public virtual object Create(ActorInitializer init) { return new RenderVoxels(init.self, this); }
	}

	public class RenderVoxels : IRender, INotifyOwnerChanged
	{
		Actor self;
		RenderVoxelsInfo info;
		List<VoxelAnimation> components = new List<VoxelAnimation>();
		IBodyOrientation body;
		WRot camera;
		WRot lightSource;

		public RenderVoxels(Actor self, RenderVoxelsInfo info)
		{
			this.self = self;
			this.info = info;
			body = self.Trait<IBodyOrientation>();
			camera = new WRot(WAngle.Zero, body.CameraPitch - new WAngle(256), new WAngle(256));
			lightSource = new WRot(WAngle.Zero,new WAngle(256) - info.LightPitch, info.LightYaw);
		}

		bool initializePalettes = true;
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { initializePalettes = true; }

		protected PaletteReference colorPalette, normalsPalette, shadowPalette;
		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (initializePalettes)
			{
				var paletteName = info.Palette ?? info.PlayerPalette + self.Owner.InternalName;
				colorPalette = wr.Palette(paletteName);
				normalsPalette = wr.Palette(info.NormalsPalette);
				shadowPalette = wr.Palette("shadow");
				initializePalettes = false;
			}

			yield return new VoxelRenderable(components, self.CenterPosition, 0, camera, info.Scale,
			                                 lightSource, info.LightAmbientColor, info.LightDiffuseColor,
			                                 colorPalette, normalsPalette, shadowPalette);
		}

		public string Image { get { return info.Image ?? self.Info.Name; } }
		public void Add(VoxelAnimation v) { components.Add(v); }
		public void Remove(VoxelAnimation v) { components.Remove(v); }
	}
}
