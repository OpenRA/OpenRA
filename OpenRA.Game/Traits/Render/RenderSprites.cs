#region Copyright & License Information
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
		public Dictionary<string, AnimationWithOffset> anims = new Dictionary<string, AnimationWithOffset>();

		public static Func<int> MakeFacingFunc(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			if (facing == null) return () => 0;
			return () => facing.Facing;
		}

		public Animation anim
		{
			get { return anims[""].Animation; }
			protected set { anims[""] = new AnimationWithOffset(value,
				anims[""].OffsetFunc, anims[""].DisableFunc, anims[""].Paused, anims[""].ZOffset); }
		}

		RenderSpritesInfo Info;
		string cachedImage = null;
		bool initializePalette = true;
		protected PaletteReference palette;

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

		protected void UpdatePalette() { initializePalette = true; }
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UpdatePalette(); }

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (initializePalette)
			{
				palette = wr.Palette(PaletteName(self));
				initializePalette = false;
			}

			foreach (var a in anims.Values)
			{
				if (a.DisableFunc != null && a.DisableFunc())
					continue;

				foreach (var r in a.Render(self, wr, palette, Info.Scale))
					yield return r;
			}
		}

		public virtual void Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Tick();
		}

		public void Add(string key, AnimationWithOffset anim)
		{
			anims.Add(key, anim);
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
				if (state >= s.First && anim.HasSequence(s.Second+baseSequence))
				    return s.Second+baseSequence;

			return baseSequence;
		}
	}
}
