using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Mobile : IOrder, IOccupySpace, IMovement
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

		public void TeleportTo(Actor self, int2 xy)
		{
			fromCell = toCell = xy;
			self.CenterLocation = Util.CenterOfCell(fromCell);
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (underCursor != null) return null;
			if (Util.GetEffectiveSpeed(self) == 0) return null;		/* allow disabling move orders from modifiers */
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
			return (fromCell == toCell)
				? new[] { fromCell } 
				: new[] { fromCell, toCell };
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
					return UnitMovementType.Fly;
				default:
					throw new InvalidOperationException("GetMovementType on unit that shouldn't be aable to move.");
			}
		}
		
		public bool CanEnterCell(int2 a)
		{
			if (Game.BuildingInfluence.GetBuildingAt(a) != null) return false;

			var actors = Game.UnitInfluence.GetUnitsAt(a);
			var crushable = true;
			foreach (Actor actor in actors)
			{
				if (actor == self) continue;
				
				var c = actor.traits.WithInterface<ICrushable>();
				if (c == null)
				{
					crushable = false;
					break;
				}
				
				foreach (var crush in c)
				{
					// TODO: Unhack this. I can't wrap my head around this right now...
					if (!(((crush.IsCrushableByEnemy() && actor.Owner != Game.LocalPlayer) || (crush.IsCrushableByFriend() && actor.Owner == Game.LocalPlayer))
						&& crush.CrushableBy().Contains(GetMovementType())))
					{
						crushable = false;
						Log.Write("{0} is NOT crushable by {1} (mobile)", actor.Info.Name, self.Info.Name);
						break;
					}
				}
				Log.Write("{0} is crushable by {1} (mobile)", actor.Info.Name, self.Info.Name);
			}
			
			if (!crushable) return false;
			
			return Rules.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(GetMovementType(),
					Rules.TileSet.GetWalkability(Rules.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public IEnumerable<int2> GetCurrentPath()
		{
			var move = self.GetCurrentActivity() as Activities.Move;
			if (move == null || move.path == null) return new int2[] { };
			return Enumerable.Reverse(move.path);
		}
	}
}
