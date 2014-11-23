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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Displays a helicopter rotor overlay.")]
	public class WithRotorInfo : ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use when flying")]
		public readonly string Sequence = "rotor";

		[Desc("Sequence name to use when landed")]
		public readonly string GroundSequence = "slow-rotor";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Change this when using this trait multiple times on the same actor.")]
		public readonly string Id = "rotor";

		public object Create(ActorInitializer init) { return new WithRotor(init.self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating(Sequence);

			var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing)), facings);
			var offset = body.LocalToWorld(Offset.Rotate(orientation));
			yield return new SpriteActorPreview(anim, offset, offset.Y + offset.Z + 1, p, rs.Scale);
		}
	}

	public class WithRotor : ITick
	{
		WithRotorInfo info;
		Animation rotorAnim;
		IMove movement;

		public WithRotor(Actor self, WithRotorInfo info)
		{
			this.info = info;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();
			movement = self.Trait<IMove>();

			rotorAnim = new Animation(self.World, rs.GetImage(self));
			rotorAnim.PlayRepeating(info.Sequence);
			rs.Add(info.Id, new AnimationWithOffset(rotorAnim,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				null, () => false, p => WithTurret.ZOffsetFromCenter(self, p, 1)));
		}

		public void Tick(Actor self)
		{
			var isFlying = movement.IsMoving && !self.IsDead;
			if (isFlying ^ (rotorAnim.CurrentSequence.Name != info.Sequence))
				return;

			rotorAnim.ReplaceAnim(isFlying ? info.Sequence : info.GroundSequence);
		}
	}
}
