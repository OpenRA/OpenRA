using System;
using System.Collections.Generic;
using System.Linq;

using OpenRa.Game.Graphics;
using IjwFramework.Types;

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

		public override IEnumerable<Pair<Sprite, float2>> CurrentImages
		{
			get
			{
				yield return Centered( animation.Image, CenterLocation );
			}
		}

		public float2 CenterLocation
		{
			get
			{
				float fraction = ( moveFraction > 0 ) ? (float)moveFraction / moveFractionTotal : 0f;
				return new float2( 12, 12 ) + 24 * float2.Lerp( fromCell, toCell, fraction );
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

		protected static Pair<Sprite, float2> Centered( Sprite s, float2 location )
		{
			var loc = location - 0.5f * s.size;
			return Pair.New( s, loc.Round() );
		}

        public float2 SelectedSize { get { return this.CurrentImages.First().First.size; } }
	}

	class TurretedUnit : Unit
	{
		Animation turretAnim;
		int turretFacing { get { return facing; } }

		public TurretedUnit( string name, int2 cell, Player owner, Game game )
			: base( name, cell, owner, game )
		{
			turretAnim = new Animation( name );
			turretAnim.PlayFetchIndex( "turret", () => turretFacing );
		}

		public override IEnumerable<Pair<Sprite, float2>> CurrentImages
		{
            get { return base.CurrentImages.Concat(new[] { Centered(turretAnim.Image, CenterLocation) }); }
		}
	}
}
