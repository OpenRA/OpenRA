#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class CriticalBuildingStateInfo : ITraitInfo, ITraitPrerequisite<HealthInfo>
	{
		public readonly int LingerTime = 20;
		public object Create(ActorInitializer init) { return new CriticalBuildingState(init.self, this); }
	}

	class CriticalBuildingState : INotifyDamage
	{
		CriticalBuildingStateInfo info;

		public CriticalBuildingState(Actor self, CriticalBuildingStateInfo info)
		{
			this.info = info;
			self.traits.Get<Health>().RemoveOnDeath = false;
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
