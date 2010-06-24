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
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class MobileInfo : ITraitInfo, ITraitPrerequisite<UnitInfo>
	{
		public readonly UnitMovementType MovementType = UnitMovementType.Wheel;
		public readonly int WaitAverage = 60;
		public readonly int WaitSpread = 20;

		public virtual object Create(ActorInitializer init) { return new Mobile(init); }
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOccupySpace, IMove
	{
		public readonly Actor self;

		[Sync]
		int2 __fromCell, __toCell;
		public int2 fromCell
		{
			get { return __fromCell; }
			set { SetLocation( value, __toCell ); }
		}
		[Sync]
		public int2 toCell
		{
			get { return __toCell; }
			set { SetLocation( __fromCell, value ); }
		}
		void SetLocation( int2 from, int2 to )
		{
			if( fromCell == from && toCell == to ) return;
			RemoveInfluence();
			__fromCell = from;
			__toCell = to;
			AddInfluence();
		}

		public Mobile(ActorInitializer init)
		{
			this.self = init.self;
			this.__fromCell = this.__toCell = init.location;
			AddInfluence();
		}

		public void SetPosition(Actor self, int2 cell)
		{
			SetLocation( cell, cell );
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
				if (!CanEnterCell(underCursor.Location, null, true)) return null;
			}
			var umt = self.Info.Traits.Get<MobileInfo>().MovementType;
			if (Util.GetEffectiveSpeed(self,umt) == 0) return null;		/* allow disabling move orders from modifiers */
			if (xy == toCell) return null;
			return new Order("Move", self, xy, mi.Modifiers.HasModifier(Modifiers.Shift));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				if (self.traits.GetOrDefault<IMove>().CanEnterCell(order.TargetLocation))
				{
					if( !order.Queued ) self.CancelActivity();
					self.QueueActivity(new Activities.Move(order.TargetLocation, 8));
				}
			}
		}

		public int2 TopLeft { get { return toCell; } }

		public virtual IEnumerable<int2> OccupiedCells()
		{
			return (fromCell == toCell)
				? new[] { fromCell }
				: CanEnterCell(toCell)
					? new[] { toCell }
					: new[] { fromCell, toCell };
		}

		public UnitMovementType GetMovementType()
		{
			return self.Info.Traits.Get<MobileInfo>().MovementType;			
		}
		
		public bool CanEnterCell(int2 p)
		{
			return CanEnterCell(p, null, true);
		}
		
		public virtual bool CanEnterCell(int2 p, Actor ignoreActor, bool checkTransientActors)
		{
			if (!self.World.WorldActor.traits.Get<BuildingInfluence>().CanMoveHere(p, ignoreActor))
				return false;
			
			if (checkTransientActors)
			{
				var canShare = self.traits.Contains<SharesCell>();
				var actors = self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(p).Where(a => a != self && a != ignoreActor);
				var nonshareable = actors.Where(a => !(canShare && a.traits.Contains<SharesCell>()));
				var shareable = actors.Where(a => canShare && a.traits.Contains<SharesCell>());
				
				// only allow 5 in a cell
				if (shareable.Count() >= 5)
					return false;
				
				// We can enter a cell with nonshareable units if we can crush all of them
				if (nonshareable.Any(
					a => !self.World.IsActorCrushableByActor(a, self)))
					return false;
			}
			
			return self.World.Map.IsInMap(p.X, p.Y) &&
				Rules.TerrainTypes[self.World.TileSet.GetTerrainType(self.World.Map.MapTiles[p.X, p.Y])]
				.GetCost(GetMovementType()) < float.PositiveInfinity;
		}

		public IEnumerable<float2> GetCurrentPath(Actor self)
		{
			var move = self.GetCurrentActivity() as Activities.Move;
			if (move == null || move.path == null) return new float2[] { };
			
			return Enumerable.Reverse(move.path).Select( c => Util.CenterOfCell(c) );
		}
		
		public virtual void AddInfluence()
		{
			self.World.WorldActor.traits.Get<UnitInfluence>().Add( self, this );
		}
		
		public virtual void RemoveInfluence()
		{
			self.World.WorldActor.traits.Get<UnitInfluence>().Remove( self, this );
		}
	}
}
