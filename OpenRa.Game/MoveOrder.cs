using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface IOrder
	{
		void Apply( Game game );
	}

	class MoveOrder : IOrder
	{
		public readonly Unit Unit;
		public readonly int2 Destination;

		public MoveOrder(Unit unit, int2 destination)
		{
			this.Unit = unit;
			this.Destination = destination;
		}

		public void Apply( Game game )
		{
			Unit.AcceptMoveOrder( Destination );
		}
	}

	class DeployMcvOrder : IOrder
	{
		public Mcv Mcv;

		public DeployMcvOrder( Mcv mcv )
		{
			this.Mcv = mcv;
		}

		public void Apply( Game game )
		{
			Mcv.AcceptDeployOrder();
		}
	}
}
