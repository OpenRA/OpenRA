using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Effects
{
	class FlashTarget : IEffect
	{
		Actor target;
		int remainingTicks = 4;

		public FlashTarget(Actor target)
		{
			this.target = target;
			foreach (var e in Game.world.Effects.OfType<FlashTarget>().Where(a => a.target == target).ToArray())
				Game.world.Remove(e);
		}

		public void Tick()
		{
			if (--remainingTicks == 0)
				Game.world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			if (remainingTicks % 2 == 0)
				foreach (var r in target.Render())
					yield return r.WithPalette(PaletteType.Highlight);
		}
	}
}
