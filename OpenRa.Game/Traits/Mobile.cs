using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Mobile : ITick, IOrder
	{
		public Actor self;

		int2 __fromCell;
		public int2 fromCell { get { return __fromCell; } set { Game.UnitInfluence.Remove( this ); __fromCell = value; Game.UnitInfluence.Add( this ); } }
		public int2 toCell { get { return self.Location; } set { Game.UnitInfluence.Remove( this ); self.Location = value; Game.UnitInfluence.Add( this ); } }

		public int Voice = Game.CosmeticRandom.Next(2);
		IActivity currentActivity;

		public Mobile(Actor self)
		{
			this.self = self;
			fromCell = toCell;
			Game.UnitInfluence.Update( this );
		}

		public void QueueActivity( IActivity nextActivity )
		{
			if( currentActivity == null )
			{
				currentActivity = nextActivity;
				return;
			}
			var act = currentActivity;
			while( act.NextActivity != null )
			{
				act = act.NextActivity;
			}
			act.NextActivity = nextActivity;
		}

		public void Tick(Actor self)
		{
			if( currentActivity == null )
			{
				fromCell = toCell;
				return;
			}

			var nextActivity = currentActivity;
			while( nextActivity != null )
			{
				currentActivity = nextActivity;
				nextActivity = nextActivity.Tick( self, this );
			}
		}

		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if( lmb ) return null;

			if( underCursor != null )
				return null;

			if (xy == toCell) return null;

			return OpenRa.Game.Order.Move( self, xy, 
				!Game.IsCellBuildable(xy, GetMovementType()) );
		}

		public void Cancel(Actor self)
		{
			if (currentActivity != null)
				currentActivity.Cancel(self, this);
		}

		public IEnumerable<int2> OccupiedCells()
		{
			return new[] { fromCell, toCell };
		}

		public UnitMovementType GetMovementType()
		{
			switch( Rules.UnitCategory[ self.unitInfo.Name ] )
			{
			case "Infantry":
				return UnitMovementType.Foot;
			case "Vehicle":
				return ( self.unitInfo as UnitInfo.VehicleInfo ).Tracked ? UnitMovementType.Track : UnitMovementType.Wheel;
			case "Ship":
				return UnitMovementType.Float;
			case "Plane":
				return UnitMovementType.Track; // FIXME: remove this when planes actually fly.
			default:
				throw new InvalidOperationException( "GetMovementType on unit that shouldn't be aable to move." );
			}
		}

		public IEnumerable<int2> GetCurrentPath()
		{
			var move = currentActivity as Traits.Activities.Move;
			if (move == null || move.path == null) return new int2[] { };
			return Enumerable.Reverse(move.path);
		}

		public bool HasActivity { get { return currentActivity != null; } }
	}
}
