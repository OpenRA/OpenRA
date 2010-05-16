using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
