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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public interface IRenderActorPreviewSpritesInfo : ITraitInfo
	{
		IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p);
	}

	[Desc("Render trait fundament that won't work without additional With* render traits.")]
	public class RenderSpritesInfo : IRenderActorPreviewInfo, ITraitInfo
	{
		[Desc("The sequence name that defines the actor sprites. Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("A dictionary of faction-specific image overrides.")]
		public readonly Dictionary<string, string> FactionImages = null;

		[Desc("Custom palette name")]
		[PaletteReference] public readonly string Palette = null;

		[Desc("Custom PlayerColorPalette: BaseName")]
		[PaletteReference(true)] public readonly string PlayerPalette = "player";

		[Desc("Change the sprite image size.")]
		public readonly float Scale = 1f;

		public virtual object Create(ActorInitializer init) { return new RenderSprites(init, this); }

		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var sequenceProvider = init.World.Map.Rules.Sequences;
			var faction = init.Get<FactionInit, string>();
			var ownerName = init.Get<OwnerInit>().PlayerName;
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
		class AnimationWrapper
		{
			public readonly AnimationWithOffset Animation;
			public readonly string Palette;
			public readonly bool IsPlayerPalette;
			public PaletteReference PaletteReference { get; private set; }

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
		}

		readonly string faction;
		readonly RenderSpritesInfo info;
		readonly List<AnimationWrapper> anims = new List<AnimationWrapper>();
		string cachedImage;

		public static Func<int> MakeFacingFunc(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			if (facing == null) return () => 0;
			return () => facing.Facing;
		}

		public RenderSprites(ActorInitializer init, RenderSpritesInfo info)
		{
			this.info = info;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;

			return cachedImage = info.GetImage(self.Info, self.World.Map.Rules.Sequences, faction);
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

				foreach (var r in a.Animation.Render(self, wr, a.PaletteReference, info.Scale))
					yield return r;
			}
		}

		public virtual void Tick(Actor self)
		{
			foreach (var a in anims)
				a.Animation.Animation.Tick();
		}

		public void Add(AnimationWithOffset anim, string palette = null, bool isPlayerPalette = false)
		{
			// Use defaults
			if (palette == null)
			{
				palette = info.Palette ?? info.PlayerPalette;
				isPlayerPalette = info.Palette == null;
			}

			anims.Add(new AnimationWrapper(anim, palette, isPlayerPalette));
		}

		public void Remove(AnimationWithOffset anim)
		{
			anims.RemoveAll(a => a.Animation == anim);
		}

		public static string NormalizeSequence(Animation anim, DamageState state, string sequence)
		{
			var states = new Pair<DamageState, string>[]
			{
				Pair.New(DamageState.Critical, "critical-"),
				Pair.New(DamageState.Heavy, "damaged-"),
				Pair.New(DamageState.Medium, "scratched-"),
				Pair.New(DamageState.Light, "scuffed-")
			};

			// Remove existing damage prefix
			foreach (var s in states)
			{
				if (sequence.StartsWith(s.Second))
				{
					sequence = sequence.Substring(s.Second.Length);
					break;
				}
			}

			foreach (var s in states)
				if (state >= s.First && anim.HasSequence(s.Second + sequence))
					return s.Second + sequence;

			return sequence;
		}

		// Required by WithSpriteBody and WithInfantryBody
		public int2 AutoSelectionSize(Actor self)
		{
			return anims.Where(b => b.IsVisible
				&& b.Animation.Animation.CurrentSequence != null)
					.Select(a => (a.Animation.Animation.Image.Size.XY * info.Scale).ToInt2())
					.FirstOrDefault();
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<FactionInit>())
				inits.Add(new FactionInit(faction));
		}
	}
}
