using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	abstract class Order
	{
		public abstract void Apply( Game game );
	}

	class MoveOrder : Order
	{
		public readonly Unit Unit;
		public readonly int2 Destination;

		public MoveOrder(Unit unit, int2 destination)
		{
			this.Unit = unit;
			this.Destination = destination;
		}

		public override void Apply( Game game )
		{
			Unit.nextOrder = UnitMissions.Move( Unit, Destination );
		}
	}

	class DeployMcvOrder : Order
	{
		Unit unit;

		public DeployMcvOrder( Unit unit )
		{
			this.unit = unit;
		}

		public override void Apply( Game game )
		{
			unit.nextOrder = UnitMissions.Deploy( unit );
		}
	}

	class HarvestOrder : Order
	{
		Unit unit;

		public HarvestOrder( Unit unit )
		{
			this.unit = unit;
		}

		public override void Apply( Game game )
		{
			unit.nextOrder = UnitMissions.Harvest( unit );
		}
	}
}
