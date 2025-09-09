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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public interface IRenderActorPreviewSpritesInfo : ITraitInfoInterface
	{
		IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, OwnerInit owner);
	}

	[Desc("Render trait fundament that won't work without additional With* render traits.")]
	public class RenderSpritesInfo : TraitInfo, IRenderActorPreviewInfo
	{
		[Desc("The sequence name that defines the actor sprites. Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("A dictionary of faction-specific image overrides.")]
		public readonly Dictionary<string, string> FactionImages = null;

		public override object Create(ActorInitializer init) { return new RenderSprites(init, this); }

		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var sequences = init.World.Map.Sequences;
			var faction = init.GetValue<FactionInit, string>(this);
			var owner = init.Get<OwnerInit>();
			var image = GetImage(init.Actor, faction);

			var facings = 0;
			var body = init.Actor.TraitInfoOrDefault<BodyOrientationInfo>();
			if (body != null)
			{
				facings = body.QuantizedFacings;

				if (facings == -1)
				{
					var qbo = init.Actor.TraitInfoOrDefault<IQuantizeBodyOrientationInfo>();
					facings = qbo?.QuantizedBodyFacings(init.Actor, sequences, faction) ?? 1;
				}
			}

			foreach (var spi in init.Actor.TraitInfos<IRenderActorPreviewSpritesInfo>())
				foreach (var preview in spi.RenderPreviewSprites(init, image, facings, owner))
					yield return preview;
		}

		public string GetImage(ActorInfo actor, string faction)
		{
			if (FactionImages != null && !string.IsNullOrEmpty(faction) && FactionImages.TryGetValue(faction, out var factionImage))
				return factionImage.ToLowerInvariant();

			return (Image ?? actor.Name).ToLowerInvariant();
		}
	}

	public class RenderSprites : IRender, ITick, IActorPreviewInitModifier
	{
		static readonly (DamageState DamageState, string Prefix)[] DamagePrefixes =
		[
			(DamageState.Critical, "critical-"),
			(DamageState.Heavy, "damaged-"),
			(DamageState.Medium, "scratched-"),
			(DamageState.Light, "scuffed-")
		];

		sealed class AnimationWrapper
		{
			public readonly AnimationWithOffset Animation;

			bool cachedVisible;
			WVec cachedOffset;
			ISpriteSequence cachedSequence;

			public AnimationWrapper(AnimationWithOffset animation)
			{
				Animation = animation;
			}

			public bool IsVisible => Animation.DisableFunc == null || !Animation.DisableFunc();

			public bool Tick()
			{
				// Tick the animation
				Animation.Animation.Tick();

				// Return to the caller whether the renderable position or size has changed
				var visible = IsVisible;
				var offset = Animation.OffsetFunc?.Invoke() ?? WVec.Zero;
				var sequence = Animation.Animation.CurrentSequence;

				var updated = visible != cachedVisible || offset != cachedOffset || sequence != cachedSequence;
				cachedVisible = visible;
				cachedOffset = offset;
				cachedSequence = sequence;

				return updated;
			}
		}

		public readonly RenderSpritesInfo Info;
		readonly string faction;
		readonly List<AnimationWrapper> anims = [];
		string cachedImage;

		public static Func<WAngle> MakeFacingFunc(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			if (facing == null)
				return () => WAngle.Zero;

			return () => facing.Facing;
		}

		public RenderSprites(ActorInitializer init, RenderSpritesInfo info)
		{
			Info = info;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;

			return cachedImage = Info.GetImage(self.Info, faction);
		}

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var a in anims)
			{
				if (!a.IsVisible)
					continue;

				var owner = self.EffectiveOwner != null && self.EffectiveOwner.Disguised ? self.EffectiveOwner.Owner : self.Owner;
				foreach (var r in a.Animation.Render(wr, self, owner))
					yield return r;
			}
		}

		public virtual IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
		{
			foreach (var a in anims)
				if (a.IsVisible)
					yield return a.Animation.ScreenBounds(self, wr);
		}

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			var updated = false;
			foreach (var a in anims)
				updated |= a.Tick();

			if (updated)
				self.World.ScreenMap.AddOrUpdate(self);
		}

		public void Add(AnimationWithOffset anim)
		{
			anims.Add(new AnimationWrapper(anim));
		}

		public void Remove(AnimationWithOffset anim)
		{
			anims.RemoveAll(a => a.Animation == anim);
		}

		public static string UnnormalizeSequence(string sequence)
		{
			// Remove existing damage prefix
			foreach (var s in DamagePrefixes)
			{
				if (sequence.StartsWith(s.Prefix, StringComparison.Ordinal))
				{
					sequence = sequence[s.Prefix.Length..];
					break;
				}
			}

			return sequence;
		}

		public static string NormalizeSequence(Animation anim, DamageState state, string sequence)
		{
			// Remove any existing damage prefix
			sequence = UnnormalizeSequence(sequence);

			foreach (var s in DamagePrefixes)
				if (state >= s.DamageState && anim.HasSequence(s.Prefix + sequence))
					return s.Prefix + sequence;

			return sequence;
		}

		// Required by WithSpriteBody and WithInfantryBody
		public int2 AutoSelectionSize()
		{
			return AutoRenderSize();
		}

		// Required by WithSpriteBody and WithInfantryBody
		public int2 AutoRenderSize()
		{
			return anims.Where(b => b.IsVisible
				&& b.Animation.Animation.CurrentSequence != null)
					.Select(a => (a.Animation.Animation.Image.Size.XY * a.Animation.Animation.CurrentSequence.Scale).ToInt2())
					.FirstOrDefault();
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<FactionInit>())
				inits.Add(new FactionInit(faction));
		}
	}
}
