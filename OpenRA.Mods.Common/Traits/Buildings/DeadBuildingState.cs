#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class DeadBuildingStateInfo : ITraitInfo, Requires<HealthInfo>, Requires<RenderSimpleInfo>
	{
		public readonly int LingerTime = 20;

		public object Create(ActorInitializer init) { return new DeadBuildingState(init.Self, this); }
	}

	class DeadBuildingState : INotifyKilled
	{
		DeadBuildingStateInfo info;
		RenderSimple rs;

		public DeadBuildingState(Actor self, DeadBuildingStateInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSimple>();
			self.Trait<Health>().RemoveOnDeath = !rs.DefaultAnimation.HasSequence("dead");
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!rs.DefaultAnimation.HasSequence("dead")) return;
			
			if (rs.DefaultAnimation.GetSequence("dead").Length > 1)
				rs.DefaultAnimation.Play("dead");
			else
				rs.DefaultAnimation.PlayRepeating("dead");
			
			self.World.AddFrameEndTask(
				w => w.Add(
					new DelayedAction(info.LingerTime,
						() => self.Destroy())));
		}
	}
}
