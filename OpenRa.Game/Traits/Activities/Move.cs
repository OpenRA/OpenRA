using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRa.GameRules;

namespace OpenRa.Traits.Activities
{
	public class Move : IActivity
	{
		public IActivity NextActivity { get; set; }

		int2? destination;
		int nearEnough;
		public List<int2> path;
		Func<Actor, Mobile, List<int2>> getPath;
		public Actor ignoreBuilding;

		MovePart move;

		public Move( int2 destination, int nearEnough )
		{
			this.getPath = ( self, mobile ) => Game.PathFinder.FindUnitPath(
				self.Location, destination,
				mobile.GetMovementType() );
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(int2 destination, Actor ignoreBuilding)
		{
			this.getPath = (self, mobile) => 
				Game.PathFinder.FindPath(
					PathSearch.FromPoint( self.Location, destination, mobile.GetMovementType(), false )
					.WithCustomBlocker( Game.PathFinder.AvoidUnitsNear( self.Location, 4 )).WithIgnoredBuilding( ignoreBuilding ));

			this.destination = destination;
			this.nearEnough = 0;
			this.ignoreBuilding = ignoreBuilding;
		}

		public Move( Actor target, int range )
		{
			this.getPath = ( self, mobile ) => Game.PathFinder.FindUnitPathToRange(
				self.Location, target.Location,
				mobile.GetMovementType(), range );
			this.destination = null;
			this.nearEnough = range;
		}

		public Move(Func<List<int2>> getPath)
		{
			this.getPath = (_, _2) => getPath();
			this.destination = null;
			this.nearEnough = 0;
		}

		bool CanEnterCell( int2 c, Actor self )
		{
			if (!Game.BuildingInfluence.CanMoveHere(c)
				&& Game.BuildingInfluence.GetBuildingAt(c) != ignoreBuilding) 
				return false;
			
			// Cannot enter a cell if any unit inside is uncrushable
			// This will need to be updated for multiple-infantry-in-a-cell
			return (!Game.UnitInfluence.GetUnitsAt(c).Any(a => a != self && !Game.IsActorCrushableByActor(a, self)));
		}

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();
			var mobile = self.traits.Get<Mobile>();

			if( move != null )
			{
				move.TickMove( self, mobile, this );
				return this;
			}

			if( destination == self.Location )
				return NextActivity;

			if( path == null )
			{
				path = getPath( self, mobile ).TakeWhile( a => a != self.Location ).ToList();
				SanityCheckPath( mobile );
			}
			
			if( path.Count == 0 )
			{
				destination = mobile.toCell;
				return this;
			}

			destination = path[ 0 ];

			var nextCell = PopPath( self, mobile );
			if( nextCell == null )
				return this;

			int2 dir = nextCell.Value - mobile.fromCell;
			var firstFacing = Util.GetFacing( dir, unit.Facing );
			if( firstFacing != unit.Facing )
			{
				path.Add( nextCell.Value );

				return new Turn( firstFacing ) { NextActivity = this };
			}
			else
			{
				mobile.toCell = nextCell.Value;
				move = new MoveFirstHalf(
					Util.CenterOfCell( mobile.fromCell ),
					Util.BetweenCells( mobile.fromCell, mobile.toCell ),
					unit.Facing,
					unit.Facing,
					0 );

				move.TickMove( self, mobile, this );

				return this;
			}
		}

		[Conditional( "SANITY_CHECKS")]
		void SanityCheckPath( Mobile mobile )
		{
			if( path.Count == 0 )
				return;
			var d = path[path.Count-1] - mobile.toCell;
			if( d.LengthSquared > 2 )
				throw new InvalidOperationException( "(Move) Sanity check failed" );
		}

		int2? PopPath( Actor self, Mobile mobile )
		{
			if( path.Count == 0 ) return null;
			var nextCell = path[ path.Count - 1 ];
			if( !CanEnterCell( nextCell, self ) )
			{
				if( ( mobile.toCell - destination.Value ).LengthSquared <= nearEnough )
				{
					path.Clear();
					return null;
				}

				Game.UnitInfluence.Remove( self, mobile );
				var newPath = getPath(self, mobile).TakeWhile(a => a != self.Location).ToList();

				Game.UnitInfluence.Add( self, mobile );
				if (newPath.Count != 0)
					path = newPath;

				return null;
			}
			path.RemoveAt( path.Count - 1 );
			return nextCell;
		}

		public void Cancel( Actor self )
		{
			path = new List<int2>();
			NextActivity = null;
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

			public void TickMove( Actor self, Mobile mobile, Move parent )
			{
				var oldFraction = moveFraction;
				var oldTotal = moveFractionTotal;

				moveFraction += (int)Util.GetEffectiveSpeed(self);
				if( moveFraction >= moveFractionTotal )
					moveFraction = moveFractionTotal;
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
				var unit = self.traits.Get<Unit>();
				var frac = (float)moveFraction / moveFractionTotal;

				self.CenterLocation = float2.Lerp( from, to, frac );
				if( moveFraction >= moveFractionTotal )
					unit.Facing = toFacing & 0xFF;
				else
					unit.Facing = ( fromFacing + ( toFacing - fromFacing ) * moveFraction / moveFractionTotal ) & 0xFF;
			}

			protected abstract MovePart OnComplete( Actor self, Mobile mobile, Move parent );
		}

		class MoveFirstHalf : MovePart
		{
			public MoveFirstHalf( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
				: base( from, to, fromFacing, toFacing, startingFraction )
			{
			}

			protected override MovePart OnComplete( Actor self, Mobile mobile, Move parent )
			{
				var unit = self.traits.Get<Unit>();

				var nextCell = parent.PopPath( self, mobile );
				if( nextCell != null )
				{
					if( ( nextCell - mobile.toCell ) != ( mobile.toCell - mobile.fromCell ) )
					{
						var ret = new MoveFirstHalf(
							Util.BetweenCells( mobile.fromCell, mobile.toCell ),
							Util.BetweenCells( mobile.toCell, nextCell.Value ),
							unit.Facing,
							Util.GetNearestFacing( unit.Facing, Util.GetFacing( nextCell.Value - mobile.toCell, unit.Facing ) ),
							moveFraction - moveFractionTotal );
						mobile.fromCell = mobile.toCell;
						mobile.toCell = nextCell.Value;
						return ret;
					}
					else
						parent.path.Add( nextCell.Value );
				}
				var ret2 = new MoveSecondHalf(
					Util.BetweenCells( mobile.fromCell, mobile.toCell ),
					Util.CenterOfCell( mobile.toCell ),
					unit.Facing,
					unit.Facing,
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

			protected override MovePart OnComplete( Actor self, Mobile mobile, Move parent )
			{
				self.CenterLocation = Util.CenterOfCell( mobile.toCell );
				mobile.fromCell = mobile.toCell;
				return null;
			}
		}
	}
}
