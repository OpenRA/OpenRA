#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class Leap : Activity
	{
		readonly Mobile mobile;
		readonly WPos origin, destination;
		readonly int length;
		readonly AttackLeap attack;
		readonly Target target;

		int ticks = 0;

		/// <summary> Visible move that changes the position in the world. </summary>
		public Leap(Actor self, WPos origin, WPos destination, int length, Mobile mobile, AttackLeap attack, Target target)
		{
			this.mobile = mobile;
			this.origin = origin;
			this.destination = destination;
			this.length = length;
			this.attack = attack;
			this.target = target;

			// Must not be canceled mid-air!
			IsInterruptible = false;
		}

		public override Activity Tick(Actor self)
		{
			var position = length > 1 ? WPos.Lerp(origin, destination, ticks, length - 1) : destination;

			mobile.SetVisualPosition(self, position);

			// We are at the destination.
			if (++ticks >= length)
			{
				mobile.IsMoving = false;
				mobile.SetPosition(self, position);

				if (!self.IsDead && !self.Disposed)
				{
					attack.LeapBuffOff(self);
					attack.DoAttack(self, target);
				}

				return NextActivity;
			}

			mobile.IsMoving = true;

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(destination);
		}
	}
}
