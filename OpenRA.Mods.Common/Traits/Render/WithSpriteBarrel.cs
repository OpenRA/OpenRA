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
	[Desc("Renders barrels for units with the Turreted trait.")]
	public class WithSpriteBarrelInfo : UpgradableTraitInfo, IRenderActorPreviewSpritesInfo, Requires<TurretedInfo>,
		Requires<ArmamentInfo>, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use.")]
		[SequenceReference] public readonly string Sequence = "barrel";

		[Desc("Armament to use for recoil.")]
		public readonly string Armament = "primary";

		[Desc("Visual offset.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new WithSpriteBarrel(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var armament = init.Actor.TraitInfos<ArmamentInfo>()
				.First(a => a.Name == Armament);
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == armament.Turret);

			var anim = new Animation(init.World, image, () => t.InitialFacing);
			anim.Play(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var turretOrientation = body.QuantizeOrientation(WRot.FromFacing(t.InitialFacing), facings);
			Func<WVec> turretOffset = () => body.LocalToWorld(t.Offset.Rotate(turretOrientation));
			Func<int> zOffset = () =>
			{
				var tmpOffset = turretOffset();
				return tmpOffset.Y + tmpOffset.Z;
			};

			yield return new SpriteActorPreview(anim, turretOffset, zOffset, p, rs.Scale);
		}
	}

	public class WithSpriteBarrel : UpgradableTrait<WithSpriteBarrelInfo>
	{
		public readonly Animation DefaultAnimation;
		readonly RenderSprites rs;
		readonly Actor self;
		readonly Armament armament;
		readonly Turreted turreted;
		readonly BodyOrientation body;

		public WithSpriteBarrel(Actor self, WithSpriteBarrelInfo info)
			: base(info)
		{
			this.self = self;
			body = self.Trait<BodyOrientation>();
			armament = self.TraitsImplementing<Armament>()
				.First(a => a.Info.Name == Info.Armament);
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == armament.Info.Turret);

			rs = self.Trait<RenderSprites>();
			DefaultAnimation = new Animation(self.World, rs.GetImage(self), () => turreted.TurretFacing);
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
			rs.Add(new AnimationWithOffset(
				DefaultAnimation, () => BarrelOffset(), () => IsTraitDisabled, p => RenderUtils.ZOffsetFromCenter(self, p, 0)));

			// Restrict turret facings to match the sprite
			turreted.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
		}

		WVec BarrelOffset()
		{
			var localOffset = Info.LocalOffset + new WVec(-armament.Recoil, WDist.Zero, WDist.Zero);
			var turretOffset = turreted != null ? turreted.Position(self) : WVec.Zero;
			var quantizedBody = body.QuantizeOrientation(self, self.Orientation);
			var turretOrientation = turreted != null ? turreted.WorldOrientation(self) - quantizedBody : WRot.Zero;

			var quantizedTurret = body.QuantizeOrientation(self, turretOrientation);
			return turretOffset + body.LocalToWorld(localOffset.Rotate(quantizedTurret).Rotate(quantizedBody));
		}

		IEnumerable<WRot> BarrelRotation()
		{
			var b = self.Orientation;
			var qb = body.QuantizeOrientation(self, b);
			yield return turreted.WorldOrientation(self) - qb + WRot.FromYaw(b.Yaw - qb.Yaw);
			yield return qb;
		}
	}
}
