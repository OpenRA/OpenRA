using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Mobile : IOrder, IOccupySpace
	{
		readonly Actor self;

		int2 __fromCell;
		public int2 fromCell
		{
			get { return __fromCell; }
			set { Game.UnitInfluence.Remove(self, this); __fromCell = value; Game.UnitInfluence.Add(self, this); }
		}
		public int2 toCell
		{
			get { return self.Location; }
			set { Game.UnitInfluence.Remove(self, this); self.Location = value; Game.UnitInfluence.Add(self, this); }
		}

		public Mobile(Actor self)
		{
			this.self = self;
			__fromCell = toCell;
			Game.UnitInfluence.Add(self, this);
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (underCursor != null) return null;
			if (xy == toCell) return null;
			return Order.Move(self, xy);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new Activities.Move(order.TargetLocation, 8));

				var attackBase = self.traits.WithInterface<AttackBase>().FirstOrDefault();
				if (attackBase != null)
					attackBase.target = null;	/* move cancels attack order */
			}
		}

		public IEnumerable<int2> OccupiedCells()
		{
			return new[] { fromCell, toCell };
		}

		public UnitMovementType GetMovementType()
		{
			switch (Rules.UnitCategory[self.Info.Name])
			{
				case "Infantry":
					return UnitMovementType.Foot;
				case "Vehicle":
					return (self.Info as VehicleInfo).Tracked ? UnitMovementType.Track : UnitMovementType.Wheel;
				case "Ship":
					return UnitMovementType.Float;
				case "Plane":
					return UnitMovementType.Track; // FIXME: remove this when planes actually fly.
				default:
					throw new InvalidOperationException("GetMovementType on unit that shouldn't be aable to move.");
			}
		}

		public IEnumerable<int2> GetCurrentPath()
		{
			var move = self.GetCurrentActivity() as Activities.Move;
			if (move == null || move.path == null) return new int2[] { };
			return Enumerable.Reverse(move.path);
		}
	}
}
