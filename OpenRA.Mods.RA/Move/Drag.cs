#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class Drag : Activity
	{
		int2 endLocation;
		int2 startLocation;
		int length;
		int ticks = 0;

		public Drag(int2 start, int2 end, int length)
		{
			startLocation = start;
			endLocation = end;
			this.length = length;
		}

		public override Activity Tick( Actor self )
		{
			var mobile = self.Trait<Mobile>();
			mobile.PxPosition = length > 1
				? int2.Lerp(startLocation, endLocation, ticks, length - 1)
				: endLocation;

			if (++ticks >= length)
			{
				mobile.IsMoving = false;
				return NextActivity;
			}

			mobile.IsMoving = true;
			return this;
		}

		public override IEnumerable<Target> GetTargets( Actor self )
		{
			yield return Target.FromPos(endLocation);
		}

		// Cannot be cancelled
		public override void Cancel( Actor self ) { }
	}
}
