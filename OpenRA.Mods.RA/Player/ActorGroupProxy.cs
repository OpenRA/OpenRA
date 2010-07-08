using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ActorGroupProxyInfo : TraitInfo<ActorGroupProxy> { }

	class ActorGroupProxy : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "CreateGroup")
			{
				/* create a group */
				var actors = order.TargetString.Split(',')
					.Select(id => uint.Parse(id))
					.Select(id => self.World.Actors.FirstOrDefault(a => a.ActorID == id))
						.Where(a => a != null);

				var g = new Group(actors);
			//	g.Dump();
			}
		}
	}
}
