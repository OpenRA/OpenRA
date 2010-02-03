using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;
using OpenRa.Graphics;

namespace OpenRa.Effects
{
	class FlashTarget : IEffect
	{
		Actor target;
		int remainingTicks = 4;

		public FlashTarget(Actor target)
		{
			this.target = target;
			foreach (var e in target.World.Effects.OfType<FlashTarget>().Where(a => a.target == target).ToArray())
				target.World.Remove(e);
		}

		public void Tick( World world )
		{
			if (--remainingTicks == 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			if (remainingTicks % 2 == 0)
				foreach (var r in target.Render())
					yield return r.WithPalette("highlight");
		}
	}
}
