using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	abstract class Unit : Actor, ISelectable
	{
		protected int facing = 0;
		protected int2 fromCell, toCell;
		protected int moveFraction, moveFractionTotal;

		protected delegate void TickFunc( World world, double t );
		protected TickFunc currentOrder = null;
		protected TickFunc nextOrder = null;

		public Unit( int2 cell, int palette )
		{
			fromCell = toCell = cell;
			// HACK: display the mcv centered in it's cell;
			renderLocation = ( cell * 24 ).ToFloat2() - new float2( 12, 12 );
			this.palette = palette;
		}

		static float2[] fvecs = Util.MakeArray<float2>( 32,
			delegate( int i ) { return -float2.FromAngle( i / 16.0f * (float)Math.PI ); } );

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

		public override void Tick( World world, double t )
		{
			if( currentOrder == null && nextOrder != null )
			{
				currentOrder = nextOrder;
				nextOrder = null;
			}

			if( currentOrder != null )
				currentOrder( world, t );
		}

		public void AcceptMoveOrder( int2 destination )
		{
			nextOrder = delegate( World world, double t )
			{
				int speed = (int)( t * ( Speed * 100 ) );

				if( nextOrder != null )
					destination = toCell;

				int desiredFacing = GetFacing( ( toCell - fromCell ).ToFloat2() );
				if( facing != desiredFacing )
					Turn( desiredFacing );
				else
				{
					moveFraction += speed;
					if( moveFraction >= moveFractionTotal )
					{
						moveFraction = 0;
						moveFractionTotal = 0;
						fromCell = toCell;

						if( toCell == destination )
							currentOrder = null;
						else
						{
							List<int2> res = PathFinder.Instance.FindUnitPath( world, this, destination );
							if( res.Count != 0 )
							{
								toCell = res[ res.Count - 1 ];

								int2 dir = toCell - fromCell;
								moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 250 : 200;
							}
							else
								destination = toCell;
						}
					}
				}

				float2 location;
				if( moveFraction > 0 )
					location = 24 * float2.Lerp( fromCell.ToFloat2(), toCell.ToFloat2(),
						(float)moveFraction / moveFractionTotal );
				else
					location = 24 * fromCell.ToFloat2();

				renderLocation = location - new float2( 12, 12 ); // HACK: center mcv in it's cell

				renderLocation = renderLocation.Round();
			};
		}

		protected void Turn( int desiredFacing )
		{
			int df = ( desiredFacing - facing + 32 ) % 32;
			facing = ( facing + ( df > 16 ? 31 : 1 ) ) % 32;
		}

		public virtual IOrder Order( int2 xy )
		{
			return new MoveOrder( this, xy );
		}

		public int2 Location
		{
			get { return toCell; }
		}
	}
}
