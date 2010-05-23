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
using System.Linq;

namespace OpenRA
{
	public class Group
	{
		List<Actor> actors;
		int id;

		static int nextGroup;

		public IEnumerable<Actor> Actors { get { return actors; } }
		
		public Group(IEnumerable<Actor> actors)
		{
			this.actors = actors.ToList();

			foreach (var a in actors)
				a.Group = this;

			id = nextGroup++;
		}

		public void Dump()
		{
			/* debug crap */
			Game.Debug("Group #{0}: {1}".F(
				id, string.Join(",", actors.Select(a => "#{0} {1}".F(a.ActorID, a.Info.Name)).ToArray())));
		}

		/* todo: add lazy group path crap, groupleader, pruning, etc */
	}
}
