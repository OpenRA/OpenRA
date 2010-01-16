using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class DelayedAction : IEffect
	{
		Action a;
		int delay;

		public DelayedAction(int delay, Action a)
		{
			this.a = a;
			this.delay = delay;
		}

		public void Tick()
		{
			if (--delay <= 0)
				Game.world.AddFrameEndTask(w => { w.Remove(this); a(); });
		}

		public IEnumerable<Renderable> Render() { yield break; }
	}
}
