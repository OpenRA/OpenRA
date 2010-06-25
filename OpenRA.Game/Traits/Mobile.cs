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
using System;
using OpenRA.GameRules;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class MobileInfo : ITraitInfo, ITraitPrerequisite<UnitInfo>
	{
		public readonly TerrainType[] TerrainTypes;
		public readonly float[] TerrainSpeeds;
		public readonly int WaitAverage = 60;
		public readonly int WaitSpread = 20;

		public virtual object Create(ActorInitializer init) { return new Mobile(init, this); }
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOccupySpace, IMove
	{
		public readonly Actor self;
		public readonly Dictionary<TerrainType,float> TerrainCost;
		public readonly Dictionary<TerrainType,float> TerrainSpeed;

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

		public Mobile(ActorInitializer init, MobileInfo info)
		{
			this.self = init.self;
			this.__fromCell = this.__toCell = init.location;
			AddInfluence();
			
			TerrainCost = new Dictionary<TerrainType, float>();
			TerrainSpeed = new Dictionary<TerrainType, float>();
			
			if (info.TerrainTypes.Count() != info.TerrainSpeeds.Count())
				throw new InvalidOperationException("Mobile TerrainType/TerrainSpeed length missmatch");
			
			for (int i = 0; i < info.TerrainTypes.Count(); i++)
			{
				TerrainCost.Add(info.TerrainTypes[i], 1f/info.TerrainSpeeds[i]);
				TerrainSpeed.Add(info.TerrainTypes[i], info.TerrainSpeeds[i]);
			}
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
			if (MovementSpeedForCell(self, self.Location) == 0) return null;		/* allow disabling move orders from modifiers */
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

		public bool CanEnterCell(int2 p)
		{
			return CanEnterCell(p, null, true);
		}
		
		public virtual bool CanEnterCell(int2 cell, Actor ignoreActor, bool checkTransientActors)
		{
			if (!self.World.WorldActor.traits.Get<BuildingInfluence>().CanMoveHere(cell, ignoreActor))
				return false;
			
			if (checkTransientActors)
			{
				var canShare = self.traits.Contains<SharesCell>();
				var actors = self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(cell).Where(a => a != self && a != ignoreActor);
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
			
			return MovementCostForCell(self, cell) < float.PositiveInfinity;
		}
		
		public virtual float MovementCostForCell(Actor self, int2 cell)
		{
			if (!self.World.Map.IsInMap(cell.X,cell.Y))
				return float.PositiveInfinity;
			
			// Custom terrain types don't stack: pick one
			var customTerrain = self.World.WorldActor.traits.WithInterface<ITerrainTypeModifier>()
				.Where( t => t.GetTerrainType(cell) != null )
				.Select( t => t.GetTerrainType(cell) )
				.FirstOrDefault();
			
			// Todo: Hack this until i finish migrating TerrainType to a string
			var type = (customTerrain != null) ? (TerrainType)Enum.Parse(typeof(TerrainType), customTerrain) : self.World.TileSet.GetTerrainType(self.World.Map.MapTiles[cell.X, cell.Y]);
			var additionalCost = self.World.WorldActor.traits.WithInterface<ITerrainCost>()
				.Select( t => t.GetTerrainCost(cell, self) ).Sum();
			
			return TerrainCost[type] + additionalCost;
		}

		public virtual float MovementSpeedForCell(Actor self, int2 cell)
		{		
			var unitInfo = self.Info.Traits.GetOrDefault<UnitInfo>();
			if( unitInfo == null )
			   return 0f;
			
			// Custom terrain types don't stack: pick one
			var customTerrain = self.World.WorldActor.traits.WithInterface<ITerrainTypeModifier>()
				.Where( t => t.GetTerrainType(cell) != null )
				.Select( t => t.GetTerrainType(cell) )
				.FirstOrDefault();
			
			// Todo: Hack this until i finish migrating TerrainType to a string
			var type = (customTerrain != null) ? (TerrainType)Enum.Parse(typeof(TerrainType), customTerrain) : self.World.TileSet.GetTerrainType(self.World.Map.MapTiles[cell.X, cell.Y]);

			var modifier = self.traits
				.WithInterface<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return unitInfo.Speed * TerrainSpeed[type] * modifier;
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
