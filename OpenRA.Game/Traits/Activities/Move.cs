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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpenRA.Traits.Activities
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
		int ticksBeforePathing;

		const int avgTicksBeforePathing = 5;
		const int spreadTicksBeforePathing = 5;

		Move()
		{
			ticksBeforePathing = avgTicksBeforePathing + 
				Game.world.SharedRandom.Next(-spreadTicksBeforePathing, spreadTicksBeforePathing);
		}

		public Move( int2 destination, int nearEnough ) 
			: this()
		{
			this.getPath = ( self, mobile ) => self.World.PathFinder.FindUnitPath(
				self.Location, destination,
				mobile.GetMovementType(), self );
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(int2 destination, Actor ignoreBuilding)
			: this()
		{
			this.getPath = (self, mobile) => 
				self.World.PathFinder.FindPath(
					PathSearch.FromPoint( self, self.Location, destination, mobile.GetMovementType(), false )
					.WithCustomBlocker( self.World.PathFinder.AvoidUnitsNear( self.Location, 4, self ))
					.WithIgnoredBuilding( ignoreBuilding ));

			this.destination = destination;
			this.nearEnough = 0;
			this.ignoreBuilding = ignoreBuilding;
		}

		public Move( Actor target, int range )
			: this()
		{
			this.getPath = ( self, mobile ) => self.World.PathFinder.FindUnitPathToRange(
				self.Location, target.Location,
				mobile.GetMovementType(), range, self );
			this.destination = null;
			this.nearEnough = range;
		}

		public Move(Func<List<int2>> getPath)
			: this()
		{
			this.getPath = (_, _2) => getPath();
			this.destination = null;
			this.nearEnough = 0;
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

			if (destination == self.Location)
				return NextActivity;

			if( path == null )
			{
				if (ticksBeforePathing > 0)
				{
					--ticksBeforePathing;
					return this;
				}

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

		bool hasWaited;
		int waitTicksRemaining;

		int2? PopPath( Actor self, Mobile mobile )
		{
			if( path.Count == 0 ) return null;
			var nextCell = path[ path.Count - 1 ];
			if( !mobile.CanEnterCell( nextCell ) )
			{
				if( ( mobile.toCell - destination.Value ).LengthSquared <= nearEnough )
				{
					path.Clear();
					return null;
				}

				if (!hasWaited)
				{
					var info = self.Info.Traits.Get<MobileInfo>();
					waitTicksRemaining = info.WaitAverage + self.World.SharedRandom.Next(-info.WaitSpread, info.WaitSpread);
					hasWaited = true;
				}

				if (--waitTicksRemaining >= 0)
					return null;

				self.World.WorldActor.traits.Get<UnitInfluence>().Remove( self, mobile );
				var newPath = getPath(self, mobile).TakeWhile(a => a != self.Location).ToList();

				self.World.WorldActor.traits.Get<UnitInfluence>().Add( self, mobile );
				if (newPath.Count != 0)
					path = newPath;

				return null;
			}
			hasWaited = false;
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
				this.moveFractionTotal = (int)(( to - from ).Length*3);
			}

			public void TickMove( Actor self, Mobile mobile, Move parent )
			{
				var umt = self.Info.Traits.Get<MobileInfo>().MovementType;
				moveFraction += (int)Util.GetEffectiveSpeed(self, umt);
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
				//	+ self.traits.WithInterface<IOffsetCenterLocation>().Aggregate(float2.Zero, (a, x) => a + x.CenterOffset);

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
