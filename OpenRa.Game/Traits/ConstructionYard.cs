using OpenRa.GameRules;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class ConstructionYardInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ConstructionYard(self); }
	}

	class ConstructionYard : IIssueOrder, IResolveOrder, IMovement
	{
		readonly Actor self;

		public ConstructionYard(Actor self)
		{
			this.self = self;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (!Rules.General.MCVUndeploy) return null;
			
			if (mi.Button == MouseButton.Left) return null;
		
			if (underCursor != null)
			{
				// force-move
				if (!mi.Modifiers.HasModifier(Modifiers.Alt)) return null;
				if (!self.World.IsActorCrushableByActor(underCursor, self)) return null;
			}

			return new Order("Move", self, null, xy, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new UndeployMcv());
			}
		}
	
		// HACK: This should make reference to an MCV actor, and use of its Mobile trait
		public UnitMovementType GetMovementType()
		{
			return UnitMovementType.Wheel;
		}
		
		public bool CanEnterCell(int2 a)
		{
			if (!self.World.BuildingInfluence.CanMoveHere(a)) return false;

			var crushable = true;
			foreach (Actor actor in self.World.UnitInfluence.GetUnitsAt(a))
			{
				if (actor == self) continue;
				
				if (!self.World.IsActorCrushableByActor(actor, self))
				{
					crushable = false;
					break;
				}
			}
			
			if (!crushable) return false;
			
			return self.World.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(GetMovementType(),
					self.World.TileSet.GetWalkability(self.World.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}
	}
}
