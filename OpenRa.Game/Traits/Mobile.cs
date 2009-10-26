using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class Mobile : ITick, IOrder
	{
		public Actor self;

		public int2 fromCell;
		public int2 toCell { get { return self.Location; } set { self.Location = value; } }
		public int facing;

		public int Voice = Game.CosmeticRandom.Next(2);
		CurrentActivity currentActivity;

		public Mobile(Actor self)
		{
			this.self = self;
			fromCell = toCell;
		}

		public void QueueActivity( CurrentActivity nextActivity )
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
			if( currentActivity != null )
				currentActivity.Tick( self, this );
			else
				fromCell = toCell;
		}

		public Order Order(Actor self, int2 xy, bool lmb)
		{
			if( lmb ) return null;

			if (xy != toCell)
				return new MoveOrder(self, xy);

			return null;
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
	
		public interface CurrentActivity
		{
			CurrentActivity NextActivity { get; set; }
			void Tick( Actor self, Mobile mobile );
			void Cancel( Actor self, Mobile mobile );
		}

		public class Turn : CurrentActivity
		{
			public CurrentActivity NextActivity { get; set; }

			public int desiredFacing;

			public Turn( int desiredFacing )
			{
				this.desiredFacing = desiredFacing;
			}

			public void Tick( Actor self, Mobile mobile )
			{
				if( desiredFacing == mobile.facing )
				{
					mobile.currentActivity = NextActivity;
					if( NextActivity != null )
						NextActivity.Tick( self, mobile );
					return;
				}
				Util.TickFacing( ref mobile.facing, desiredFacing, self.unitInfo.ROT );
			}

			public void Cancel( Actor self, Mobile mobile )
			{
				desiredFacing = mobile.facing;
				NextActivity = null;
			}
		}

		public class MoveTo : CurrentActivity
		{
			public CurrentActivity NextActivity { get; set; }

			int2 destination;
			List<int2> path;

			MovePart move;

			public MoveTo( int2 destination )
			{
				this.destination = destination;
			}

			static bool CanEnterCell(int2 c, Actor self)
			{
				var u = Game.UnitInfluence.GetUnitAt(c);
				return u == null || u == self;
			}

			public void Tick( Actor self, Mobile mobile )
			{
				if( move != null )
				{
					move.TickMove( self, mobile, this );
					return;
				}

				if( destination == self.Location )
				{
					mobile.currentActivity = NextActivity;
					return;
				}

				if( path == null )
					path = Game.pathFinder.FindUnitPath( self.Location, destination, mobile.GetMovementType() );
				if( path.Count == 0 )
				{
					destination = mobile.toCell;
					return;
				}

				var nextCell = path[ path.Count - 1 ];
				int2 dir = nextCell - mobile.fromCell;
				var firstFacing = Util.GetFacing( dir, mobile.facing );
				if( firstFacing != mobile.facing )
					mobile.currentActivity = new Turn( firstFacing ) { NextActivity = this };
				else
				{
					if (!CanEnterCell(nextCell, self)) return;	/* todo: repath, sometimes */

					mobile.toCell = nextCell;
					path.RemoveAt( path.Count - 1 );

					move = new MoveFirstHalf(
						CenterOfCell( mobile.fromCell ),
						BetweenCells( mobile.fromCell, mobile.toCell ),
						mobile.facing,
						mobile.facing,
						0 );

					Game.UnitInfluence.Update(mobile);
				}
				mobile.currentActivity.Tick( self, mobile );
			}

			static float2 CenterOfCell( int2 loc )
			{
				return new float2( 12, 12 ) + Game.CellSize * (float2)loc;
			}

			static float2 BetweenCells( int2 from, int2 to )
			{
				return 0.5f * ( CenterOfCell( from ) + CenterOfCell( to ) );
			}

			abstract class MovePart
			{
				public readonly float2 from, to;
				public readonly int fromFacing, toFacing;
				public int moveFraction;
				public readonly int moveFractionTotal;

				public MovePart( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
				{
					this.from = from;
					this.to = to;
					this.fromFacing = fromFacing;
					this.toFacing = toFacing;
					this.moveFraction = startingFraction;
					this.moveFractionTotal = (int)( to - from ).Length * ( 25 / 6 );
				}

				public void TickMove( Actor self, Mobile mobile, MoveTo parent )
				{
					var oldFraction = moveFraction;
					var oldTotal = moveFractionTotal;

					moveFraction += ( self.unitInfo as UnitInfo.MobileInfo ).Speed;
					UpdateCenterLocation( self, mobile );
					if( moveFraction >= moveFractionTotal )
					{
						parent.move = OnComplete( self, mobile, parent );
						if( parent.move == null )
							UpdateCenterLocation( self, mobile );
					}
				}

				void UpdateCenterLocation( Actor self, Mobile mobile )
				{
					var frac = (float)moveFraction / moveFractionTotal;

					self.CenterLocation = float2.Lerp( from, to, frac );
					if( moveFraction >= moveFractionTotal )
						mobile.facing = toFacing & 0xFF;
					else
						mobile.facing = ( fromFacing + ( toFacing - fromFacing ) * moveFraction / moveFractionTotal ) & 0xFF;
				}

				protected abstract MovePart OnComplete( Actor self, Mobile mobile, MoveTo parent );
			}

			class MoveFirstHalf : MovePart
			{
				public MoveFirstHalf( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
					: base( from, to, fromFacing, toFacing, startingFraction )
				{
				}

				protected override MovePart OnComplete( Actor self, Mobile mobile, MoveTo parent )
				{
					if( parent.path.Count > 0 )
					{
						var nextCell = parent.path[ parent.path.Count - 1 ];
						if( ( nextCell - mobile.toCell ) != ( mobile.toCell - mobile.fromCell ) )
						{
							if( CanEnterCell( nextCell, self ) )
							{
								parent.path.RemoveAt( parent.path.Count - 1 );

								var ret = new MoveFirstHalf(
									BetweenCells( mobile.fromCell, mobile.toCell ),
									BetweenCells( mobile.toCell, nextCell ),
									mobile.facing,
									Util.GetNearestFacing( mobile.facing, Util.GetFacing( nextCell - mobile.toCell, mobile.facing ) ),
									moveFraction - moveFractionTotal );
								mobile.fromCell = mobile.toCell;
								mobile.toCell = nextCell;
								Game.UnitInfluence.Update( mobile );
								return ret;
							}
						}
					}
					var ret2 = new MoveSecondHalf(
						BetweenCells( mobile.fromCell, mobile.toCell ),
						CenterOfCell( mobile.toCell ),
						mobile.facing,
						mobile.facing,
						moveFraction - moveFractionTotal );
					mobile.fromCell = mobile.toCell;
					return ret2;
				}
			}

			class MoveSecondHalf : MovePart
			{
				public MoveSecondHalf( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
					: base( from, to, fromFacing, toFacing, startingFraction )
				{
				}

				protected override MovePart OnComplete( Actor self, Mobile mobile, MoveTo parent )
				{
					self.CenterLocation = CenterOfCell( mobile.toCell );
					mobile.fromCell = mobile.toCell;
					return null;
				}
			}

			public void Cancel( Actor self, Mobile mobile )
			{
				path.Clear();
				NextActivity = null;
			}
		}
	}
}
