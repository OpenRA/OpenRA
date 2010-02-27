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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public class MobileInfo : ITraitInfo
	{
		public readonly UnitMovementType MovementType = UnitMovementType.Wheel;

		public object Create(Actor self) { return new Mobile(self); }
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOccupySpace, IMovement
	{
		readonly Actor self;

		[Sync]
		int2 __fromCell;
		public int2 fromCell
		{
			get { return __fromCell; }
			set { self.World.WorldActor.traits.Get<UnitInfluence>().Remove(self, this); __fromCell = value; self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this); }
		}
		public int2 toCell
		{
			get { return self.Location; }
			set
			{
				if (self.Location != value)
				{
					self.World.WorldActor.traits.Get<UnitInfluence>().Remove(self, this);
					self.Location = value;
					self.Owner.Shroud.Explore(self);
				}
				self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this);
			}
		}

		public Mobile(Actor self)
		{
			this.self = self;
			__fromCell = toCell;
			self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this);
		}

		public void TeleportTo(Actor self, int2 xy)
		{
			fromCell = toCell = xy;
			self.CenterLocation = Util.CenterOfCell(fromCell);
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			// force-fire should *always* take precedence over move.
			if (mi.Modifiers.HasModifier(Modifiers.Ctrl)) return null;
		
			if (underCursor != null && underCursor.Owner != null)
			{
				// force-move
				if (!mi.Modifiers.HasModifier(Modifiers.Alt)) return null;
				if (!self.World.IsActorCrushableByActor(underCursor, self)) return null;
			}

			if (Util.GetEffectiveSpeed(self) == 0) return null;		/* allow disabling move orders from modifiers */
			if (xy == toCell) return null;
			return new Order("Move", self, xy);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				if (self.traits.GetOrDefault<IMovement>().CanEnterCell(order.TargetLocation))
				{
					self.CancelActivity();
					self.QueueActivity(new Activities.Move(order.TargetLocation, 8));
				}
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
			return self.Info.Traits.Get<MobileInfo>().MovementType;			
		}
		
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
				TerrainCosts.Cost(GetMovementType(),
					self.World.TileSet.GetWalkability(self.World.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public IEnumerable<int2> GetCurrentPath()
		{
			var move = self.GetCurrentActivity() as Activities.Move;
			if (move == null || move.path == null) return new int2[] { };
			return Enumerable.Reverse(move.path);
		}
	}
}
