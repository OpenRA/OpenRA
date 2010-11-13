using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
    public class DeployOrderTargeter : IOrderTargeter
	{
		readonly Func<bool> useDeployCursor;

		public DeployOrderTargeter( string order, int priority )
			: this( order, priority, () => true )
		{
		}

		public DeployOrderTargeter( string order, int priority, Func<bool> useDeployCursor )
		{
			this.OrderID = order;
			this.OrderPriority = priority;
			this.useDeployCursor = useDeployCursor;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; private set; }

		public bool CanTargetUnit( Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor )
		{
			IsQueued = forceQueued;
			cursor = useDeployCursor() ? "deploy" : "deploy-blocked";
			return self == target;
		}

		public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
		{
			return false;
		}

		public bool IsQueued { get; protected set; }
	}
}
