using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	abstract class Unit : Actor, IOrderGenerator
	{
		protected Animation animation;

		protected int facing = 0;
		protected int2 fromCell, toCell;
		protected int moveFraction, moveFractionTotal;

		protected delegate void TickFunc( Game game, int t );
		protected TickFunc currentOrder = null;
		protected TickFunc nextOrder = null;

		protected readonly float2 renderOffset;

		public Unit( string name, int2 cell, Player owner, float2 renderOffset )
		{
			fromCell = toCell = cell;
			this.renderOffset = renderOffset;
			this.owner = owner;

			animation = new Animation( name );
			animation.PlayFetchIndex( "idle", delegate { return facing; } );
		}

		static float2[] fvecs = Util.MakeArray<float2>( 32,
			delegate( int i ) { return -float2.FromAngle( i / 16.0f * (float)Math.PI ) * new float2( 1f, 1.3f ); } );

		int GetFacing( float2 d )
		{
			if( float2.WithinEpsilon( d, float2.Zero, 0.001f ) )
				return facing;

			int highest = -1;
			float highestDot = -1.0f;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				float dot = float2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest;
		}

		const int Speed = 6;

		public override void Tick( Game game, int t )
		{
			animation.Tick( t );
			if( currentOrder == null && nextOrder != null )
			{
				currentOrder = nextOrder;
				nextOrder = null;
			}

			if( currentOrder != null )
				currentOrder( game, t );
		}

		public void AcceptMoveOrder( int2 destination )
		{
			nextOrder = delegate( Game game, int t )
			{
				if( nextOrder != null )
					destination = toCell;

				if( Turn( GetFacing( toCell - fromCell ) ) )
					return;

				moveFraction += t * Speed;
				if( moveFraction < moveFractionTotal )
					return;

				moveFraction = 0;
				moveFractionTotal = 0;
				fromCell = toCell;

				if( toCell == destination )
				{
					currentOrder = null;
					return;
				}

				List<int2> res = game.pathFinder.FindUnitPath( this, PathFinder.DefaultEstimator( destination ) );
				if( res.Count != 0 )
				{
					toCell = res[ res.Count - 1 ];

					int2 dir = toCell - fromCell;
					moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 2500 : 2000;
				}
				else
					destination = toCell;
			};
		}

		protected bool Turn( int desiredFacing )
		{
			if( facing == desiredFacing )
				return false;

			int df = ( desiredFacing - facing + 32 ) % 32;
			facing = ( facing + ( df > 16 ? 31 : 1 ) ) % 32;
			return true;
		}

		public override float2 RenderLocation
		{
			get
			{
				float fraction = (moveFraction > 0) ? (float)moveFraction / moveFractionTotal : 0f;

				float2 location = 24 * float2.Lerp( fromCell, toCell, fraction );
				return ( location - renderOffset ).Round(); ;
			}
		}

		public int2 Location { get { return toCell; } }
		public virtual IOrder Order(Game game, int2 xy) { return new MoveOrder(this, xy); }
		public override Sprite[] CurrentImages { get { return animation.Images; } }

		public void PrepareOverlay(Game game, int2 xy) { }
	}
}
