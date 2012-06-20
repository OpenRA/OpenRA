#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	class ScatterFromProjectilesInfo : ITraitInfo, Requires<MobileInfo>
	{
		public readonly Stance[] ScatterFrom = { Stance.Ally };

		public object Create(ActorInitializer init) { return new ScatterFromProjectiles(init.self, this); }
	}

	class ScatterFromProjectiles : INotifyIncomingProjectile
	{
		private readonly ScatterFromProjectilesInfo Info;

		public ScatterFromProjectiles(Actor self, ScatterFromProjectilesInfo info)
		{
			Info = info;
		}

		public void OnNotifyIncomingProjectile(Actor self, Actor firedBy, int2 projectileDestination, int blastRadius)
		{
			// Should we scatter from the projectile based on the attacker's stance from us?
			Stance stanceAgainstFirer = self.Owner.Stances[firedBy.Owner];
			if (!Info.ScatterFrom.Contains(stanceAgainstFirer)) return;

			// Find the directional vector away from ground-zero for this unit:
			var distVec = (self.CenterLocation - projectileDestination).ToFloat2();
			// Create a vector that is a little less than `radius` distance from ground-zero:
			var awayVec = distVec * (blastRadius * 0.75f / distVec.Length);

			// Create the point to move to:
			var moveTo = (self.CenterLocation + awayVec).ToInt2();
			var moveToCell = Util.CellContaining(moveTo);
			var moveToTarget = Target.FromPos(moveTo);

			// Move the unit out of the way:
			self.CancelActivity();
			self.SetTargetLine(moveToTarget, System.Drawing.Color.Green, false);
			self.QueueActivity(new Move(moveToCell, 0));
		}
	}
}
