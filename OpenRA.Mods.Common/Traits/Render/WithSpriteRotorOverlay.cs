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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays a helicopter rotor overlay.")]
	public class WithSpriteRotorOverlayInfo : ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use when flying")]
		[SequenceReference] public readonly string Sequence = "rotor";

		[Desc("Sequence name to use when landed")]
		[SequenceReference] public readonly string GroundSequence = "slow-rotor";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public object Create(ActorInitializer init) { return new WithSpriteRotorOverlay(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing)), facings);
			var offset = body.LocalToWorld(Offset.Rotate(orientation));
			yield return new SpriteActorPreview(anim, offset, offset.Y + offset.Z + 1, p, rs.Scale);
		}
	}

	public class WithSpriteRotorOverlay : ITick
	{
		readonly WithSpriteRotorOverlayInfo info;
		readonly Animation rotorAnim;
		readonly IMove movement;

		public WithSpriteRotorOverlay(Actor self, WithSpriteRotorOverlayInfo info)
		{
			this.info = info;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();
			movement = self.Trait<IMove>();

			rotorAnim = new Animation(self.World, rs.GetImage(self));
			rotorAnim.PlayRepeating(info.Sequence);
			rs.Add(new AnimationWithOffset(rotorAnim,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				null, p => ZOffsetFromCenter(self, p, 1)));
		}

		public void Tick(Actor self)
		{
			var isFlying = movement.IsMoving && !self.IsDead;
			if (isFlying ^ (rotorAnim.CurrentSequence.Name != info.Sequence))
				return;

			rotorAnim.ReplaceAnim(isFlying ? info.Sequence : info.GroundSequence);
		}

		public static int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
