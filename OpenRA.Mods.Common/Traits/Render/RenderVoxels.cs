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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public interface IRenderActorPreviewVoxelsInfo : ITraitInfo
	{
		IEnumerable<VoxelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p);
	}

	public class RenderVoxelsInfo : ITraitInfo, IRenderActorPreviewInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("Custom palette name")]
		[PaletteReference] public readonly string Palette = null;

		[Desc("Custom PlayerColorPalette: BaseName")]
		[PaletteReference] public readonly string PlayerPalette = "player";
		[PaletteReference] public readonly string NormalsPalette = "normals";
		[PaletteReference] public readonly string ShadowPalette = "shadow";

		[Desc("Change the image size.")]
		public readonly float Scale = 10;

		public readonly WAngle LightPitch = WAngle.FromDegrees(50);
		public readonly WAngle LightYaw = WAngle.FromDegrees(240);
		public readonly float[] LightAmbientColor = { 0.6f, 0.6f, 0.6f };
		public readonly float[] LightDiffuseColor = { 0.4f, 0.4f, 0.4f };

		public virtual object Create(ActorInitializer init) { return new RenderVoxels(init.Self, this); }

		public virtual IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var faction = init.Get<FactionInit, string>();
			var ownerName = init.Get<OwnerInit>().PlayerName;
			var sequenceProvider = init.World.Map.Rules.Sequences;
			var image = Image ?? init.Actor.Name;
			var facings = body.QuantizedFacings == -1 ?
				init.Actor.TraitInfo<IQuantizeBodyOrientationInfo>().QuantizedBodyFacings(init.Actor, sequenceProvider, faction) :
				body.QuantizedFacings;
			var palette = init.WorldRenderer.Palette(Palette ?? PlayerPalette + ownerName);

			var components = init.Actor.TraitInfos<IRenderActorPreviewVoxelsInfo>()
				.SelectMany(rvpi => rvpi.RenderPreviewVoxels(init, this, image, init.GetOrientation(), facings, palette))
				.ToArray();

			yield return new VoxelPreview(components, WVec.Zero, 0, Scale, LightPitch,
				LightYaw, LightAmbientColor, LightDiffuseColor, body.CameraPitch,
				palette, init.WorldRenderer.Palette(NormalsPalette), init.WorldRenderer.Palette(ShadowPalette));
		}
	}

	public class RenderVoxels : IRender, INotifyOwnerChanged
	{
		readonly List<VoxelAnimation> components = new List<VoxelAnimation>();
		readonly Actor self;
		readonly RenderVoxelsInfo info;
		readonly BodyOrientation body;
		readonly WRot camera;
		readonly WRot lightSource;

		public RenderVoxels(Actor self, RenderVoxelsInfo info)
		{
			this.self = self;
			this.info = info;
			body = self.Trait<BodyOrientation>();
			camera = new WRot(WAngle.Zero, body.CameraPitch - new WAngle(256), new WAngle(256));
			lightSource = new WRot(WAngle.Zero, new WAngle(256) - info.LightPitch, info.LightYaw);
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
				shadowPalette = wr.Palette(info.ShadowPalette);
				initializePalettes = false;
			}

			return new IRenderable[] { new VoxelRenderable(
				components, self.CenterPosition, 0, camera, info.Scale,
				lightSource, info.LightAmbientColor, info.LightDiffuseColor,
				colorPalette, normalsPalette, shadowPalette) };
		}

		public string Image { get { return info.Image ?? self.Info.Name; } }
		public void Add(VoxelAnimation v) { components.Add(v); }
		public void Remove(VoxelAnimation v) { components.Remove(v); }
	}
}
