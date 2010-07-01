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
using System.Linq;
namespace OpenRA.Mods.RA
{
	public class FreeActorInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string Actor = null;
		public readonly string InitialActivity = null;
		public readonly int2 SpawnOffset = int2.Zero;
		public readonly int Facing = 0;
		
		public object Create( ActorInitializer init ) { return new FreeActor(init.self, this); }
	}

	public class FreeActor
	{
		public FreeActor(Actor self, FreeActorInfo info)
		{			
			self.World.AddFrameEndTask(
				w =>
				{
					var a = w.CreateActor(info.Actor, self.Location 
						+ info.SpawnOffset, self.Owner);
					var unit = a.traits.WithInterface<Unit>().FirstOrDefault();
					
					if (unit != null)
						unit.Facing = info.Facing;

					if (info.InitialActivity != null)
						a.QueueActivity(Game.CreateObject<IActivity>(info.InitialActivity));
				});
		}
	}
}
