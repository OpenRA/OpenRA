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
				if (!Game.IsActorCrushableByActor(underCursor, self)) return null;
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
			if (!Game.BuildingInfluence.CanMoveHere(a)) return false;

			var crushable = true;
			foreach (Actor actor in Game.UnitInfluence.GetUnitsAt(a))
			{
				if (actor == self) continue;
				
				if (!Game.IsActorCrushableByActor(actor, self))
				{
					crushable = false;
					break;
				}
			}
			
			if (!crushable) return false;
			
			return Game.world.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(GetMovementType(),
					Game.world.TileSet.GetWalkability(Game.world.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}
	}
}
