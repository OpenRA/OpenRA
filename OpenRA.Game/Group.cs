#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
