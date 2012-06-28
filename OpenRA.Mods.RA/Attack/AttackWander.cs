﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using System.Drawing;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA
{
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
			var target = (Util.SubPxVector[self.World.SharedRandom.Next(255)] * Info.MoveRadius).ToPVecInt().ToCVec() + self.Location;
			self.Trait<AttackMove>().ResolveOrder(self, new Order("AttackMove", self, false) { TargetLocation = target });
		}
	}
}
