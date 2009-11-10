using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	abstract class RenderSimple : IRender, ITick
	{
		public Animation anim;

		public RenderSimple(Actor self)
		{
			anim = new Animation(self.unitInfo.Image ?? self.unitInfo.Name);
		}

		public abstract IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self);

		public virtual void Tick(Actor self)
		{
			anim.Tick();
		}
	}
}
