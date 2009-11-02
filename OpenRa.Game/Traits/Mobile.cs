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

		public int2 fromCell;
		public int2 toCell { get { return self.Location; } set { self.Location = value; } }
		public int facing;

		public int Voice = Game.CosmeticRandom.Next(2);
		Activity currentActivity;

		public Mobile(Actor self)
		{
			this.self = self;
			fromCell = toCell;
			Game.UnitInfluence.Update( this );
		}

		public void QueueActivity( Activity nextActivity )
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

		public void InternalSetActivity( Activity activity )
		{
			currentActivity = activity;
		}

		public void Tick(Actor self)
		{
			if( currentActivity != null )
				currentActivity.Tick( self, this );
			else
				fromCell = toCell;
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
			var vi = self.unitInfo as UnitInfo.VehicleInfo;
			if (vi == null) return UnitMovementType.Foot;
			if (vi.WaterBound) return UnitMovementType.Float;
			return vi.Tracked ? UnitMovementType.Track : UnitMovementType.Wheel;
		}

		public IEnumerable<int2> GetCurrentPath()
		{
			var move = currentActivity as Traits.Activities.Move;
			if (move == null || move.path == null) return new int2[] { };
			return Enumerable.Reverse(move.path);
		}
	}
}
