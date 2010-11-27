#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public abstract class RenderSimpleInfo : ITraitInfo
	{
		public readonly string Image = null;
		public readonly string[] OverrideTileset = null;
		public readonly string[] OverrideImage = null;
		public readonly string Palette = null;
		public readonly float Scale = 1f;
		public abstract object Create(ActorInitializer init);
	}

	public abstract class RenderSimple : IRender, ITick
	{
		public Dictionary<string, AnimationWithOffset> anims = new Dictionary<string, AnimationWithOffset>();
		public Animation anim { get { return anims[""].Animation; } protected set { anims[""].Animation = value; } }

		public static string GetImage(ActorInfo actor, string Tileset)
		{
			var Info = actor.Traits.Get<RenderSimpleInfo>();
			if (Info.OverrideTileset != null && Tileset != null)
				for (int i = 0; i < Info.OverrideTileset.Length; i++)
					if (Info.OverrideTileset[i] == Tileset)
						return Info.OverrideImage[i];
			
			return Info.Image ?? actor.Name;
		}
		
		string cachedImage = null;
		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;
			
			return cachedImage = GetImage(self.Info, self.World.Map.Tileset);
		}

		RenderSimpleInfo Info;
		public RenderSimple(Actor self, Func<int> baseFacing)
		{
			anims.Add( "", new Animation( GetImage(self), baseFacing ) );
			Info = self.Info.Traits.Get<RenderSimpleInfo>();
		}

		public virtual IEnumerable<Renderable> Render( Actor self )
		{

			foreach( var a in anims.Values )
				if( a.DisableFunc == null || !a.DisableFunc() )
				{
					Renderable ret = a.Image( self );
					if (Info.Scale != 1f)
						ret = ret.WithScale(Info.Scale).WithPos(ret.Pos + 0.5f*ret.Sprite.size*(1 - Info.Scale));
					yield return ( Info.Palette == null ) ? ret : ret.WithPalette(Info.Palette);
				}
		}

		public virtual void Tick(Actor self)
		{
			foreach( var a in anims.Values )
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

		public class AnimationWithOffset
		{
			public Animation Animation;
			public Func<float2> OffsetFunc;
			public Func<bool> DisableFunc;
			public int ZOffset;

			public AnimationWithOffset( Animation a )
				: this( a, null, null )
			{
			}

			public AnimationWithOffset( Animation a, Func<float2> o, Func<bool> d )
			{
				this.Animation = a;
				this.OffsetFunc = o;
				this.DisableFunc = d;
			}

			public Renderable Image( Actor self )
			{
				var r = Util.Centered( self, Animation.Image, self.CenterLocation 
					+ (OffsetFunc != null ? OffsetFunc() : float2.Zero) );
				return ZOffset != 0 ? r.WithZOffset(ZOffset) : r;
			}

			public static implicit operator AnimationWithOffset( Animation a )
			{
				return new AnimationWithOffset( a );
			}
		}
	}
}
