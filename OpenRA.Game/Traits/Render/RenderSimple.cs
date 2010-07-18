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
		public readonly string[] OverrideTheater = null;
		public readonly string[] OverrideImage = null;
		public readonly string Palette = null;
		public abstract object Create(ActorInitializer init);
	}

	public abstract class RenderSimple : IRender, ITick
	{
		public Dictionary<string, AnimationWithOffset> anims = new Dictionary<string, AnimationWithOffset>();
		public Animation anim { get { return anims[""].Animation; } protected set { anims[""].Animation = value; } }

		string cachedImage = null;
		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;
			
			var Info = self.Info.Traits.Get<RenderSimpleInfo>();
			if (Info.OverrideTheater != null)
				for (int i = 0; i < Info.OverrideTheater.Length; i++)
					if (Info.OverrideTheater[i] == self.World.Map.Theater)
						return cachedImage = Info.OverrideImage[i];
			
			return cachedImage = Info.Image ?? self.Info.Name;
		}

		public RenderSimple(Actor self, Func<int> baseFacing)
		{
			anims.Add( "", new Animation( GetImage(self), baseFacing ) );
		}

		public virtual IEnumerable<Renderable> Render( Actor self )
		{
			var palette = self.Info.Traits.Get<RenderSimpleInfo>().Palette;
			foreach( var a in anims.Values )
				if( a.DisableFunc == null || !a.DisableFunc() )
					yield return ( palette == null ) ? a.Image( self ) : a.Image( self ).WithPalette(palette);
		}

		public virtual void Tick(Actor self)
		{
			foreach( var a in anims.Values )
				a.Animation.Tick();
		}

		protected virtual string GetPrefix(Actor self)
		{
			return self.GetDamageState() == DamageState.Half ? "damaged-" : "";
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (anim.HasSequence(name))
				anim.PlayThen(GetPrefix(self) + name,
					() => anim.PlayRepeating(GetPrefix(self) + "idle"));
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
