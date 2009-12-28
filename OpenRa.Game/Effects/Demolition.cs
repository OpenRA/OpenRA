using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	class Demolition : IEffect
	{
		Actor attacker;
		Actor target;
		int delay;

		public Demolition(Actor attacker, Actor target, int delay)
		{
			this.attacker = attacker;
			this.target = target;
			this.delay = delay;
		}

		public void Tick()
		{
			if (--delay <= 0)
				Game.world.AddFrameEndTask(w =>
				{
					w.Remove(this);
					target.InflictDamage(attacker, target.Health, Rules.WarheadInfo["DemolishWarhead"]);
				});
		}

		public IEnumerable<Renderable> Render() { yield break; }
	}
}
