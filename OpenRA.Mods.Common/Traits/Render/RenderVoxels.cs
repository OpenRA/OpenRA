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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public interface IRenderActorPreviewVoxelsInfo : ITraitInfoInterface
	{
		IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p);
	}

	public class RenderVoxelsInfo : TraitInfo, IRenderActorPreviewInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("Custom palette name")]
		[PaletteReference]
		public readonly string Palette = null;

		[PaletteReference]
		[Desc("Custom PlayerColorPalette: BaseName")]
		public readonly string PlayerPalette = "player";

		[PaletteReference]
		public readonly string NormalsPalette = "normals";

		[PaletteReference]
		public readonly string ShadowPalette = "shadow";

		[Desc("Change the image size.")]
		public readonly float Scale = 12;

		public readonly WAngle LightPitch = WAngle.FromDegrees(50);
		public readonly WAngle LightYaw = WAngle.FromDegrees(240);
		public readonly float[] LightAmbientColor = { 0.6f, 0.6f, 0.6f };
		public readonly float[] LightDiffuseColor = { 0.4f, 0.4f, 0.4f };

		public override object Create(ActorInitializer init) { return new RenderVoxels(init.Self, this); }

		public virtual IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var faction = init.GetValue<FactionInit, string>(this);
			var ownerName = init.Get<OwnerInit>().InternalName;
			var sequences = init.World.Map.Sequences;
			var image = Image ?? init.Actor.Name;
			var facings = body.QuantizedFacings == -1 ?
				init.Actor.TraitInfo<IQuantizeBodyOrientationInfo>().QuantizedBodyFacings(init.Actor, sequences, faction) :
				body.QuantizedFacings;
			var palette = init.WorldRenderer.Palette(Palette ?? PlayerPalette + ownerName);

			var components = init.Actor.TraitInfos<IRenderActorPreviewVoxelsInfo>()
				.SelectMany(rvpi => rvpi.RenderPreviewVoxels(init, this, image, init.GetOrientation(), facings, palette))
				.ToArray();

			yield return new ModelPreview(components, WVec.Zero, 0, Scale, LightPitch,
				LightYaw, LightAmbientColor, LightDiffuseColor, body.CameraPitch,
				palette, init.WorldRenderer.Palette(NormalsPalette), init.WorldRenderer.Palette(ShadowPalette));
		}
	}

	public class RenderVoxels : IRender, ITick, INotifyOwnerChanged
	{
		sealed class AnimationWrapper
		{
			readonly ModelAnimation model;
			bool cachedVisible;
			WVec cachedOffset;

			public AnimationWrapper(ModelAnimation model)
			{
				this.model = model;
			}

			public bool Tick()
			{
				// Return to the caller whether the renderable position or size has changed
				var visible = model.IsVisible;
				var offset = model.OffsetFunc?.Invoke() ?? WVec.Zero;

				var updated = visible != cachedVisible || offset != cachedOffset;
				cachedVisible = visible;
				cachedOffset = offset;

				return updated;
			}
		}

		public readonly RenderVoxelsInfo Info;

		readonly List<ModelAnimation> components = new();
		readonly Dictionary<ModelAnimation, AnimationWrapper> wrappers = new();

		readonly Actor self;
		readonly BodyOrientation body;
		readonly WRot camera;
		readonly WRot lightSource;

		public RenderVoxels(Actor self, RenderVoxelsInfo info)
		{
			this.self = self;
			Info = info;
			body = self.Trait<BodyOrientation>();
			camera = new WRot(WAngle.Zero, body.CameraPitch - new WAngle(256), new WAngle(256));
			lightSource = new WRot(WAngle.Zero, new WAngle(256) - info.LightPitch, info.LightYaw);
		}

		bool initializePalettes = true;
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { initializePalettes = true; }

		void ITick.Tick(Actor self)
		{
			var updated = false;
			foreach (var w in wrappers.Values)
				updated |= w.Tick();

			if (updated)
				self.World.ScreenMap.AddOrUpdate(self);
		}

		protected PaletteReference colorPalette, normalsPalette, shadowPalette;
		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (initializePalettes)
			{
				var paletteName = Info.Palette ?? Info.PlayerPalette + self.Owner.InternalName;
				colorPalette = wr.Palette(paletteName);
				normalsPalette = wr.Palette(Info.NormalsPalette);
				shadowPalette = wr.Palette(Info.ShadowPalette);
				initializePalettes = false;
			}

			return new IRenderable[]
			{
				new ModelRenderable(
					components, self.CenterPosition, 0, camera, Info.Scale,
					lightSource, Info.LightAmbientColor, Info.LightDiffuseColor,
					colorPalette, normalsPalette, shadowPalette)
			};
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			var pos = self.CenterPosition;
			foreach (var c in components)
				if (c.IsVisible)
					yield return c.ScreenBounds(pos, wr, Info.Scale);
		}

		public string Image => Info.Image ?? self.Info.Name;

		public void Add(ModelAnimation m)
		{
			components.Add(m);
			wrappers.Add(m, new AnimationWrapper(m));
		}

		public void Remove(ModelAnimation m)
		{
			components.Remove(m);
			wrappers.Remove(m);
		}
	}
}
