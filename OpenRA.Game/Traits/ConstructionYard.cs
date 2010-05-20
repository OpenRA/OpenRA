#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits.Activities;

namespace OpenRA.Traits
{
	class ConstructionYardInfo : ITraitInfo
	{
		public readonly bool AllowUndeploy = true;

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
			if (!self.Info.Traits.Get<ConstructionYardInfo>().AllowUndeploy) return null;
			
			if (mi.Button == MouseButton.Left) return null;
		
			if (underCursor != null)
			{
				// force-move
				if (!mi.Modifiers.HasModifier(Modifiers.Alt)) return null;
				if (!self.World.IsActorCrushableByActor(underCursor, self)) return null;
			}
			if (self.traits.GetOrDefault<IMovement>().CanEnterCell(xy))
				return new Order("Move", self, xy);
			else 
				return null;
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
		public UnitMovementType GetMovementType() { return UnitMovementType.Wheel; }
		
		public bool CanEnterCell(int2 a)
		{
			if (!self.World.WorldActor.traits.Get<BuildingInfluence>().CanMoveHere(a)) return false;

			var crushable = true;
			foreach (Actor actor in self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(a))
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
				Rules.TerrainTypes[self.World.TileSet.GetTerrainType(self.World.Map.MapTiles[a.X, a.Y])]
				.GetCost(GetMovementType()) < float.PositiveInfinity;
		}
	}
}
