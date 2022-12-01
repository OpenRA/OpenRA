#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class Nudge : Activity
	{
		readonly Actor nudger;
		public Nudge(Actor nudger)
		{
			this.nudger = nudger;
		}

		protected override void OnFirstRun(Actor self)
		{
			var move = self.Trait<IMove>();
			if (move is Mobile mobile)
			{
				if (mobile.IsTraitDisabled || mobile.IsTraitPaused || mobile.IsImmovable)
					return;

				var cell = mobile.GetAdjacentCell(nudger.Location);
				if (cell != null)
					QueueChild(mobile.MoveTo(cell.Value, 0, targetLineColor: mobile.Info.TargetLineColor));
			}
			else if (move is Aircraft aircraft)
			{
				if (aircraft.IsTraitDisabled || aircraft.IsTraitPaused || aircraft.RequireForceMove)
					return;

				// Disable nudging if the aircraft is outside the map.
				if (!self.World.Map.Contains(self.Location))
					return;

				var offset = new WVec(0, -self.World.SharedRandom.Next(512, 2048), 0)
					.Rotate(WRot.FromFacing(self.World.SharedRandom.Next(256)));

				var target = Target.FromPos(self.CenterPosition + offset);
				QueueChild(new Fly(self, target, targetLineColor: aircraft.Info.TargetLineColor));
				aircraft.UnReserve();
			}
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;

			yield break;
		}
	}
}
