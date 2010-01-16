using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	class CaptureBuilding : IActivity
	{
		Actor target;

		public CaptureBuilding(Actor target) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;

			if (target.Owner == self.Owner)
			{
				if (target.Health == target.Info.Traits.Get<OwnedActorInfo>().HP)
					return NextActivity;
				target.InflictDamage(self, -EngineerCapture.EngineerDamage, Rules.WarheadInfo["Super"]);
			}
			else
			{
				if (target.Health - EngineerCapture.EngineerDamage <= 0)
				{
					target.Owner = self.Owner;
					target.InflictDamage(self, target.Health - EngineerCapture.EngineerDamage, Rules.WarheadInfo["Super"]);
				}
				else
					target.InflictDamage(self, EngineerCapture.EngineerDamage, Rules.WarheadInfo["Super"]);
			}

			// the engineer is sacrificed.
			Game.world.AddFrameEndTask(w => w.Remove(self));

			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
