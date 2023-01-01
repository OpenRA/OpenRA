#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Will AttackMove to a random location within MoveRadius when idle.",
		"This conflicts with player orders and should only be added to animal creeps.")]
	class AttackWanderInfo : WandersInfo, Requires<AttackMoveInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackWander(init.Self, this); }
	}

	class AttackWander : Wanders
	{
		readonly AttackMove attackMove;

		public AttackWander(Actor self, AttackWanderInfo info)
			: base(self, info)
		{
			attackMove = self.Trait<AttackMove>();
		}

		public override void DoAction(Actor self, CPos targetCell)
		{
			attackMove.ResolveOrder(self, new Order("AttackMove", self, Target.FromCell(self.World, targetCell), false));
		}
	}
}
