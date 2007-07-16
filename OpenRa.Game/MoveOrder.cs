using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface IOrder
	{
		void Apply();
	}

	class MoveOrder : IOrder
	{
		public readonly Mcv Unit;
		public readonly int2 Destination;

		public MoveOrder(Mcv unit, int2 destination)
		{
			this.Unit = unit;
			this.Destination = destination;
		}

		public void Apply()
		{
			Unit.AcceptMoveOrder( Destination );
		}
	}

	class DeployMcvOrder : IOrder
	{
		public Mcv Unit;

		public DeployMcvOrder( Mcv unit )
		{
			this.Unit = unit;
		}

		public void Apply()
		{
			Unit.AcceptDeployOrder();
		}
	}
}
