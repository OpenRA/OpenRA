using System;
using System.Collections.Generic;
using IjwFramework.Collections;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	abstract class RenderSimple : IRender, ITick
	{
		public Dictionary<string, AnimationWithOffset> anims = new Dictionary<string, AnimationWithOffset>();
		public Animation anim { get { return anims[ "" ].Animation; } }

		public RenderSimple(Actor self)
		{
			anims.Add( "", new Animation( self.Info.Image ?? self.Info.Name ) );
		}

		public virtual IEnumerable<Tuple<Sprite, float2, int>> Render( Actor self )
		{
			foreach( var a in anims.Values )
				if( a.DisableFunc == null || !a.DisableFunc() )
					yield return a.Image( self );
		}

		public virtual void Tick(Actor self)
		{
			foreach( var a in anims.Values )
				a.Animation.Tick();
		}

		public class AnimationWithOffset
		{
			public Animation Animation;
			public Func<float2> OffsetFunc;
			public Func<bool> DisableFunc;

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

			public Tuple<Sprite, float2, int> Image( Actor self )
			{
				if( OffsetFunc != null )
					return Util.Centered( self, Animation.Image, self.CenterLocation + OffsetFunc() );
				else
					return Util.Centered( self, Animation.Image, self.CenterLocation );
			}

			public static implicit operator AnimationWithOffset( Animation a )
			{
				return new AnimationWithOffset( a );
			}
		}
	}
}
