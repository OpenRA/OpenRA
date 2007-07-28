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
			Unit.nextOrder = UnitMissions.Move( Unit, Destination );
		}
	}

	class DeployMcvOrder : IOrder
	{
		Unit unit;

		public DeployMcvOrder( Unit unit )
		{
			this.unit = unit;
		}

		public void Apply( Game game )
		{
			unit.nextOrder = UnitMissions.Deploy( unit );
		}
	}

	class HarvestOrder : IOrder
	{
		Unit unit;

		public HarvestOrder( Unit unit )
		{
			this.unit = unit;
		}

		public void Apply( Game game )
		{
			unit.nextOrder = UnitMissions.Harvest( unit );
		}
	}
}
