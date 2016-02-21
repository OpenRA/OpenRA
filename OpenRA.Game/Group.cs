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
using System.Linq;

namespace OpenRA
{
	public class Group
	{
		readonly Actor[] actors;
		readonly int id;

		static int nextGroup;

		public IEnumerable<Actor> Actors { get { return actors; } }

		public Group(IEnumerable<Actor> actors)
		{
			this.actors = actors.ToArray();

			foreach (var a in actors)
				a.Group = this;

			id = nextGroup++;
		}

		public void Dump()
		{
			/* debug crap */
			Game.Debug("Group #{0}: {1}".F(
				id, actors.Select(a => "#{0} {1}".F(a.ActorID, a.Info.Name)).JoinWith(",")));
		}

		/* TODO: add lazy group path crap, groupleader, pruning, etc */
	}
}
