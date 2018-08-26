#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	class Leap : Activity
	{
		readonly Mobile mobile;
		readonly INotifyMoving[] notifyMoving;
		readonly WeaponInfo weapon;
		readonly int length;
		readonly bool wasMoving;

		WPos from;
		WPos to;
		int ticks;
		WAngle angle;
		BitSet<DamageType> damageTypes;

		public Leap(Actor self, Actor target, Armament a, WDist speed, WAngle angle, BitSet<DamageType> damageTypes)
		{
			var targetMobile = target.TraitOrDefault<Mobile>();
			if (targetMobile == null)
				throw new InvalidOperationException("Leap requires a target actor with the Mobile trait");

			this.weapon = a.Weapon;
			this.angle = angle;
			this.damageTypes = damageTypes;
			mobile = self.Trait<Mobile>();
			notifyMoving = self.TraitsImplementing<INotifyMoving>().ToArray();
			mobile.SetLocation(mobile.FromCell, mobile.FromSubCell, targetMobile.FromCell, targetMobile.FromSubCell);
			wasMoving = mobile.IsMoving;
			mobile.IsMoving = true;

			from = self.CenterPosition;
			to = self.World.Map.CenterOfSubCell(targetMobile.FromCell, targetMobile.FromSubCell);
			length = Math.Max((to - from).Length / speed.Length, 1);

			// HACK: why isn't this using the interface?
			self.Trait<WithInfantryBody>().Attacking(self, Target.FromActor(target), a);

			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
		}

		public override Activity Tick(Actor self)
		{
			if (ticks == 0)
			{
				// We only bother notifying INotifyMoving if the activity isn't immediately canceled again.
				// If it's canceled, and the actor wasn't moving before, we properly reset IsMoving as well.
				if (IsCanceled)
				{
					if (!wasMoving)
						mobile.IsMoving = false;

					return NextActivity;
				}
				else
					foreach (var n in notifyMoving)
						n.StartedMoving(self);
			}

			mobile.SetVisualPosition(self, WPos.LerpQuadratic(from, to, angle, ++ticks, length));
			if (ticks >= length)
			{
				mobile.SetLocation(mobile.ToCell, mobile.ToSubCell, mobile.ToCell, mobile.ToSubCell);
				mobile.FinishedMoving(self);
				mobile.IsMoving = false;
				foreach (var n in notifyMoving)
					n.StoppedMoving(self);

				self.World.ActorMap.GetActorsAt(mobile.ToCell, mobile.ToSubCell)
					.Except(new[] { self }).Where(t => weapon.IsValidAgainst(t, self))
					.Do(t => t.Kill(self, damageTypes));

				return NextActivity;
			}

			return this;
		}
	}
}
