#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from TakeOff.cs and pretty much the same.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.yupgi_alert.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Activities
{
	public class SpawnedTakeOff : Activity
	{
		readonly Aircraft aircraft;
		readonly IMove move;
		readonly Actor self;

		public SpawnedTakeOff(Actor self)
		{
			this.self = self;
			aircraft = self.Trait<Aircraft>();
			move = self.Trait<IMove>();
		}

		public override Activity Tick(Actor self)
		{
			aircraft.UnReserve();

			var host = self.Trait<Spawned>().Master;

			var destination = self.World.Map.CellContaining(host.CenterPosition);

			if (NextActivity == null)
				return new AttackMoveActivity(self, move.MoveTo(destination, 1));
			else
				return NextActivity;
		}
	}
}
