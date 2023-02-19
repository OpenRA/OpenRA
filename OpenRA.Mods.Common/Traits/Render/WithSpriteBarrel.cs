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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders barrels for units with the Turreted trait.")]
	public class WithSpriteBarrelInfo : ConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<TurretedInfo>,
		Requires<ArmamentInfo>, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use.")]
		public readonly string Sequence = "barrel";

		[Desc("Armament to use for recoil.")]
		public readonly string Armament = "primary";

		[Desc("Visual offset.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new WithSpriteBarrel(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var armament = init.Actor.TraitInfos<ArmamentInfo>()
				.First(a => a.Name == Armament);
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == armament.Turret);

			var turretFacing = t.WorldFacingFromInit(init);
			var anim = new Animation(init.World, image, turretFacing);
			anim.Play(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var facing = init.GetFacing();
			WRot Orientation() => body.QuantizeOrientation(WRot.FromYaw(facing()), facings);
			WVec TurretOffset() => body.LocalToWorld(t.Offset.Rotate(Orientation()));
			int ZOffset()
			{
				var tmpOffset = TurretOffset();
				return -(tmpOffset.Y + tmpOffset.Z) + 1;
			}

			yield return new SpriteActorPreview(anim, TurretOffset, ZOffset, p);
		}
	}

	public class WithSpriteBarrel : ConditionalTrait<WithSpriteBarrelInfo>
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
			DefaultAnimation = new Animation(self.World, rs.GetImage(self), () => turreted.WorldOrientation.Yaw);
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
			var orientation = turreted != null ? turreted.WorldOrientation : self.Orientation;
			var localOffset = Info.LocalOffset + new WVec(-armament.Recoil, WDist.Zero, WDist.Zero);
			var turretLocalOffset = turreted != null ? turreted.Offset : WVec.Zero;
			return body.LocalToWorld(turretLocalOffset + localOffset.Rotate(orientation));
		}
	}
}
