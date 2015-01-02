#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Will AttackMove to a random location within MoveRadius when idle.",
		"This conflicts with player orders and should only be added to animal creeps.")]
	class AttackWanderInfo : ITraitInfo, Requires<AttackMoveInfo>
	{
		public readonly int WanderMoveRadius = 10;

		[Desc("Number of ticks to wait until decreasing the effective move radius.")]
		public readonly int MoveReductionRadiusScale = 5;

		public object Create(ActorInitializer init) { return new AttackWander(init.self, this); }
	}

	class AttackWander : INotifyIdle
	{
		readonly AttackMove attackMove;
		readonly AttackWanderInfo info;

		int ticksIdle;
		int effectiveMoveRadius;

		public AttackWander(Actor self, AttackWanderInfo info)
		{
			this.info = info;
			effectiveMoveRadius = info.WanderMoveRadius;
			attackMove = self.TraitOrDefault<AttackMove>();
		}

		public void TickIdle(Actor self)
		{
			var target = self.CenterPosition + new WVec(0, -1024 * effectiveMoveRadius, 0).Rotate(WRot.FromFacing(self.World.SharedRandom.Next(255)));
			var targetCell = self.World.Map.CellContaining(target);

			if (!self.World.Map.Contains(targetCell))
			{
				// If MoveRadius is too big there might not be a valid cell to order the attack to (if actor is on a small island and can't leave)
				if (++ticksIdle % info.MoveReductionRadiusScale == 0)
					effectiveMoveRadius--;

				return;  // We'll be back the next tick; better to sit idle for a few seconds than prolong this tick indefinitely with a loop
			}

			attackMove.ResolveOrder(self, new Order("AttackMove", self, false) { TargetLocation = targetCell });

			ticksIdle = 0;
			effectiveMoveRadius = info.WanderMoveRadius;
		}
	}
}
