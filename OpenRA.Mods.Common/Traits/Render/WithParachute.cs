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
	[Desc("Renders a parachute on units.")]
	public class WithParachuteInfo : UpgradableTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("The image that contains the parachute sequences.")]
		public readonly string Image = null;

		[Desc("Parachute opening sequence.")]
		[SequenceReference("Image")] public readonly string OpeningSequence = null;

		[Desc("Parachute idle sequence.")]
		[SequenceReference("Image")] public readonly string Sequence = null;

		[Desc("Parachute closing sequence. Defaults to opening sequence played backwards.")]
		[SequenceReference("Image")] public readonly string ClosingSequence = null;

		[Desc("Palette used to render the parachute.")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "player";
		public readonly bool IsPlayerPalette = true;

		[Desc("Parachute position relative to the paradropped unit.")]
		public readonly WVec Offset = new WVec(0, 0, 384);

		[Desc("The image that contains the shadow sequence for the paradropped unit.")]
		public readonly string ShadowImage = null;

		[Desc("Paradropped unit's shadow sequence.")]
		[SequenceReference("ShadowImage")] public readonly string ShadowSequence = null;

		[Desc("Palette used to render the paradropped unit's shadow.")]
		[PaletteReference(false)] public readonly string ShadowPalette = "shadow";

		[Desc("Shadow position relative to the paradropped unit's intended landing position.")]
		public readonly WVec ShadowOffset = new WVec(0, 128, 0);

		[Desc("Z-offset to apply on the shadow sequence.")]
		public readonly int ShadowZOffset = 0;

		public override object Create(ActorInitializer init) { return new WithParachute(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			if (image == null)
				yield break;

			// For this, image must not be null
			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			Func<int> facing;
			if (init.Contains<DynamicFacingInit>())
				facing = init.Get<DynamicFacingInit, Func<int>>();
			else
			{
				var f = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
				facing = () => f;
			}

			var anim = new Animation(init.World, image);
			anim.PlayThen(OpeningSequence, () => anim.PlayRepeating(Sequence));

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			Func<WRot> orientation = () => body.QuantizeOrientation(WRot.FromFacing(facing()), facings);
			Func<WVec> offset = () => body.LocalToWorld(Offset.Rotate(orientation()));
			Func<int> zOffset = () =>
			{
				var tmpOffset = offset();
				return tmpOffset.Y + tmpOffset.Z + 1;
			};

			yield return new SpriteActorPreview(anim, offset, zOffset, p, rs.Scale);
		}
	}

	public class WithParachute : UpgradableTrait<WithParachuteInfo>, ITick, IRender
	{
		readonly Animation shadow;
		readonly AnimationWithOffset anim;
		readonly WithParachuteInfo info;

		bool renderProlonged = false;

		public WithParachute(Actor self, WithParachuteInfo info)
			: base(info)
		{
			this.info = info;

			if (info.ShadowImage != null)
			{
				shadow = new Animation(self.World, info.ShadowImage);
				shadow.PlayRepeating(info.ShadowSequence);
			}

			if (info.Image == null)
				return;

			// For this, info.Image must not be null
			var overlay = new Animation(self.World, info.Image);
			var body = self.Trait<BodyOrientation>();
			anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled && !renderProlonged,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			var rs = self.Trait<RenderSprites>();
			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		protected override void UpgradeEnabled(Actor self)
		{
			if (info.Image == null)
				return;

			anim.Animation.PlayThen(info.OpeningSequence, () => anim.Animation.PlayRepeating(info.Sequence));
		}

		protected override void UpgradeDisabled(Actor self)
		{
			if (info.Image == null)
				return;

			renderProlonged = true;
			if (!string.IsNullOrEmpty(info.ClosingSequence))
				anim.Animation.PlayThen(info.ClosingSequence, () => renderProlonged = false);
			else
				anim.Animation.PlayBackwardsThen(info.OpeningSequence, () => renderProlonged = false);
		}

		public void Tick(Actor self)
		{
			if (shadow != null)
				shadow.Tick();
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (info.ShadowImage == null)
				return Enumerable.Empty<IRenderable>();

			if (IsTraitDisabled)
				return Enumerable.Empty<IRenderable>();

			if (self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<IRenderable>();

			if (self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			var dat = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			var pos = self.CenterPosition - new WVec(0, 0, dat.Length);
			var palette = wr.Palette(info.ShadowPalette);
			return new IRenderable[] { new SpriteRenderable(shadow.Image, pos, info.ShadowOffset, info.ShadowZOffset, palette, 1, true) };
		}
	}
}
