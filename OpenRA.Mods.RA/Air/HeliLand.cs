#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class HeliLand : Activity
	{
		bool requireSpace;

		public HeliLand(bool requireSpace)
		{
			this.requireSpace = requireSpace;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var helicopter = self.Trait<Helicopter>();

			if (requireSpace && !helicopter.CanLand(self.Location))
				return this;

			var centerOfCell = self.World.Map.CenterOfCell(self.Location);
			var pos = helicopter.CenterPosition;
			if ((requireSpace && (pos.X != centerOfCell.X || pos.Y != centerOfCell.Y))) // If requireSpace, then require center of cell
				return Util.SequenceActivities(new AttackMove.AttackMoveActivity(self, self.Trait<IMove>().MoveTo(self.Location, 1)), this, NextActivity);

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.LandAltitude))
				return this;

			return NextActivity;
		}
	}
}
