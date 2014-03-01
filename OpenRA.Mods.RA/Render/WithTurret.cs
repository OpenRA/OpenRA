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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class WithTurretInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<TurretedInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "turret";

		[Desc("Sequence name to use when prepared to fire")]
		public readonly string AimSequence = null;

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[Desc("Render recoil")]
		public readonly bool Recoils = true;

		public object Create(ActorInitializer init) { return new WithTurret(init.self, this); }
	}

	class WithTurret : ITick
	{
		WithTurretInfo info;
		RenderSprites rs;
		IBodyOrientation body;
		AttackBase ab;
		Turreted t;
		IEnumerable<Armament> arms;
		Animation anim;

		public WithTurret(Actor self, WithTurretInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			body = self.Trait<IBodyOrientation>();

			ab = self.TraitOrDefault<AttackBase>();
			t = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);
			arms = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == info.Turret);

			anim = new Animation(rs.GetImage(self), () => t.turretFacing);
			anim.Play(info.Sequence);
			rs.anims.Add("turret_{0}".F(info.Turret), new AnimationWithOffset(
				anim, () => TurretOffset(self), null, p => ZOffsetFromCenter(self, p, 1)));

			// Restrict turret facings to match the sprite
			t.QuantizedFacings = anim.CurrentSequence.Facings;
		}

		WVec TurretOffset(Actor self)
		{
			if (!info.Recoils)
				return t.Position(self);

			var recoil = arms.Aggregate(WDist.Zero, (a,b) => a + b.Recoil);
			var localOffset = new WVec(-recoil, WDist.Zero, WDist.Zero);
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			var turretOrientation = body.QuantizeOrientation(self, t.LocalOrientation(self));
			return t.Position(self) + body.LocalToWorld(localOffset.Rotate(turretOrientation).Rotate(bodyOrientation));
		}

		public void Tick(Actor self)
		{
			if (info.AimSequence == null)
				return;

			var sequence = ab.IsAttacking ? info.AimSequence : info.Sequence;
			rs.anims["turret_{0}".F(info.Turret)].Animation.ReplaceAnim(sequence);
		}

		static public int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
