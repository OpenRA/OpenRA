using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	abstract class RenderSimple : IRender, ITick
	{
		public Animation anim;

		public RenderSimple(Actor self)
		{
			anim = new Animation(self.Info.Image ?? self.Info.Name);
		}

		public abstract IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self);

		public virtual void Tick(Actor self)
		{
			anim.Tick();
		}
	}
}
