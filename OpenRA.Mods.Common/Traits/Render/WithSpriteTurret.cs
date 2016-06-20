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
	[Desc("Renders turrets for units with the Turreted trait.")]
	public class WithSpriteTurretInfo : UpgradableTraitInfo, IRenderActorPreviewSpritesInfo,
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

		public override object Create(ActorInitializer init) { return new WithSpriteTurret(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var turretFacing = Turreted.TurretFacingFromInit(init, t.InitialFacing, Turret);
			var anim = new Animation(init.World, image, turretFacing);
			anim.Play(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			Func<int> facing = init.GetFacing();
			Func<WRot> orientation = () => body.QuantizeOrientation(WRot.FromFacing(facing()), facings);
			Func<WVec> offset = () => body.LocalToWorld(t.Offset.Rotate(orientation()));
			Func<int> zOffset = () =>
			{
				var tmpOffset = offset();
				return -(tmpOffset.Y + tmpOffset.Z) + 1;
			};

			yield return new SpriteActorPreview(anim, offset, zOffset, p, rs.Scale);
		}
	}

	public class WithSpriteTurret : UpgradableTrait<WithSpriteTurretInfo>, INotifyBuildComplete, INotifySold, INotifyTransform, ITick, INotifyDamageStateChanged
	{
		public readonly Animation DefaultAnimation;
		protected readonly AttackBase Attack;
		readonly RenderSprites rs;
		readonly BodyOrientation body;
		readonly Turreted t;
		readonly Armament[] arms;

		// TODO: This should go away once https://github.com/OpenRA/OpenRA/issues/7035 is implemented
		bool buildComplete;

		public WithSpriteTurret(Actor self, WithSpriteTurretInfo info)
			: base(info)
		{
			rs = self.Trait<RenderSprites>();
			body = self.Trait<BodyOrientation>();
			Attack = self.TraitOrDefault<AttackBase>();
			t = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);
			arms = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == info.Turret).ToArray();
			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units

			DefaultAnimation = new Animation(self.World, rs.GetImage(self), () => t.TurretFacing);
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, info.Sequence));
			rs.Add(new AnimationWithOffset(DefaultAnimation,
				() => TurretOffset(self),
				() => IsTraitDisabled || !buildComplete,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1)));

			// Restrict turret facings to match the sprite
			t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		WVec TurretOffset(Actor self)
		{
			if (!Info.Recoils)
				return t.Position(self);

			var recoil = arms.Aggregate(WDist.Zero, (a, b) => a + b.Recoil);
			var localOffset = new WVec(-recoil, WDist.Zero, WDist.Zero);
			var quantizedWorldTurret = t.WorldOrientation(self);
			return t.Position(self) + body.LocalToWorld(localOffset.Rotate(quantizedWorldTurret));
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

		void INotifyBuildComplete.BuildingComplete(Actor self) { buildComplete = true; }
		void INotifySold.Selling(Actor self) { buildComplete = false; }
		void INotifySold.Sold(Actor self) { }
		void INotifyTransform.BeforeTransform(Actor self) { buildComplete = false; }
		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor toActor) { }
	}
}
