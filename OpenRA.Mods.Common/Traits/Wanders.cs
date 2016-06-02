﻿#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Wanders around aimlessly while idle.")]
	public abstract class WandersInfo : ITraitInfo
	{
		public readonly int WanderMoveRadius = 1;

		[Desc("Number of ticks to wait before decreasing the effective move radius.")]
		public readonly int ReduceMoveRadiusDelay = 5;

		[Desc("Minimum amount of ticks the actor will sit idly before starting to wander.")]
		public readonly int MinMoveDelay = 0;

		[Desc("Maximum amount of ticks the actor will sit idly before starting to wander.")]
		public readonly int MaxMoveDelay = 0;

		public abstract object Create(ActorInitializer init);
	}

	public class Wanders : INotifyIdle, INotifyBecomingIdle
	{
		readonly Actor self;
		readonly WandersInfo info;

		int countdown;
		int ticksIdle;
		int effectiveMoveRadius;

		public Wanders(Actor self, WandersInfo info)
		{
			this.self = self;
			this.info = info;
			effectiveMoveRadius = info.WanderMoveRadius;
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			countdown = self.World.SharedRandom.Next(info.MinMoveDelay, info.MaxMoveDelay);
		}

		public void TickIdle(Actor self)
		{
			if (--countdown > 0)
				return;

			var targetCell = PickTargetLocation();
			if (targetCell != CPos.Zero)
				DoAction(self, targetCell);
		}

		CPos PickTargetLocation()
		{
			var target = self.CenterPosition + new WVec(0, -1024 * effectiveMoveRadius, 0).Rotate(WRot.FromFacing(self.World.SharedRandom.Next(255)));
			var targetCell = self.World.Map.CellContaining(target);

			if (!self.World.Map.Contains(targetCell))
			{
				// If MoveRadius is too big there might not be a valid cell to order the attack to (if actor is on a small island and can't leave)
				if (++ticksIdle % info.ReduceMoveRadiusDelay == 0)
					effectiveMoveRadius--;

				return CPos.Zero; // We'll be back the next tick; better to sit idle for a few seconds than prolong this tick indefinitely with a loop
			}

			ticksIdle = 0;
			effectiveMoveRadius = info.WanderMoveRadius;

			return targetCell;
		}

		public virtual void DoAction(Actor self, CPos targetCell)
		{
			throw new NotImplementedException("Base class Wanders does not implement method DoAction!");
		}
	}
}
