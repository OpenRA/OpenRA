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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class ThrowsParticleInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<BodyOrientationInfo>
	{
		[FieldLoader.Require]
		public readonly string Anim = null;

		[Desc("Initial position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Minimum distance to throw the particle")]
		public readonly WDist MinThrowRange = new WDist(256);

		[Desc("Maximum distance to throw the particle")]
		public readonly WDist MaxThrowRange = new WDist(768);

		[Desc("Minimum angle to throw the particle")]
		public readonly WAngle MinThrowAngle = WAngle.FromDegrees(30);

		[Desc("Maximum angle to throw the particle")]
		public readonly WAngle MaxThrowAngle = WAngle.FromDegrees(60);

		[Desc("Speed to throw the particle (horizontal WPos/tick)")]
		public readonly int Velocity = 75;

		[Desc("Speed at which the particle turns.")]
		public readonly int TurnSpeed = 15;

		public object Create(ActorInitializer init) { return new ThrowsParticle(init, this); }
	}

	class ThrowsParticle : ITick
	{
		WVec pos;
		WVec initialPos;
		WVec finalPos;
		WAngle angle;

		int tick = 0;
		int length;

		WAngle facing;
		WAngle rotation;

		public ThrowsParticle(ActorInitializer init, ThrowsParticleInfo info)
		{
			var self = init.Self;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			// TODO: Carry orientation over from the parent instead of just facing
			var bodyFacing = init.Contains<DynamicFacingInit>() ? init.Get<DynamicFacingInit, Func<int>>()()
				: init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
			facing = WAngle.FromFacing(Turreted.TurretFacingFromInit(init, 0)());

			// Calculate final position
			var throwRotation = WRot.FromFacing(Game.CosmeticRandom.Next(1024));
			var throwDistance = Game.CosmeticRandom.Next(info.MinThrowRange.Length, info.MaxThrowRange.Length);

			initialPos = pos = info.Offset.Rotate(body.QuantizeOrientation(self, WRot.FromFacing(bodyFacing)));
			finalPos = initialPos + new WVec(throwDistance, 0, 0).Rotate(throwRotation);
			angle = new WAngle(Game.CosmeticRandom.Next(info.MinThrowAngle.Angle, info.MaxThrowAngle.Angle));
			length = (finalPos - initialPos).Length / info.Velocity;

			// Facing rotation
			rotation = WAngle.FromFacing(WDist.FromPDF(Game.CosmeticRandom, 2).Length * info.TurnSpeed / 1024);

			var anim = new Animation(init.World, rs.GetImage(self), () => facing.Angle / 4);
			anim.PlayRepeating(info.Anim);
			rs.Add(new AnimationWithOffset(anim, () => pos, null));
		}

		public void Tick(Actor self)
		{
			if (tick >= length)
				return;

			pos = WVec.LerpQuadratic(initialPos, finalPos, angle, tick++, length);

			// Spin the particle
			facing += rotation;
			rotation = new WAngle(rotation.Angle * 90 / 100);
		}
	}
}
