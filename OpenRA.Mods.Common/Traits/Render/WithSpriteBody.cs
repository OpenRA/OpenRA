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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Default trait for rendering sprite-based actors.")]
	public class WithSpriteBodyInfo : PausableConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Animation to play when the actor is created.")]
		public readonly string StartSequence = null;

		[SequenceReference]
		[Desc("Animation to play when the actor is idle.")]
		public readonly string Sequence = "idle";

		[Desc("Identifier used to assign modifying traits to this sprite body.")]
		public readonly string Name = "body";

		[Desc("Forces sprite body to be rendered on ground regardless of actor altitude (for example for custom shadow sprites).")]
		public readonly bool ForceToGround = false;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithSpriteBody(init, this); }

		public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			if (IsPlayerPalette)
				p = init.WorldRenderer.Palette(Palette + init.Get<OwnerInit>().InternalName);
			else if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	public class WithSpriteBody : PausableConditionalTrait<WithSpriteBodyInfo>, INotifyDamageStateChanged, IAutoMouseBounds
	{
		public readonly Animation DefaultAnimation;
		readonly RenderSprites rs;
		readonly Animation boundsAnimation;

		public WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info)
			: this(init, info, () => WAngle.Zero) { }

		protected WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info, Func<WAngle> baseFacing)
			: base(info)
		{
			rs = init.Self.Trait<RenderSprites>();

			bool Paused() => IsTraitPaused &&
				DefaultAnimation.CurrentSequence.Name == NormalizeSequence(init.Self, Info.Sequence);

			Func<WVec> subtractDAT = null;
			if (info.ForceToGround)
				subtractDAT = () => new WVec(0, 0, -init.Self.World.Map.DistanceAboveTerrain(init.Self.CenterPosition).Length);

			DefaultAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing, Paused);
			rs.Add(new AnimationWithOffset(DefaultAnimation, subtractDAT, () => IsTraitDisabled), info.Palette, info.IsPlayerPalette);

			// Cache the bounds from the default sequence to avoid flickering when the animation changes
			boundsAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing, Paused);
			boundsAnimation.PlayRepeating(info.Sequence);
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (Info.StartSequence != null)
				PlayCustomAnimation(self, Info.StartSequence,
					() => DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence)));
			else
				DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
		}

		public virtual void PlayCustomAnimation(Actor self, string name, Action after = null)
		{
			DefaultAnimation.PlayThen(NormalizeSequence(self, name), () =>
			{
				CancelCustomAnimation(self);
				after?.Invoke();
			});
		}

		public virtual void PlayCustomAnimationRepeating(Actor self, string name)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, name));
		}

		public virtual void PlayCustomAnimationBackwards(Actor self, string name, Action after = null)
		{
			DefaultAnimation.PlayBackwardsThen(NormalizeSequence(self, name), () =>
			{
				CancelCustomAnimation(self);
				after?.Invoke();
			});
		}

		public virtual void CancelCustomAnimation(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
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

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return boundsAnimation.ScreenBounds(wr, self.CenterPosition, WVec.Zero);
		}
	}
}
