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
	[Desc("Renders a parachute on units.")]
	public class WithParachuteInfo : ConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("The image that contains the parachute sequences.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Parachute opening sequence.")]
		public readonly string OpeningSequence = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Parachute idle sequence.")]
		public readonly string Sequence = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Parachute closing sequence. Defaults to opening sequence played backwards.")]
		public readonly string ClosingSequence = null;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Palette used to render the parachute.")]
		public readonly string Palette = "player";

		public readonly bool IsPlayerPalette = true;

		[Desc("Parachute position relative to the paradropped unit.")]
		public readonly WVec Offset = new WVec(0, 0, 384);

		[Desc("The image that contains the shadow sequence for the paradropped unit.")]
		public readonly string ShadowImage = null;

		[SequenceReference(nameof(ShadowImage), allowNullImage: true)]
		[Desc("Paradropped unit's shadow sequence.")]
		public readonly string ShadowSequence = null;

		[Desc("Color to render the paradropped unit's shadow.")]
		public readonly Color ShadowColor = Color.FromArgb(140, 0, 0, 0);

		[Desc("Shadow position relative to the paradropped unit's intended landing position.")]
		public readonly WVec ShadowOffset = new WVec(0, 128, 0);

		[Desc("Z-offset to apply on the shadow sequence.")]
		public readonly int ShadowZOffset = 0;

		public override object Create(ActorInitializer init) { return new WithParachute(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			if (image == null)
				yield break;

			// For this, image must not be null
			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			Func<WAngle> facing;
			var dynamicfacingInit = init.GetOrDefault<DynamicFacingInit>();
			if (dynamicfacingInit != null)
				facing = dynamicfacingInit.Value;
			else
			{
				var f = init.GetValue<FacingInit, WAngle>(WAngle.Zero);
				facing = () => f;
			}

			var anim = new Animation(init.World, image);
			anim.PlayThen(OpeningSequence, () => anim.PlayRepeating(Sequence));

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			Func<WRot> orientation = () => body.QuantizeOrientation(WRot.FromYaw(facing()), facings);
			Func<WVec> offset = () => body.LocalToWorld(Offset.Rotate(orientation()));
			Func<int> zOffset = () =>
			{
				var tmpOffset = offset();
				return tmpOffset.Y + tmpOffset.Z + 1;
			};

			yield return new SpriteActorPreview(anim, offset, zOffset, p);
		}
	}

	public class WithParachute : ConditionalTrait<WithParachuteInfo>, ITick, IRender
	{
		readonly Animation shadow;
		readonly AnimationWithOffset anim;
		readonly WithParachuteInfo info;
		readonly float3 shadowColor;
		readonly float shadowAlpha;

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
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => IsTraitDisabled && !renderProlonged,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			var rs = self.Trait<RenderSprites>();
			rs.Add(anim, info.Palette, info.IsPlayerPalette);

			shadowColor = new float3(info.ShadowColor.R, info.ShadowColor.G, info.ShadowColor.B) / 255f;
			shadowAlpha = info.ShadowColor.A / 255f;
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.Image == null)
				return;

			anim.Animation.PlayThen(info.OpeningSequence, () => anim.Animation.PlayRepeating(info.Sequence));
		}

		protected override void TraitDisabled(Actor self)
		{
			if (info.Image == null)
				return;

			renderProlonged = true;
			if (!string.IsNullOrEmpty(info.ClosingSequence))
				anim.Animation.PlayThen(info.ClosingSequence, () => renderProlonged = false);
			else
				anim.Animation.PlayBackwardsThen(info.OpeningSequence, () => renderProlonged = false);
		}

		void ITick.Tick(Actor self)
		{
			shadow?.Tick();
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (info.ShadowImage == null)
				return Enumerable.Empty<IRenderable>();

			if (IsTraitDisabled || self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<IRenderable>();

			if (self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			var dat = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			var pos = self.CenterPosition - new WVec(0, 0, dat.Length);
			var palette = wr.Palette(info.Palette);
			var alpha = shadow.CurrentSequence.GetAlpha(shadow.CurrentFrame);
			var tintModifiers = shadow.CurrentSequence.IgnoreWorldTint ? TintModifiers.ReplaceColor | TintModifiers.IgnoreWorldTint : TintModifiers.ReplaceColor;
			return new IRenderable[] { new SpriteRenderable(shadow.Image, pos, info.ShadowOffset, info.ShadowZOffset, palette, 1, shadowAlpha * alpha, shadowColor, tintModifiers, true) };
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			if (info.ShadowImage == null)
				return Enumerable.Empty<Rectangle>();

			if (IsTraitDisabled || self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<Rectangle>();

			if (self.World.FogObscures(self))
				return Enumerable.Empty<Rectangle>();

			var dat = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			var pos = self.CenterPosition - new WVec(0, 0, dat.Length);
			return new Rectangle[] { shadow.ScreenBounds(wr, pos, info.ShadowOffset) };
		}
	}
}
