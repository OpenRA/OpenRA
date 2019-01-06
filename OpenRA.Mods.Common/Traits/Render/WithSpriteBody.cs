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
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Default trait for rendering sprite-based actors.")]
	public class WithSpriteBodyInfo : PausableConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Animation to play when the actor is created."), SequenceReference]
		public readonly string StartSequence = null;

		[Desc("Animation to play when the actor is idle."), SequenceReference]
		public readonly string Sequence = "idle";

		[Desc("Identifier used to assign modifying traits to this sprite body.")]
		public readonly string Name = "body";

		[Desc("Forces sprite body to be rendered on ground regardless of actor altitude (for example for custom shadow sprites).")]
		public readonly bool ForceToGround = false;

		public override object Create(ActorInitializer init) { return new WithSpriteBody(init, this); }

		public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	public class WithSpriteBody : PausableConditionalTrait<WithSpriteBodyInfo>, INotifyDamageStateChanged, IAutoMouseBounds
	{
		public readonly Animation DefaultAnimation;
		readonly RenderSprites rs;
		readonly Animation boundsAnimation;

		public WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info)
			: this(init, info, () => 0) { }

		protected WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info, Func<int> baseFacing)
			: base(info)
		{
			rs = init.Self.Trait<RenderSprites>();

			Func<bool> paused = () => IsTraitPaused &&
				DefaultAnimation.CurrentSequence.Name == NormalizeSequence(init.Self, Info.Sequence);

			Func<WVec> subtractDAT = null;
			if (info.ForceToGround)
				subtractDAT = () => new WVec(0, 0, -init.Self.World.Map.DistanceAboveTerrain(init.Self.CenterPosition).Length);

			DefaultAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing, paused);
			rs.Add(new AnimationWithOffset(DefaultAnimation, subtractDAT, () => IsTraitDisabled));

			// Cache the bounds from the default sequence to avoid flickering when the animation changes
			boundsAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing, paused);
			boundsAnimation.PlayRepeating(info.Sequence);

			if (info.StartSequence != null)
				PlayCustomAnimation(init.Self, info.StartSequence,
					() => DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, info.Sequence)));
			else
				DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, info.Sequence));
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
		}

		protected override void TraitEnabled(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
		}

		public virtual void PlayCustomAnimation(Actor self, string name, Action after = null)
		{
			DefaultAnimation.PlayThen(NormalizeSequence(self, name), () =>
			{
				CancelCustomAnimation(self);
				if (after != null)
					after();
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
				if (after != null)
					after();
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
			return boundsAnimation.ScreenBounds(wr, self.CenterPosition, WVec.Zero, rs.Info.Scale);
		}
	}
}
