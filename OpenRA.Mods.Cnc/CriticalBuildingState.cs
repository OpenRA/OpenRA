using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.Cnc
{
	class CriticalBuildingStateInfo : ITraitInfo
	{
		public readonly int LingerTime = 20;
		public object Create(Actor self) { return new CriticalBuildingState(self, this); }
	}

	class CriticalBuildingState : INotifyDamage
	{
		CriticalBuildingStateInfo info;

		public CriticalBuildingState(Actor self, CriticalBuildingStateInfo info)
		{
			this.info = info;
			self.RemoveOnDeath = false;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead)
			{
				self.traits.Get<RenderSimple>().anim.PlayRepeating("critical-idle");
				self.World.AddFrameEndTask(
					w => w.Add(
						new DelayedAction(info.LingerTime,
							() => w.Remove(self))));
			}
		}
	}
}
