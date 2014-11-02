#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	class SandwormInfo : Requires<RenderUnitInfo>, Requires<MobileInfo>, IOccupySpaceInfo
	{
		readonly public int WanderMoveRadius = 20;
		readonly public string WormSignNotification = "WormSign";
		
		public object Create(ActorInitializer init) { return new Sandworm(this); }
	}

	class Sandworm : INotifyIdle
	{
		int ticksIdle;
		int effectiveMoveRadius;
		readonly int maxMoveRadius;

		public Sandworm(SandwormInfo info)
		{
			maxMoveRadius = info.WanderMoveRadius;
			effectiveMoveRadius = info.WanderMoveRadius;

			// TODO: Someone familiar with how the sounds work should fix this:
			// TODO: This should not be here. It should be same as "Enemy unit sighted".
			//Sound.PlayNotification(self.Owner, "Speech", info.WormSignNotification, self.Owner.Country.Race);
		}

		// TODO: This copies AttackWander and builds on top of it. AttackWander should be revised.
		public void TickIdle(Actor self)
		{
			var globalOffset = new WVec(0, -1024 * effectiveMoveRadius, 0).Rotate(WRot.FromFacing(self.World.SharedRandom.Next(255)));
			var offset = new CVec(globalOffset.X/1024, globalOffset.Y/1024);
			var targetlocation = self.Location + offset;

			if (!self.World.Map.Bounds.Contains(targetlocation.X, targetlocation.Y))
			{
				// If MoveRadius is too big there might not be a valid cell to order the attack to (if actor is on a small island and can't leave)
				if (++ticksIdle % 10 == 0)      // completely random number
				{
					effectiveMoveRadius--;
				}
				return;  // We'll be back the next tick; better to sit idle for a few seconds than prolongue this tick indefinitely with a loop
			}

			self.World.IssueOrder(new Order("AttackMove", self, false) { TargetLocation = targetlocation });

			ticksIdle = 0;
			effectiveMoveRadius = maxMoveRadius;
		}

		public bool CanAttackAtLocation(Actor self, CPos targetLocation)
		{
			return self.Trait<Mobile>().MovementSpeedForCell(self, targetLocation) != 0;
		}
	}
}
