#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class WithSpriteTurretInfo : ConditionalTraitInfo, IRenderActorPreviewSpritesInfo,
		Requires<RenderSpritesInfo>, Requires<TurretedInfo>, Requires<BodyOrientationInfo>, Requires<ArmamentInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "turret";

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[Desc("Render recoil")]
		public readonly bool Recoils = true;

		public override object Create(ActorInitializer init) { return new WithSpriteTurret(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
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

			if (IsPlayerPalette)
				p = init.WorldRenderer.Palette(Palette + init.Get<OwnerInit>().PlayerName);
			else if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			yield return new SpriteActorPreview(anim, offset, zOffset, p, rs.Scale);
		}
	}

	public class WithSpriteTurret : ConditionalTrait<WithSpriteTurretInfo>, INotifyDamageStateChanged
	{
		public readonly Animation DefaultAnimation;
		readonly RenderSprites rs;
		readonly BodyOrientation body;
		readonly Turreted t;
		readonly Armament[] arms;

		public WithSpriteTurret(Actor self, WithSpriteTurretInfo info)
			: base(info)
		{
			rs = self.Trait<RenderSprites>();
			body = self.Trait<BodyOrientation>();
			t = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);
			arms = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == info.Turret).ToArray();

			DefaultAnimation = new Animation(self.World, rs.GetImage(self), () => t.TurretFacing);
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, info.Sequence));
			rs.Add(new AnimationWithOffset(DefaultAnimation,
				() => TurretOffset(self),
				() => IsTraitDisabled,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1)), info.Palette, info.IsPlayerPalette);

			// Restrict turret facings to match the sprite
			t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		protected virtual WVec TurretOffset(Actor self)
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

		protected virtual void DamageStateChanged(Actor self)
		{
			if (DefaultAnimation.CurrentSequence != null)
				DefaultAnimation.ReplaceAnim(NormalizeSequence(self, DefaultAnimation.CurrentSequence.Name));
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			DamageStateChanged(self);
		}

		public void PlayCustomAnimation(Actor self, string name, Action after = null)
		{
			DefaultAnimation.PlayThen(NormalizeSequence(self, name), () =>
			{
				CancelCustomAnimation(self);
				if (after != null)
					after();
			});
		}

		public void CancelCustomAnimation(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
		}
	}
}
