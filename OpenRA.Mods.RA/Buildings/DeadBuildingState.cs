#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class DeadBuildingStateInfo : ITraitInfo, Requires<HealthInfo>, Requires<RenderSpritesInfo>
	{
		public readonly int LingerTime = 20;

		public object Create(ActorInitializer init) { return new DeadBuildingState(init.self, this); }
	}

	class DeadBuildingState : INotifyKilled
	{
		DeadBuildingStateInfo info;
		RenderSprites rs;

		public DeadBuildingState(Actor self, DeadBuildingStateInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			self.Trait<Health>().RemoveOnDeath = !rs.anim.HasSequence("dead");
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!rs.anim.HasSequence("dead")) return;
			
			if (rs.anim.GetSequence("dead").Length > 1)
				rs.anim.Play("dead");
			else
				rs.anim.PlayRepeating("dead");
			
			self.World.AddFrameEndTask(
				w => w.Add(
					new DelayedAction(info.LingerTime,
						() => self.Destroy())));
		}
	}
}
