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
	public class Move : CancelableActivity
	{
		int2? destination;
		int nearEnough;
		public List<int2> path;
		Func<Actor, Mobile, List<int2>> getPath;
		public Actor ignoreBuilding;
		
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
					PathSearch.FromPoint( self.World, mobile.Info, mobile.toCell, destination, false )
					.WithoutLaneBias());
			this.destination = destination;
			this.nearEnough = 0;
		}

		public Move( int2 destination, int nearEnough ) 
			: this()
		{
			this.getPath = (self,mobile) => self.World.PathFinder.FindUnitPath( mobile.toCell, destination, self );
			this.destination = destination;
			this.nearEnough = nearEnough;
		}
		
		public Move(int2 destination, Actor ignoreBuilding)
			: this()
		{
			this.getPath = (self,mobile) => 
				self.World.PathFinder.FindPath(
					PathSearch.FromPoint( self.World, mobile.Info, mobile.toCell, destination, false )
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

		static int HashList<T>(List<T> xs)
		{
			int hash = 0;
			int n = 0;
			foreach (var x in xs)
				hash += n++ * x.GetHashCode();

			return hash;
		}

		List<int2> EvalPath( Actor self, Mobile mobile )
		{
			var path = getPath(self, mobile).TakeWhile(a => a != mobile.toCell).ToList();

			Log.Write("debug", "EvalPath #{0} {1}",
				self.ActorID, string.Join(" ", path.Select(a => a.ToString()).ToArray()));

			mobile.PathHash = HashList(path);
			Log.Write("debug", "EvalPathHash #{0} {1}", 
				self.ActorID, mobile.PathHash);

			return path;
		}

		public override IActivity Tick( Actor self )
		{
			var mobile = self.Trait<Mobile>();

			if (destination == mobile.toCell)
				return NextActivity;

			if( path == null )
			{
				if (ticksBeforePathing > 0)
				{
					--ticksBeforePathing;
					return this;
				}

				path = EvalPath(self, mobile);
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
				Log.Write("debug", "Turn: #{0} from {1} to {2}",
					self.ActorID, mobile.Facing, firstFacing);

				return Util.SequenceActivities( new Turn( firstFacing ), this ).Tick( self );
			}
			else
			{
				mobile.toCell = nextCell.Value;
				var move = new MoveFirstHalf(
					this,
					Util.CenterOfCell( mobile.fromCell ),
					Util.BetweenCells( mobile.fromCell, mobile.toCell ),
					mobile.Facing,
					mobile.Facing,
					0 );

				return move.Tick( self );
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

			Log.Write("debug", "NudgeBlocker #{0} nudges #{1} at {2} from {3}",
				self.ActorID, blocker.ActorID, nextCell, self.Location);

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
				var newPath = EvalPath(self, mobile);
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

		protected override bool OnCancel()
		{
			path = new List<int2>();
			return true;
		}

		public override IEnumerable<float2> GetCurrentPath()
		{
			if( path != null )
				return Enumerable.Reverse(path).Select( c => Util.CenterOfCell(c) );
			if( destination != null )
				return new float2[] { destination.Value };
			return new float2[ 0 ];
		}

		abstract class MovePart : IActivity
		{
			public readonly Move move;
			public readonly float2 from, to;
			public readonly int fromFacing, toFacing;
			public int moveFraction;
			public readonly int moveFractionTotal;

			public MovePart( Move move, float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
			{
				this.move = move;
				this.from = from;
				this.to = to;
				this.fromFacing = fromFacing;
				this.toFacing = toFacing;
				this.moveFraction = startingFraction;
				this.moveFractionTotal = (int)(( to - from ).Length*3);
			}

			public void Cancel( Actor self )
			{
				move.Cancel( self );
			}

			public void Queue( IActivity activity )
			{
				move.Queue( activity );
			}

			public IActivity Tick( Actor self )
			{
				var mobile = self.Trait<Mobile>();
				moveFraction += (int)mobile.MovementSpeedForCell(self, mobile.toCell);
				if( moveFraction >= moveFractionTotal )
					moveFraction = moveFractionTotal;
				UpdateCenterLocation( self, mobile );
				if( moveFraction >= moveFractionTotal )
				{
					var next = OnComplete( self, mobile, move );
					if( next != null )
						return next;
					UpdateCenterLocation( self, mobile );
					return move;
				}
				return this;
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

			public IEnumerable<float2> GetCurrentPath()
			{
				return move.GetCurrentPath();
			}
		}

		class MoveFirstHalf : MovePart
		{
			public MoveFirstHalf( Move move, float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
				: base( move, from, to, fromFacing, toFacing, startingFraction )
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
							move,
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
					move,
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
			public MoveSecondHalf( Move move, float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
				: base( move, from, to, fromFacing, toFacing, startingFraction )
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
