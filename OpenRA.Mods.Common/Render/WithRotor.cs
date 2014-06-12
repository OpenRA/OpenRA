#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Render
{
	public class WithRotorInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use when flying")]
		public readonly string Sequence = "rotor";

		[Desc("Sequence name to use when landed")]
		public readonly string GroundSequence = "slow-rotor";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public readonly string Id = "rotor";

		public object Create(ActorInitializer init) { return new WithRotor(init.self, this); }
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
			var isFlying = movement.IsMoving && !self.IsDead();
			if (isFlying ^ (rotorAnim.CurrentSequence.Name != info.Sequence))
				return;

			rotorAnim.ReplaceAnim(isFlying ? info.Sequence : info.GroundSequence);
		}
	}
}
