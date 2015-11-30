#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders turrets for units with the Turreted trait.")]
	public class WithTurretInfo : UpgradableTraitInfo, IRenderActorPreviewSpritesInfo,
		Requires<RenderSpritesInfo>, Requires<TurretedInfo>, Requires<BodyOrientationInfo>, Requires<ArmamentInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "turret";

		[Desc("Sequence name to use when prepared to fire")]
		[SequenceReference] public readonly string AimSequence = null;

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[Desc("Render recoil")]
		public readonly bool Recoils = true;

		public override object Create(ActorInitializer init) { return new WithTurret(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var ifacing = init.Actor.TraitInfoOrDefault<IFacingInfo>();
			var bodyFacing = ifacing != null ? init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : ifacing.GetInitialFacing() : 0;
			var turretFacing = Turreted.GetInitialTurretFacing(init, t.InitialFacing, Turret);

			var anim = new Animation(init.World, image, () => turretFacing);
			anim.Play(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(bodyFacing)), facings);
			var offset = body.LocalToWorld(t.Offset.Rotate(orientation));
			yield return new SpriteActorPreview(anim, offset, -(offset.Y + offset.Z) + 1, p, rs.Scale);
		}
	}

	public class WithTurret : UpgradableTrait<WithTurretInfo>, ITick, INotifyDamageStateChanged
	{
		public readonly Animation DefaultAnimation;
		protected readonly AttackBase Attack;
		readonly RenderSprites rs;
		readonly BodyOrientation body;
		readonly Turreted t;
		readonly Armament[] arms;

		public WithTurret(Actor self, WithTurretInfo info)
			: base(info)
		{
			rs = self.Trait<RenderSprites>();
			body = self.Trait<BodyOrientation>();
			Attack = self.TraitOrDefault<AttackBase>();
			t = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);
			arms = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == info.Turret).ToArray();

			DefaultAnimation = new Animation(self.World, rs.GetImage(self), () => t.TurretFacing);
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, info.Sequence));
			rs.Add(new AnimationWithOffset(
				DefaultAnimation, () => TurretOffset(self), () => IsTraitDisabled, () => false, p => ZOffsetFromCenter(self, p, 1)));

			// Restrict turret facings to match the sprite
			t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		WVec TurretOffset(Actor self)
		{
			if (!Info.Recoils)
				return t.Position(self);

			var recoil = arms.Aggregate(WDist.Zero, (a, b) => a + b.Recoil);
			var localOffset = new WVec(-recoil, WDist.Zero, WDist.Zero);
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			var turretOrientation = body.QuantizeOrientation(self, t.LocalOrientation(self));
			return t.Position(self) + body.LocalToWorld(localOffset.Rotate(turretOrientation).Rotate(bodyOrientation));
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
		}

		public virtual void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (DefaultAnimation.CurrentSequence != null)
				DefaultAnimation.ReplaceAnim(NormalizeSequence(self, DefaultAnimation.CurrentSequence.Name));
		}

		public virtual void Tick(Actor self)
		{
			if (Info.AimSequence == null)
				return;

			var sequence = Attack.IsAttacking ? Info.AimSequence : Info.Sequence;
			DefaultAnimation.ReplaceAnim(sequence);
		}

		public static int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
