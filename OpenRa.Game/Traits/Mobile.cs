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
		CurrentAction currentAction;

		public Mobile(Actor self)
		{
			this.self = self;
			fromCell = toCell;
		}

		public void QueueAction( CurrentAction nextAction )
		{
			if( currentAction == null )
			{
				currentAction = nextAction;
				return;
			}
			var act = currentAction;
			while( act.NextAction != null )
			{
				act = act.NextAction;
			}
			act.NextAction = nextAction;
		}

		public void Tick(Actor self)
		{
			if( currentAction != null )
				currentAction.Tick( self, this );
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
			if (currentAction != null)
				currentAction.Cancel(self, this);
		}
	
		public interface CurrentAction
		{
			CurrentAction NextAction { get; set; }
			void Tick( Actor self, Mobile mobile );
			void Cancel( Actor self, Mobile mobile );
		}

		public class Turn : CurrentAction
		{
			public CurrentAction NextAction { get; set; }

			public int desiredFacing;

			public Turn( int desiredFacing )
			{
				this.desiredFacing = desiredFacing;
			}

			public void Tick( Actor self, Mobile mobile )
			{
				if( desiredFacing == mobile.facing )
				{
					mobile.currentAction = NextAction;
					if( NextAction != null )
						NextAction.Tick( self, mobile );
					return;
				}
				Util.TickFacing( ref mobile.facing, desiredFacing, self.unitInfo.ROT );
			}

			public void Cancel( Actor self, Mobile mobile )
			{
				desiredFacing = mobile.facing;
				NextAction = null;
			}
		}

		public class MoveTo : CurrentAction
		{
			public CurrentAction NextAction { get; set; }

			int2 destination;
			List<int2> path;

			int moveFraction, moveFractionTotal;
			float2 from, to;
			int fromFacing, toFacing;

			Action<Actor, Mobile> OnComplete;

			public MoveTo( int2 destination )
			{
				this.destination = destination;
			}

			public void Tick( Actor self, Mobile mobile )
			{
				if( moveFractionTotal != 0 )
				{
					TickMove( self, mobile );
					return;
				}

				if( destination == self.Location )
				{
					mobile.currentAction = NextAction;
					return;
				}

				if( path == null )
					path = Game.pathFinder.FindUnitPath( self.Location, PathFinder.DefaultEstimator( destination ) );
				if( path.Count == 0 )
				{
					destination = mobile.toCell;
					return;
				}

				var nextCell = path[ path.Count - 1 ];
				int2 dir = nextCell - mobile.fromCell;
				var firstFacing = Util.GetFacing( dir, mobile.facing );
				if( firstFacing != mobile.facing )
					mobile.currentAction = new Turn( firstFacing ) { NextAction = this };
				else
				{
					mobile.toCell = nextCell;
					path.RemoveAt( path.Count - 1 );
					moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 35 : 25;
					from = CenterOfCell( mobile.fromCell );
					to = BetweenCells( mobile.fromCell, mobile.toCell );
					CalculateMoveFraction();
					fromFacing = mobile.facing;
					toFacing = mobile.facing;
					OnComplete = OnCompleteFirstHalf;
				}
				mobile.currentAction.Tick( self, mobile );
			}

			void TickMove( Actor self, Mobile mobile )
			{
				moveFraction += ( self.unitInfo as UnitInfo.MobileInfo ).Speed;
				UpdateCenterLocation( self, mobile, (float)moveFraction / moveFractionTotal );
				if( moveFraction >= moveFractionTotal )
				{
					moveFraction -= moveFractionTotal;
					OnComplete( self, mobile );
					//mobile.fromCell = mobile.toCell;
				}
				return;
			}

			void UpdateCenterLocation( Actor self, Mobile mobile, float frac )
			{
				self.CenterLocation = float2.Lerp( from, to, frac );
				if( moveFraction >= moveFractionTotal )
					mobile.facing = toFacing;
				else
					mobile.facing = ( fromFacing + ( toFacing - fromFacing ) * moveFraction / moveFractionTotal ) & 0xFF;
			}

			void CalculateMoveFraction()
			{
				var d = to - from;
				moveFractionTotal = (int)Math.Sqrt( d.X * d.X + d.Y * d.Y ) * (25 / 6);
			}

			static float2 CenterOfCell( int2 loc )
			{
				return new float2( 12, 12 ) + Game.CellSize * (float2)loc;
			}

			static float2 BetweenCells( int2 from, int2 to )
			{
				return 0.5f * ( CenterOfCell( from ) + CenterOfCell( to ) );
			}

			void OnCompleteFirstHalf( Actor self, Mobile mobile )
			{
				if( path.Count > 0 )
				{
					var nextCell = path[ path.Count - 1 ];
					if( ( nextCell - mobile.toCell ) != ( mobile.toCell - mobile.fromCell ) )
					{
						path.RemoveAt( path.Count - 1 );
						from = BetweenCells( mobile.fromCell, mobile.toCell );
						to = BetweenCells( mobile.toCell, nextCell );
						CalculateMoveFraction();
						mobile.fromCell = mobile.toCell;
						mobile.toCell = nextCell;
						fromFacing = mobile.facing;
						toFacing = Util.GetNearestFacing( fromFacing, Util.GetFacing( mobile.toCell-mobile.fromCell, fromFacing ) );
						OnComplete = OnCompleteFirstHalf;
						return;
					}
				}
				from = BetweenCells( mobile.fromCell, mobile.toCell );
				to = CenterOfCell( mobile.toCell );
				CalculateMoveFraction();
				fromFacing = toFacing = mobile.facing;
				OnComplete = OnCompleteSecondHalf;
				mobile.fromCell = mobile.toCell;
			}

			void OnCompleteSecondHalf( Actor self, Mobile mobile )
			{
				moveFractionTotal = 0;
				self.CenterLocation = CenterOfCell( mobile.toCell );
				OnComplete = null;
				mobile.fromCell = mobile.toCell;
			}

			public void Cancel( Actor self, Mobile mobile )
			{
				path.Clear();
				NextAction = null;
			}
		}
	}
}
