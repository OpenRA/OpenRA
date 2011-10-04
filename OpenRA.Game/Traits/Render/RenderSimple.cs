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

namespace OpenRA.Traits
{
	public class RenderSimpleInfo : ITraitInfo
	{
		public readonly string Image = null;
		public readonly string Palette = null;
		public readonly string PlayerPalette = "player";
		public readonly float Scale = 1f;

		public virtual object Create(ActorInitializer init) { return new RenderSimple(init.self); }

		public virtual IEnumerable<Renderable> RenderPreview(ActorInfo building, Player owner)
		{
			var anim = new Animation(RenderSimple.GetImage(building), () => 0);
			anim.PlayRepeating("idle");
			yield return new Renderable(anim.Image, 0.5f * anim.Image.size * (1 - Scale), 
				Palette ?? (owner != null ? PlayerPalette + owner.InternalName : null), 0, Scale);
		}
	}

	public class RenderSimple : IRender, ITick
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
			protected set { anims[""].Animation = value; }
		}

		public static string GetImage(ActorInfo actor)
		{
			var Info = actor.Traits.Get<RenderSimpleInfo>();
			return Info.Image ?? actor.Name;
		}

		string cachedImage = null;
		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;

			return cachedImage = GetImage(self.Info);
		}

		RenderSimpleInfo Info;

		public RenderSimple(Actor self, Func<int> baseFacing)
		{
			anims.Add("", new Animation(GetImage(self), baseFacing));
			Info = self.Info.Traits.Get<RenderSimpleInfo>();
		}

		public RenderSimple(Actor self) : this( self, MakeFacingFunc(self) )
		{
			anim.PlayRepeating("idle");
		}

		public string Palette(Player p) { return Info.Palette ?? Info.PlayerPalette + p.InternalName; }

		public virtual IEnumerable<Renderable> Render(Actor self)
		{
			foreach (var a in anims.Values)
				if (a.DisableFunc == null || !a.DisableFunc())
				{
					Renderable ret = a.Image(self, Palette(self.Owner));
					if (Info.Scale != 1f)
						ret = ret.WithScale(Info.Scale).WithPos(ret.Pos + 0.5f * ret.Sprite.size * (1 - Info.Scale));
					yield return ret;
				}
		}

		public virtual void Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Tick();
		}

		protected virtual string NormalizeSequence(Actor self, string baseSequence)
		{
			string damageState = self.GetDamageState() >= DamageState.Heavy ? "damaged-" : "";
			if (anim.HasSequence(damageState + baseSequence))
				return damageState + baseSequence;
			else
				return baseSequence;
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (anim.HasSequence(name))
				anim.PlayThen(NormalizeSequence(self, name),
					() => anim.PlayRepeating(NormalizeSequence(self, "idle")));
		}
	}
}
