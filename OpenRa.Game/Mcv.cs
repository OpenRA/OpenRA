using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class Mcv : Actor, ISelectable
	{
		static Range<int>? mcvRange = null;
		int facing = 0;
		int2 fromCell, toCell;
		int moveFraction, moveFractionTotal;

		delegate void TickFunc( World world, double t );
		TickFunc currentOrder = null;
		TickFunc nextOrder = null;

		public Mcv( int2 cell, int palette )
		{
			fromCell = toCell = cell;
			float2 location = ( cell * 24 ).ToFloat2();
			this.renderLocation = location - new float2( 12, 12 ); // HACK: display the mcv centered in it's cell
			this.palette = palette;

			if (mcvRange == null)
				mcvRange = UnitSheetBuilder.AddUnit("mcv");
		}

		static float2[] fvecs;

		static Mcv()
		{
			fvecs = new float2[32];
			for (int i = 0; i < 32; i++)
			{
				float angle = i / 16.0f * (float)Math.PI;
				fvecs[i] = new float2(-(float)Math.Sin(angle), -(float)Math.Cos(angle));
			}
		}

		int GetFacing(float2 d)
		{
			if (float2.WithinEpsilon(d, float2.Zero, 0.001f))
				return facing;

			int highest = -1;
			float highestDot = -1.0f;

			for (int i = 0; i < 32; i++)
			{
				float dot = float2.Dot(fvecs[i], d);
				if (dot > highestDot)
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest;
		}

		public override Sprite[] CurrentImages
		{
			get
			{
				return new Sprite[] { UnitSheetBuilder.sprites[facing + mcvRange.Value.Start] };
			}
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
				{
					int df = ( desiredFacing - facing + 32 ) % 32;
					if( df > 16 )
						facing = ( facing + 31 ) % 32;
					else
						facing = ( facing + 1 ) % 32;
				}
				else
				{
					moveFraction += speed;
					if( moveFraction >= moveFractionTotal )
					{
						moveFraction = 0;
						moveFractionTotal = 0;
						fromCell = toCell;

						if( toCell == destination )
						{
							currentOrder = null;
						}
						else
						{
							int2 dir = destination - fromCell;
							toCell = fromCell + new int2( Math.Sign( dir.X ), Math.Sign( dir.Y ) );
							moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 250 : 200;
						}
					}
				}

				float2 location;
				if( moveFraction > 0 )
				{
					float frac = (float)moveFraction / moveFractionTotal;
					location = 24 * ( ( 1 - frac ) * fromCell.ToFloat2() + frac * toCell.ToFloat2() );
				}
				else
					location = 24 * fromCell.ToFloat2();

				renderLocation = location - new float2( 12, 12 ); // HACK: center mcv in it's cell

				renderLocation.X = (float)Math.Round( renderLocation.X );
				renderLocation.Y = (float)Math.Round( renderLocation.Y );
			};
		}

		public void AcceptDeployOrder()
		{
			nextOrder = delegate( World world, double t )
			{
				int desiredFacing = 12;
				if( facing != desiredFacing )
				{
					int df = ( desiredFacing - facing + 32 ) % 32;
					if( df > 16 )
						facing = ( facing + 31 ) % 32;
					else
						facing = ( facing + 1 ) % 32;
				}
				else
				{
					world.AddFrameEndTask( delegate
					{
						world.Add( new Refinery( ( fromCell * 24 - new int2( 24, 24 ) ).ToFloat2(), palette ) );
					} );
					currentOrder = null;
				}
			};
		}

		public IOrder Order( int2 xy )
		{
			if( ( fromCell == toCell || moveFraction == 0 ) && fromCell == xy )
				return new DeployMcvOrder( this );
			else
				return new MoveOrder( this, xy );
		}
	}
}
