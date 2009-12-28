using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class CaptureBuilding : IActivity
	{
		Actor target;

		public CaptureBuilding(Actor target) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;
			// todo: don't waste engineers on things that have been captured 
			// between order issue time and the time we hit the building.

			target.Health -= EngineerCapture.EngineerDamage;
			if (target.Health <= 0)
			{
				target.Owner = self.Owner;
				target.Health = EngineerCapture.EngineerDamage;
			}

			// the engineer is sacrificed.
			Game.world.AddFrameEndTask(w => w.Remove(self));

			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
