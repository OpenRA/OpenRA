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
using OpenRA.GameRules;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Leap : Activity
	{
		Mobile mobile;
		WeaponInfo weapon;

		WPos from;
		WPos to;
		int ticks;
		int length;
		WAngle angle;

		public Leap(Actor self, Actor target, WeaponInfo weapon, WRange speed, WAngle angle)
		{
			var targetMobile = target.TraitOrDefault<Mobile>();
			if (targetMobile == null)
				throw new InvalidOperationException("Leap requires a target actor with the Mobile trait");

			this.weapon = weapon;
			this.angle = angle;
			mobile = self.Trait<Mobile>();
			mobile.SetLocation(mobile.fromCell, mobile.fromSubCell, targetMobile.fromCell, targetMobile.fromSubCell);
			mobile.IsMoving = true;

			from = self.CenterPosition;
			to = self.World.Map.CenterOfCell(targetMobile.fromCell) + MobileInfo.SubCellOffsets[targetMobile.fromSubCell];
			length = Math.Max((to - from).Length / speed.Range, 1);

			self.Trait<RenderInfantry>().Attacking(self, Target.FromActor(target));

			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
		}

		public override Activity Tick(Actor self)
		{
			if (ticks == 0 && IsCanceled)
				return NextActivity;

			mobile.SetVisualPosition(self, WPos.LerpQuadratic(from, to, angle, ++ticks, length));
			if (ticks >= length)
			{
				mobile.SetLocation(mobile.toCell, mobile.toSubCell, mobile.toCell, mobile.toSubCell);
				mobile.FinishedMoving(self);
				mobile.IsMoving = false;

				self.World.ActorMap.GetUnitsAt(mobile.toCell, mobile.toSubCell)
					.Except(new []{self}).Where(t => weapon.IsValidAgainst(t, self))
					.Do(t => t.Kill(self));

				return NextActivity;
			}

			return this;
		}
	}
}
