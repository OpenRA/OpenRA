#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Renders barrels for units with the Turreted trait.")]
	class WithTurretInfo : ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<TurretedInfo>, Requires<IBodyOrientationInfo>
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

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var t = init.Actor.Traits.WithInterface<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var anim = new Animation(init.World, image, () => t.InitialFacing);
			anim.Play(Sequence);

			var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(t.InitialFacing)), facings);
			var offset = body.LocalToWorld(t.Offset.Rotate(orientation));
			yield return new SpriteActorPreview(anim, offset, offset.Y + offset.Z + 1, p, rs.Scale);
		}
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

			anim = new Animation(self.World, rs.GetImage(self), () => t.turretFacing);
			anim.Play(info.Sequence);
			rs.Add("turret_{0}".F(info.Turret), new AnimationWithOffset(
				anim, () => TurretOffset(self), null, () => false, p => ZOffsetFromCenter(self, p, 1)));

			// Restrict turret facings to match the sprite
			t.QuantizedFacings = anim.CurrentSequence.Facings;
		}

		WVec TurretOffset(Actor self)
		{
			if (!info.Recoils)
				return t.Position(self);

			var recoil = arms.Aggregate(WRange.Zero, (a,b) => a + b.Recoil);
			var localOffset = new WVec(-recoil, WRange.Zero, WRange.Zero);
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			var turretOrientation = body.QuantizeOrientation(self, t.LocalOrientation(self));
			return t.Position(self) + body.LocalToWorld(localOffset.Rotate(turretOrientation).Rotate(bodyOrientation));
		}

		public void Tick(Actor self)
		{
			if (info.AimSequence == null)
				return;

			var sequence = ab.IsAttacking ? info.AimSequence : info.Sequence;
			anim.ReplaceAnim(sequence);
		}

		static public int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
