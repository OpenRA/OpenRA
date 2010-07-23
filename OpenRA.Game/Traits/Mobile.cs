#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class MobileInfo : ITraitInfo, ITraitPrerequisite<UnitInfo>
	{
		public readonly string[] TerrainTypes;
		public readonly float[] TerrainSpeeds;
		public readonly string[] Crushes;
		public readonly int WaitAverage = 60;
		public readonly int WaitSpread = 20;

		public virtual object Create(ActorInitializer init) { return new Mobile(init, this); }
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOccupySpace, IMove, IOrderCursor, INudge
	{
		public readonly Actor self;
		public readonly MobileInfo Info;
		public readonly Dictionary<string,float> TerrainCost;
		public readonly Dictionary<string,float> TerrainSpeed;

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
			this.Info = info;
			this.__fromCell = this.__toCell = init.location;
			AddInfluence();
			
			TerrainCost = new Dictionary<string, float>();
			TerrainSpeed = new Dictionary<string, float>();
			
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
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Move")
				return null;
			
			return (CanEnterCell(order.TargetLocation)) ? "move" : "move-blocked";
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
			// Check for buildings
			var building = self.World.WorldActor.traits.Get<BuildingInfluence>().GetBuildingBlocking(cell);
			if (building != null && building != ignoreActor)
			{
				if (Info.Crushes == null)
					return false;
				
				var crushable = building.traits.WithInterface<ICrushable>();
				if (crushable.Count() == 0)
					return false;
				
				if (!crushable.Any(b => b.CrushClasses.Intersect(Info.Crushes).Any()))
					return false;
			}
			
			// Check mobile actors
			if (checkTransientActors)
			{
				var canShare = self.traits.Contains<SharesCell>();
				var actors = self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(cell).Where(a => a != self && a != ignoreActor);
				var nonshareable = actors.Where(a => !(canShare && a.traits.Contains<SharesCell>()));
				var shareable = actors.Where(a => canShare && a.traits.Contains<SharesCell>());
				
				// only allow 5 in a cell
				if (shareable.Count() >= 5)
					return false;

				// We can enter a cell with nonshareable units only if we can crush all of them
				if (Info.Crushes == null && nonshareable.Count() > 0)
					return false;

				if (nonshareable.Any(a => !(a.traits.Contains<ICrushable>() &&
						                     a.traits.WithInterface<ICrushable>().Any(b => b.CrushClasses.Intersect(Info.Crushes).Any()))))
					return false;
			}
			
			return MovementCostForCell(self, cell) < float.PositiveInfinity;
		}
		
		public virtual void FinishedMoving(Actor self)
		{
			var crushable = self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(self.Location).Where(a => a != self && a.traits.Contains<ICrushable>());
			foreach (var a in crushable)
			{
				var crushActions = a.traits.WithInterface<ICrushable>().Where(b => b.CrushClasses.Intersect(Info.Crushes).Any());
				foreach (var b in crushActions)
					b.OnCrush(self);
			}
		}
		
		public virtual float MovementCostForCell(Actor self, int2 cell)
		{
			if (!self.World.Map.IsInMap(cell.X,cell.Y))
				return float.PositiveInfinity;

			var type = self.World.GetTerrainType(cell);
			var additionalCost = self.World.WorldActor.traits.WithInterface<ITerrainCost>()
				.Select( t => t.GetTerrainCost(cell, self) ).Sum();
			
			return TerrainCost[type] + additionalCost;
		}

		public virtual float MovementSpeedForCell(Actor self, int2 cell)
		{		
			var unitInfo = self.Info.Traits.GetOrDefault<UnitInfo>();
			if( unitInfo == null )
			   return 0f;

			var type = self.World.GetTerrainType(cell);

			var modifier = self.traits
				.WithInterface<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return unitInfo.Speed * TerrainSpeed[type] * modifier;
		}
		
		public IEnumerable<float2> GetCurrentPath(Actor self)
		{
			var move = self.GetCurrentActivity() as Move;
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

		public void OnNudge(Actor self, Actor nudger)
		{
			/* initial fairly braindead implementation. */

			if (self.Owner.Stances[nudger.Owner] != Stance.Ally)
				return;		/* don't allow ourselves to be pushed around
							 * by the enemy! */

			if (!self.IsIdle)
				return;		/* don't nudge if we're busy doing something! */

			// pick an adjacent available cell.
			var availCells = new List<int2>();
			var notStupidCells = new List<int2>();

			for( var i = -1; i < 2; i++ )
				for (var j = -1; j < 2; j++)
				{
					var p = self.Location + new int2(i, j);
					if (CanEnterCell(p))
						availCells.Add(p);
					else
						if (p != nudger.Location && p != self.Location)
							notStupidCells.Add(p);
				}

			var moveTo = availCells.Any() ? availCells.Random(self.World.SharedRandom) :
				notStupidCells.Any() ? notStupidCells.Random(self.World.SharedRandom) : (int2?)null;

			if (moveTo.HasValue)
			{
				self.CancelActivity();
				self.QueueActivity(new Move(moveTo.Value, 0));
			}
		}
	}
}
