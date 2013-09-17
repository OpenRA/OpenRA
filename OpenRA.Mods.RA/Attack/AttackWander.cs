#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Will AttackMove to a random location within MoveRadius when idle.",
		"This conflicts with player orders and should only be added to animal creeps.")]
	class AttackWanderInfo : ITraitInfo
	{
		public readonly int MoveRadius = 4;

		public object Create(ActorInitializer init) { return new AttackWander(init.self, this); }
	}

	class AttackWander : INotifyIdle
	{
		readonly AttackWanderInfo Info;
		public AttackWander(Actor self, AttackWanderInfo info)
		{
			Info = info;
		}

		public void TickIdle(Actor self)
		{
			var target = self.CenterPosition + new WVec(0, -1024*Info.MoveRadius, 0).Rotate(WRot.FromFacing(self.World.SharedRandom.Next(255)));
			self.Trait<AttackMove>().ResolveOrder(self, new Order("AttackMove", self, false) { TargetLocation = target.ToCPos() });
		}
	}
}
