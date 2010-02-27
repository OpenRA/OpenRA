#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits.Activities
{
	class HeliReturn : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;

		static Actor ChooseHelipad(Actor self)
		{
			return self.World.Queries.OwnedBy[self.Owner].FirstOrDefault(
				a => a.Info.Name == "hpad" &&
					!Reservable.IsReserved(a));
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var dest = ChooseHelipad(self);

			var initialFacing = self.Info.Traits.Get<UnitInfo>().InitialFacing;

			if (dest == null)
				return Util.SequenceActivities(
					new Turn(initialFacing), 
					new HeliLand(true),
					NextActivity);

			var res = dest.traits.GetOrDefault<Reservable>();
			if (res != null)
				self.traits.Get<Helicopter>().reservation = res.Reserve(self);

			var pi = dest.Info.Traits.GetOrDefault<ProductionInfo>();
			var offset = pi != null ? pi.SpawnOffset : null;
			var offsetVec = offset != null ? new float2(offset[0], offset[1]) : float2.Zero;

			return Util.SequenceActivities(
				new HeliFly(dest.CenterLocation + offsetVec),
				new Turn(initialFacing),
				new HeliLand(false),
				new Rearm(),
				NextActivity);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
