﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public class RenderSpritesInfo : ITraitInfo
	{
		[Desc("Defaults to the actor name.")]
		public readonly string Image = null;

		[Desc("Custom palette name")]
		public readonly string Palette = null;
		[Desc("Custom PlayerColorPalette: BaseName")]
		public readonly string PlayerPalette = "player";
		[Desc("Change the sprite image size.")]
		public readonly float Scale = 1f;

		public virtual object Create(ActorInitializer init) { return new RenderSprites(init.self); }
	}

	public class RenderSprites : IRender, ITick, INotifyOwnerChanged
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

		Dictionary<string, AnimationWrapper> anims = new Dictionary<string, AnimationWrapper>();

		public static Func<int> MakeFacingFunc(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			if (facing == null) return () => 0;
			return () => facing.Facing;
		}

		RenderSpritesInfo Info;
		string cachedImage = null;

		public RenderSprites(Actor self)
		{
			Info = self.Info.Traits.Get<RenderSpritesInfo>();
		}

		public static string GetImage(ActorInfo actor)
		{
			var Info = actor.Traits.Get<RenderSpritesInfo>();
			return Info.Image ?? actor.Name;
		}

		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;

			return cachedImage = GetImage(self.Info);
		}

		protected virtual string PaletteName(Actor self)
		{
			return Info.Palette ?? Info.PlayerPalette + self.Owner.InternalName;
		}

		protected void UpdatePalette()
		{
			foreach (var anim in anims.Values)
				anim.OwnerChanged();
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UpdatePalette(); }

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var a in anims.Values)
			{
				if (!a.IsVisible)
					continue;

				if (a.PaletteReference == null)
					a.CachePalette(wr, self.Owner);

				foreach (var r in a.Animation.Render(self, wr, a.PaletteReference, Info.Scale))
					yield return r;
			}
		}

		public virtual void Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Animation.Tick();
		}

		public void Add(string key, AnimationWithOffset anim, string palette = null, bool isPlayerPalette = false)
		{
			// Use defaults
			if (palette == null)
			{
				palette = Info.Palette ?? Info.PlayerPalette;
				isPlayerPalette = Info.Palette == null;
			}

			anims.Add(key, new AnimationWrapper(anim, palette, isPlayerPalette));
		}

		public void Remove(string key)
		{
			anims.Remove(key);
		}

		public static string NormalizeSequence(Animation anim, DamageState state, string baseSequence)
		{
			var states = new Pair<DamageState, string>[]
			{
				Pair.New(DamageState.Critical, "critical-"),
				Pair.New(DamageState.Heavy, "damaged-"),
				Pair.New(DamageState.Medium, "scratched-"),
				Pair.New(DamageState.Light, "scuffed-")
			};

			foreach (var s in states)
				if (state >= s.First && anim.HasSequence(s.Second + baseSequence))
					return s.Second + baseSequence;

			return baseSequence;
		}

		// Required by RenderSimple
		protected int2 AutoSelectionSize(Actor self)
		{
			return anims.Values.Where(b => b.IsVisible
				&& b.Animation.Animation.CurrentSequence != null)
					.Select(a => (a.Animation.Animation.Image.size*Info.Scale).ToInt2())
					.FirstOrDefault();
		}
	}
}
