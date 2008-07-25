using System;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class Unit : PlayerOwned, IOrderGenerator
	{
		public int facing = 0;
		public int2 fromCell;
		public int2 toCell
		{
			get { return location; }
			set { location = value; }
		}

		public int moveFraction, moveFractionTotal;

		readonly float2 renderOffset;
		public readonly UnitInfo unitInfo;

		public Unit( string name, int2 cell, Player owner, Game game )
			: base( game, name, cell )
		{
			fromCell = toCell = cell;

			this.owner = owner;
			this.unitInfo = Rules.UnitInfo( name );

			animation.PlayFetchIndex( "idle", () => facing );
			renderOffset = animation.Center;
		}

		static float2[] fvecs = Util.MakeArray<float2>(32,
			i => -float2.FromAngle(i / 16.0f * (float)Math.PI) * new float2(1f, 1.3f));

		public int GetFacing( float2 d )
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

		public override void Tick( Game game, int t )
		{
			animation.Tick( t );
			if( currentOrder == null && nextOrder != null )
			{
				currentOrder = nextOrder;
				nextOrder = null;
			}

			if( currentOrder != null )
				currentOrder( t );
		}

		public override float2 RenderLocation
		{
			get
			{
				float fraction = ( moveFraction > 0 ) ? (float)moveFraction / moveFractionTotal : 0f;

				float2 location = 24 * float2.Lerp( fromCell, toCell, fraction );
				return ( location - renderOffset ).Round();
			}
		}

		bool SupportsMission( SupportedMissions mission )
		{
			return mission == ( unitInfo.supportedMissions & mission );
		}

		public Order Order( Game game, int2 xy )
		{
			if( ( fromCell == toCell || moveFraction == 0 ) && fromCell == xy )
			{
				if( SupportsMission( SupportedMissions.Deploy ) )
					return new DeployMcvOrder( this );
				if( SupportsMission( SupportedMissions.Harvest ) )
					return new HarvestOrder( this );
			}
			
			return new MoveOrder( this, xy );
		}

		public void PrepareOverlay(Game game, int2 xy) { }
	}
}
