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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HasUnitOnBuildInfo : ITraitInfo
	{
		public readonly string Unit = null;
		public readonly string InitialActivity = null;
		public readonly int2 SpawnOffset = int2.Zero;
		public readonly int Facing = 0;
		
		public object Create( Actor self ) { return new HasUnitOnBuild(self); }
	}

	public class HasUnitOnBuild
	{
		
		public HasUnitOnBuild(Actor self)
		{
			var info = self.Info.Traits.Get<HasUnitOnBuildInfo>();
			
			self.World.AddFrameEndTask(
				w =>
				{
					var unit = w.CreateActor(info.Unit, self.Location 
						+ info.SpawnOffset, self.Owner);
					var unitTrait = unit.traits.Get<Unit>();
					unitTrait.Facing = info.Facing;

					if (info.InitialActivity != null)
						unit.QueueActivity(Game.CreateObject<IActivity>(info.InitialActivity));
				});
		}
	}
}
