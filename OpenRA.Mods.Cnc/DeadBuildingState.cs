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
	class DeadBuildingStateInfo : ITraitInfo, ITraitPrerequisite<HealthInfo>, ITraitPrerequisite<RenderSimpleInfo>
	{
		public readonly int LingerTime = 20;
		public readonly bool Zombie = false; // Civilian structures stick around after death
		public object Create(ActorInitializer init) { return new DeadBuildingState(init.self, this); }
	}

	class DeadBuildingState : INotifyDamage
	{
		DeadBuildingStateInfo info;
		RenderSimple rs;
		public DeadBuildingState(Actor self, DeadBuildingStateInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSimple>();
			self.Trait<Health>().RemoveOnDeath = !rs.anim.HasSequence("dead");
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead)
			{
				if (!rs.anim.HasSequence("dead")) return;
				rs.anim.PlayRepeating("dead");
				if (!info.Zombie)
					self.World.AddFrameEndTask(
						w => w.Add(
							new DelayedAction(info.LingerTime,
								() => self.Destroy())));
			}
		}
	}
}
