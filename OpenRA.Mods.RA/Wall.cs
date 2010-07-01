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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class WallInfo : ITraitInfo
	{
		public readonly string[] CrushClasses = { };

		public object Create(ActorInitializer init) { return new Wall(init.self, this); }
	}

	public class Wall : ICrushable, IBlocksBullets
	{
		readonly Actor self;
		readonly WallInfo info;
		
		public Wall(Actor self, WallInfo info)
		{
			this.self = self;
			this.info = info;
			self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, self.traits.Get<Building>());
		}
		
		public IEnumerable<string> CrushClasses { get { return info.CrushClasses; } }
		public void OnCrush(Actor crusher) { self.InflictDamage(crusher, self.Health, null); }
	}
}
