#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p);
	}

	[Desc("Render trait fundament that won't work without additional With* render traits.")]
	public class RenderSpritesInfo : TraitInfo, IRenderActorPreviewInfo
	{
		[Desc("The sequence name that defines the actor sprites. Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("A dictionary of faction-specific image overrides.")]
		public readonly Dictionary<string, string> FactionImages = null;

		[PaletteReference]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[PaletteReference(true)]
		[Desc("Custom PlayerColorPalette: BaseName")]
		public readonly string PlayerPalette = "player";

		[Desc("Change the sprite image size.")]
		public readonly float Scale = 1f;

		public override object Create(ActorInitializer init) { return new RenderSprites(init, this); }

		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var sequenceProvider = init.World.Map.Rules.Sequences;
			var faction = init.GetValue<FactionInit, string>(this);
			var ownerName = init.Get<OwnerInit>(this).InternalName;
			var image = GetImage(init.Actor, sequenceProvider, faction);
			var palette = init.WorldRenderer.Palette(Palette ?? PlayerPalette + ownerName);

			var facings = 0;
			var body = init.Actor.TraitInfoOrDefault<BodyOrientationInfo>();
			if (body != null)
			{
				facings = body.QuantizedFacings;

				if (facings == -1)
				{
					var qbo = init.Actor.TraitInfoOrDefault<IQuantizeBodyOrientationInfo>();
					facings = qbo != null ? qbo.QuantizedBodyFacings(init.Actor, sequenceProvider, faction) : 1;
				}
			}

			foreach (var spi in init.Actor.TraitInfos<IRenderActorPreviewSpritesInfo>())
				foreach (var preview in spi.RenderPreviewSprites(init, this, image, facings, palette))
					yield return preview;
		}

		public string GetImage(ActorInfo actor, SequenceProvider sequenceProvider, string faction)
		{
			if (FactionImages != null && !string.IsNullOrEmpty(faction))
			{
				string factionImage = null;
				if (FactionImages.TryGetValue(faction, out factionImage) && sequenceProvider.HasSequence(factionImage))
					return factionImage;
			}

			return (Image ?? actor.Name).ToLowerInvariant();
		}
	}

	public class RenderSprites : IRender, ITick, INotifyOwnerChanged, INotifyEffectiveOwnerChanged, IActorPreviewInitModifier
	{
		static readonly Pair<DamageState, string>[] DamagePrefixes =
		{
			Pair.New(DamageState.Critical, "critical-"),
			Pair.New(DamageState.Heavy, "damaged-"),
			Pair.New(DamageState.Medium, "scratched-"),
			Pair.New(DamageState.Light, "scuffed-")
		};

		class AnimationWrapper
		{
			public readonly AnimationWithOffset Animation;
			public readonly string Palette;
			public readonly bool IsPlayerPalette;
			public PaletteReference PaletteReference { get; private set; }

			bool cachedVisible;
			WVec cachedOffset;
			ISpriteSequence cachedSequence;

			public AnimationWrapper(AnimationWithOffset animation, string palette, bool isPlayerPalette)
			{
				Animation = animation;
				Palette = palette;
				IsPlayerPalette = isPlayerPalette;
			}

			public void CachePalette(WorldRenderer wr, Player owner)
			{
				PaletteReference = wr.Palette(IsPlayerPalette ? Palette + owner.InternalName : Palette);
			}

			public void OwnerChanged()
			{
				// Update the palette reference next time we draw
				if (IsPlayerPalette)
					PaletteReference = null;
			}

			public bool IsVisible
			{
				get
				{
					return Animation.DisableFunc == null || !Animation.DisableFunc();
				}
			}

			public bool Tick()
			{
				// Tick the animation
				Animation.Animation.Tick();

				// Return to the caller whether the renderable position or size has changed
				var visible = IsVisible;
				var offset = Animation.OffsetFunc != null ? Animation.OffsetFunc() : WVec.Zero;
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
		readonly List<AnimationWrapper> anims = new List<AnimationWrapper>();
		string cachedImage;

		public static Func<WAngle> MakeFacingFunc(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			if (facing == null)
				return () => WAngle.Zero;

			return () => WAngle.FromFacing(facing.Facing);
		}

		public RenderSprites(ActorInitializer init, RenderSpritesInfo info)
		{
			Info = info;
			faction = init.GetValue<FactionInit, string>(info, init.Self.Owner.Faction.InternalName);
		}

		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;

			return cachedImage = Info.GetImage(self.Info, self.World.Map.Rules.Sequences, faction);
		}

		public void UpdatePalette()
		{
			foreach (var anim in anims)
				anim.OwnerChanged();
		}

		public virtual void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UpdatePalette(); }
		public void OnEffectiveOwnerChanged(Actor self, Player oldEffectiveOwner, Player newEffectiveOwner) { UpdatePalette(); }

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var a in anims)
			{
				if (!a.IsVisible)
					continue;

				if (a.PaletteReference == null)
				{
					var owner = self.EffectiveOwner != null && self.EffectiveOwner.Disguised ? self.EffectiveOwner.Owner : self.Owner;
					a.CachePalette(wr, owner);
				}

				foreach (var r in a.Animation.Render(self, wr, a.PaletteReference, Info.Scale))
					yield return r;
			}
		}

		public virtual IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
		{
			foreach (var a in anims)
				if (a.IsVisible)
					yield return a.Animation.ScreenBounds(self, wr, Info.Scale);
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

		public void Add(AnimationWithOffset anim, string palette = null, bool isPlayerPalette = false)
		{
			// Use defaults
			if (palette == null)
			{
				palette = Info.Palette ?? Info.PlayerPalette;
				isPlayerPalette = Info.Palette == null;
			}

			anims.Add(new AnimationWrapper(anim, palette, isPlayerPalette));
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
				if (sequence.StartsWith(s.Second, StringComparison.Ordinal))
				{
					sequence = sequence.Substring(s.Second.Length);
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
				if (state >= s.First && anim.HasSequence(s.Second + sequence))
					return s.Second + sequence;

			return sequence;
		}

		// Required by WithSpriteBody and WithInfantryBody
		public int2 AutoSelectionSize(Actor self)
		{
			return AutoRenderSize(self);
		}

		// Required by WithSpriteBody and WithInfantryBody
		public int2 AutoRenderSize(Actor self)
		{
			return anims.Where(b => b.IsVisible
				&& b.Animation.Animation.CurrentSequence != null)
					.Select(a => (a.Animation.Animation.Image.Size.XY * Info.Scale).ToInt2())
					.FirstOrDefault();
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<FactionInit>())
				inits.Add(new FactionInit(faction));
		}
	}
}
