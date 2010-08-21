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
		bool cancellable = true;
		
		MovePart move;
		int ticksBeforePathing;

		const int avgTicksBeforePathing = 5;
		const int spreadTicksBeforePathing = 5;

		Move()
		{
			ticksBeforePathing = avgTicksBeforePathing + 
				Game.world.SharedRandom.Next(-spreadTicksBeforePathing, spreadTicksBeforePathing);
		}

		// Scriptable move order
		// Ignores lane bias and nearby units
		public Move( int2 destination ) 
			: this()
		{
			this.getPath = (self,mobile) =>
				self.World.PathFinder.FindPath(
					PathSearch.FromPoint( self, mobile.toCell, destination, false )
					.WithoutLaneBias());
			this.destination = destination;
			this.nearEnough = 0;
			this.cancellable = false;
		}

		public Move( int2 destination, int nearEnough ) 
			: this()
		{
			this.getPath = (self,mobile) => self.World.PathFinder.FindUnitPath(
				mobile.toCell, destination, self );
			this.destination = destination;
			this.nearEnough = nearEnough;
		}
		
		public Move(int2 destination, Actor ignoreBuilding)
			: this()
		{
			this.getPath = (self,mobile) => 
				self.World.PathFinder.FindPath(
					PathSearch.FromPoint( self, mobile.toCell, destination, false )
					.WithCustomBlocker( self.World.PathFinder.AvoidUnitsNear( mobile.toCell, 4, self ))
					.WithIgnoredBuilding( ignoreBuilding ));

			this.destination = destination;
			this.nearEnough = 0;
			this.ignoreBuilding = ignoreBuilding;
		}

		public Move( Actor target, int range )
			: this()
		{
			this.getPath = (self,mobile) => self.World.PathFinder.FindUnitPathToRange(
				mobile.toCell, target.Location,
				range, self );
			this.destination = null;
			this.nearEnough = range;
		}

		public Move(Target target, int range)
			: this()
		{
			this.getPath = (self,mobile) => self.World.PathFinder.FindUnitPathToRange(
				mobile.toCell, Util.CellContaining(target.CenterLocation),
				range, self);
			this.destination = null;
			this.nearEnough = range;
		}

		public Move(Func<List<int2>> getPath)
			: this()
		{
			this.getPath = (_1,_2) => getPath();
			this.destination = null;
			this.nearEnough = 0;
		}

		public IActivity Tick( Actor self )
		{
			var mobile = self.Trait<Mobile>();

			if( move != null )
			{
				move.TickMove( self, mobile, this );
				return this;
			}

			if (destination == mobile.toCell)
				return NextActivity;

			if( path == null )
			{
				if (ticksBeforePathing > 0)
				{
					--ticksBeforePathing;
					return this;
				}

				path = getPath( self, mobile ).TakeWhile( a => a != mobile.toCell ).ToList();
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
			var firstFacing = Util.GetFacing( dir, mobile.Facing );
			if( firstFacing != mobile.Facing )
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
					mobile.Facing,
					mobile.Facing,
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
		bool hasNudged;
		int waitTicksRemaining;

		void NudgeBlocker(Actor self, int2 nextCell)
		{
			var blocker = self.World.WorldActor.Trait<UnitInfluence>().GetUnitsAt(nextCell).FirstOrDefault();
			if (blocker == null) return;

			var nudge = blocker.TraitOrDefault<INudge>();
			if (nudge != null)
				nudge.OnNudge(blocker, self);
		}

		int2? PopPath( Actor self, Mobile mobile )
		{
			if( path.Count == 0 ) return null;
			var nextCell = path[ path.Count - 1 ];
			if( !mobile.CanEnterCell( nextCell, ignoreBuilding, true ) )
			{
				if( ( mobile.toCell - destination.Value ).LengthSquared <= nearEnough )
				{
					path.Clear();
					return null;
				}

				if (!hasNudged)
				{
					NudgeBlocker(self, nextCell);
					hasNudged = true;
				}

				if (!hasWaited)
				{
					var info = self.Info.Traits.Get<MobileInfo>();
					waitTicksRemaining = info.WaitAverage + self.World.SharedRandom.Next(-info.WaitSpread, info.WaitSpread);
					hasWaited = true;
				}

				if (--waitTicksRemaining >= 0)
					return null;

				mobile.RemoveInfluence();
				var newPath = getPath( self, mobile ).TakeWhile(a => a != mobile.toCell).ToList();
				mobile.AddInfluence();

				if (newPath.Count != 0)
					path = newPath;

				return null;
			}
			hasNudged = false;
			hasWaited = false;
			path.RemoveAt( path.Count - 1 );
			return nextCell;
		}

		public void Cancel( Actor self )
		{
			if (!cancellable) return;
			
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
				moveFraction += (int)mobile.MovementSpeedForCell(self, mobile.toCell);
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
				var frac = (float)moveFraction / moveFractionTotal;

				self.CenterLocation = float2.Lerp( from, to, frac );

				if( moveFraction >= moveFractionTotal )
					mobile.Facing = toFacing & 0xFF;
				else
					mobile.Facing = ( fromFacing + ( toFacing - fromFacing ) * moveFraction / moveFractionTotal ) & 0xFF;
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
				var nextCell = parent.PopPath( self, mobile );
				if( nextCell != null )
				{
					if( ( nextCell - mobile.toCell ) != ( mobile.toCell - mobile.fromCell ) )
					{
						var ret = new MoveFirstHalf(
							Util.BetweenCells( mobile.fromCell, mobile.toCell ),
							Util.BetweenCells( mobile.toCell, nextCell.Value ),
							mobile.Facing,
							Util.GetNearestFacing( mobile.Facing, Util.GetFacing( nextCell.Value - mobile.toCell, mobile.Facing ) ),
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
					mobile.Facing,
					mobile.Facing,
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
				mobile.FinishedMoving(self);
				return null;
			}
		}
	}
}
