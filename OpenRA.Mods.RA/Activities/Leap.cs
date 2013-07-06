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
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Leap : Activity
	{
		Mobile mobile;

		PPos from;
		PPos to;

		int moveFraction;
		const int length = 6;

		public Leap(Actor self, Target target)
		{
			if (!target.IsActor)
				throw new InvalidOperationException("Leap requires a target actor");

			var targetMobile = target.Actor.TraitOrDefault<Mobile>();
			if (targetMobile == null)
				throw new InvalidOperationException("Leap requires a target actor with the Mobile trait");

			mobile = self.Trait<Mobile>();
			mobile.SetLocation(mobile.fromCell, mobile.fromSubCell, targetMobile.fromCell, targetMobile.fromSubCell);
			mobile.IsMoving = true;

			from = self.CenterLocation;
			to = Util.CenterOfCell(targetMobile.fromCell) + MobileInfo.SubCellOffsets[targetMobile.fromSubCell];

			self.Trait<RenderInfantry>().Attacking(self, target);
			Sound.Play("dogg5p.aud", self.CenterLocation);
		}

		public override Activity Tick(Actor self)
		{
			if (moveFraction == 0 && IsCanceled)
				return NextActivity;

			mobile.AdjustPxPosition(self, PPos.Lerp(from, to, moveFraction++, length - 1));
			if (moveFraction >= length)
			{
				mobile.SetLocation(mobile.toCell, mobile.toSubCell, mobile.toCell, mobile.toSubCell);
				mobile.FinishedMoving(self);
				mobile.IsMoving = false;

				// Kill whatever else is in our new cell
				self.World.ActorMap.GetUnitsAt(mobile.toCell, mobile.toSubCell).Except(new []{self}).Do(a => a.Kill(self));
				return NextActivity;
			}

			return this;
		}
	}
}
