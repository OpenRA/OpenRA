#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Leap : Activity
	{
		readonly Mobile mobile;
		readonly WPos origin, destination;
		readonly int length;

		int ticks = 0;

		/// <summary> Visible move that changes the position in the world. </summary>
		public Leap(Actor self, WPos origin, WPos destination, int length, Mobile mobile)
		{
			this.mobile = mobile;
			this.origin = origin;
			this.destination = destination;
			this.length = length;
		}

		public override Activity Tick(Actor self)
		{
			var position = length > 1 ? WPos.Lerp(origin, destination, ticks, length - 1) : destination;

			mobile.SetVisualPosition(self, position);
			if (++ticks >= length)
			{
				mobile.IsMoving = false;
				mobile.SetPosition(self, position);

				return NextActivity;
			}

			mobile.IsMoving = true;

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(destination);
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
