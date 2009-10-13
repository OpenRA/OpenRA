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
		public readonly Actor Unit;
		public readonly int2 Destination;

		public MoveOrder( Actor unit, int2 destination )
		{
			this.Unit = unit;
			this.Destination = destination;
		}

		public override void Apply( Game game )
		{
			Unit.traits.Get<Traits.Mobile>().destination = Destination;
		}
	}

	class DeployMcvOrder : Order
	{
		Actor Unit;
		int2 Location;

		public DeployMcvOrder( Actor unit, int2 location )
		{
			Unit = unit;
			Location = location;
		}

		public override void Apply( Game game )
		{
			Unit.traits.Get<Traits.McvDeploy>().DeployLocation = Location;
			var mobile = Unit.traits.Get<Traits.Mobile>();
			mobile.destination = mobile.toCell;
		}
	}

	//class HarvestOrder : Order
	//{
	//    Unit unit;

	//    public HarvestOrder( Unit unit )
	//    {
	//        this.unit = unit;
	//    }

	//    public override void Apply( Game game )
	//    {
	//        unit.nextOrder = UnitMissions.Harvest( unit );
	//    }
	//}
}
