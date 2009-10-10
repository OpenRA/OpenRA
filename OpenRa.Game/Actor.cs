using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class Actor
	{
		public readonly TypeDictionary traits = new TypeDictionary();
		public readonly UnitInfo.BaseInfo unitInfo;

		public int2 Location;
		public Player Owner;

		public Actor( string name, int2 location, Player owner )
		{
			unitInfo = Rules.UnitInfo.Get( name );
			Location = location;
			CenterLocation = new float2( 12, 12 ) + 24 * (float2)Location;
			Owner = owner;

			switch( name )
			{
			///// vehicles /////
			case "mcv":
				traits.Add( new Traits.Mobile( this ) );
				traits.Add( new Traits.RenderUnit( this ) );
				traits.Add( new Traits.McvDeploy( this ) );
				break;
			case "mnly":
			case "apc":
			case "v2rl":
			case "arty":
				traits.Add( new Traits.Mobile( this ) );
				traits.Add( new Traits.RenderUnit( this ) );
				break;
			case "jeep":
			case "1tnk":
			case "2tnk":
			case "3tnk":
			case "4tnk":
			case "mrj":
			case "mgg":
				traits.Add( new Traits.Mobile( this ) );
				traits.Add( new Traits.Turreted( this ) );
				traits.Add( new Traits.RenderUnitTurreted( this ) );
				break;
			case "harv":
				traits.Add( new Traits.Mobile( this ) );
				traits.Add( new Traits.RenderUnit( this ) );
				break;
			///// TODO: infantry /////

			///// TODO: boats /////

			///// TODO: planes /////

			///// buildings /////
			case "iron":
			case "pdox":
			case "mslo":
			case "atek":
			case "stek":
			case "fact":
			case "proc":
			case "silo":
			case "hpad":
			case "afld":
			case "dome":
			case "powr":
			case "apwr":
			case "barr":
			case "tent":
			case "kenn":
			case "fix":
			case "spen":
			case "syrd":
			//GAP
			//SBAG, BRIK, FENC
			//FACF, WEAF, SYRF, SPEF, DOMF
			case "pbox":
			case "hbox":
			case "tsla":
			case "ftur":
				traits.Add( new Traits.Building( this ) );
				traits.Add( new Traits.RenderBuilding( this ) );
				break;
			case "weap":
				traits.Add( new Traits.Building( this ) );
				traits.Add( new Traits.RenderWarFactory( this ) );
				break;
			case "gun":
			case "agun":
			case "sam":
				traits.Add( new Traits.Building( this ) );
				traits.Add( new Traits.Turreted( this ) );
				traits.Add( new Traits.RenderBuildingTurreted( this ) );
				break;
			default:
				throw new NotImplementedException( "Actor traits for " + name );
			}
		}

		public Actor( TreeReference tree, TreeCache treeRenderer, int2 mapOffset )
		{
			Location = new int2( tree.Location ) - mapOffset;
			traits.Add( new Traits.Tree( treeRenderer.GetImage( tree.Image ) ) );
		}

		public void Tick( Game game, int dt )
		{
			foreach( var tick in traits.WithInterface<Traits.ITick>() )
				tick.Tick( this, game, dt );
		}

		public float2 CenterLocation;
		public float2 SelectedSize { get { return Render().FirstOrDefault().First.size; } }

		public IEnumerable<Pair<Sprite, float2>> Render()
		{
			return traits.WithInterface<Traits.IRender>().SelectMany( x => x.Render( this ) );
		}

		public Order Order( Game game, int2 xy )
		{
			return traits.WithInterface<Traits.IOrder>()
				.Select( x => x.Order( this, game, xy ) )
				.Where( x => x != null )
				.FirstOrDefault();
		}
	}

	namespace Traits
	{
		interface ITick
		{
			void Tick( Actor self, Game game, int dt );
		}

		interface IRender
		{
			IEnumerable<Pair<Sprite, float2>> Render( Actor self );
		}

		interface IOrder
		{
			Order Order( Actor self, Game game, int2 xy );
		}

		abstract class RenderSimple : IRender, ITick
		{
			public Animation anim;

			public RenderSimple( Actor self )
			{
				anim = new Animation( self.unitInfo.Name );
			}

			public abstract IEnumerable<Pair<Sprite, float2>> Render( Actor self );

			public virtual void Tick( Actor self, Game game, int dt )
			{
				anim.Tick( dt );
			}
		}

		class RenderBuilding : RenderSimple
		{
			public RenderBuilding( Actor self )
				: base( self )
			{
				anim.PlayThen( "make", () => anim.PlayRepeating( "idle" ) );
			}

			public override IEnumerable<Pair<Sprite, float2>> Render( Actor self )
			{
				yield return Pair.New( anim.Image, 24f * (float2)self.Location );
			}
		}

		class RenderBuildingTurreted : RenderBuilding
		{
			public RenderBuildingTurreted( Actor self )
				: base( self )
			{
				anim.PlayThen( "make", () => anim.PlayFetchIndex( "idle", () => self.traits.Get<Turreted>().turretFacing ) );
			}
		}

		class RenderWarFactory : RenderBuilding
		{
			public Animation roof;
			bool doneBuilding;

			public RenderWarFactory( Actor self )
				: base( self )
			{
				roof = new Animation( self.unitInfo.Name );
				anim.PlayThen( "make", () =>
					{
						doneBuilding = true;
						anim.Play( "idle" );
						roof.Play( "idle-top" );
					} );
			}

			public override IEnumerable<Pair<Sprite, float2>> Render( Actor self )
			{
				yield return Pair.New( anim.Image, 24f * (float2)self.Location );
				if( doneBuilding )
					yield return Pair.New( roof.Image, 24f * (float2)self.Location );
			}

			public override void Tick( Actor self, Game game, int dt )
			{
				base.Tick( self, game, dt );
				roof.Tick( dt );
			}
		}

		class RenderUnit : RenderSimple
		{
			public RenderUnit( Actor self )
				: base( self )
			{
				anim.PlayFetchIndex( "idle", () => self.traits.Get<Mobile>().facing );
			}

			protected static Pair<Sprite, float2> Centered( Sprite s, float2 location )
			{
				var loc = location - 0.5f * s.size;
				return Pair.New( s, loc.Round() );
			}

			public override IEnumerable<Pair<Sprite, float2>> Render( Actor self )
			{
				var mobile = self.traits.Get<Mobile>();
				float fraction = ( mobile.moveFraction > 0 ) ? (float)mobile.moveFraction / mobile.moveFractionTotal : 0f;
				var centerLocation = new float2( 12, 12 ) + 24 * float2.Lerp( mobile.fromCell, mobile.toCell, fraction );
				yield return Centered( anim.Image, centerLocation );
			}
		}

		class RenderUnitTurreted : RenderUnit
		{
			public Animation turretAnim;

			public RenderUnitTurreted( Actor self )
				: base( self )
			{
				turretAnim = new Animation( self.unitInfo.Name );
				turretAnim.PlayFetchIndex( "turret", () => self.traits.Get<Turreted>().turretFacing );
			}

			public override IEnumerable<Pair<Sprite, float2>> Render( Actor self )
			{
				var mobile = self.traits.Get<Mobile>();
				yield return Centered( anim.Image, self.CenterLocation );
				yield return Centered( turretAnim.Image, self.CenterLocation );
			}

			public override void Tick( Actor self, Game game, int dt )
			{
				base.Tick( self, game, dt );
				turretAnim.Tick( dt );
			}
		}

		class Mobile : ITick, IOrder
		{
			public Actor self;

			public int2 fromCell, destination;
			public int2 toCell { get { return self.Location; } }
			public int moveFraction, moveFractionTotal;
			public int facing;

			public Mobile( Actor self )
			{
				this.self = self;
				fromCell = destination = self.Location;
			}

			public bool Turn( int desiredFacing )
			{
				if( facing == desiredFacing )
					return false;

				int df = ( desiredFacing - facing + 32 ) % 32;
				facing = ( facing + ( df > 16 ? 31 : 1 ) ) % 32;
				return true;
			}

			static float2[] fvecs = Util.MakeArray<float2>( 32,
				i => -float2.FromAngle( i / 16.0f * (float)Math.PI ) * new float2( 1f, 1.3f ) );

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

			void UpdateCenterLocation()
			{
				float fraction = ( moveFraction > 0 ) ? (float)moveFraction / moveFractionTotal : 0f;
				self.CenterLocation = new float2( 12, 12 ) + 24 * float2.Lerp( fromCell, toCell, fraction );
			}

			public void Tick( Actor self, Game game, int dt )
			{
				Move( self, game, dt );
				UpdateCenterLocation();
			}

			void Move( Actor self, Game game, int dt )
			{
				if( fromCell != toCell )
				{
					if( Turn( GetFacing( toCell - fromCell ) ) )
						return;

					moveFraction += dt * ( (UnitInfo.MobileInfo)self.unitInfo ).Speed;
				}
				if( moveFraction < moveFractionTotal )
					return;

				moveFraction = 0;
				moveFractionTotal = 0;
				fromCell = toCell;

				if( destination == toCell )
					return;

				List<int2> res = game.pathFinder.FindUnitPath( toCell, PathFinder.DefaultEstimator( destination ) );
				if( res.Count != 0 )
				{
					self.Location = res[ res.Count - 1 ];

					int2 dir = toCell - fromCell;
					moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 2500 : 2000;
				}
				else
					destination = toCell;
			}

			public Order Order( Actor self, Game game, int2 xy )
			{
				if( xy != toCell )
					return new MoveOrder( self, xy );
				return null;
			}
		}

		class McvDeploy : IOrder, ITick
		{
			public bool Deploying;

			public McvDeploy( Actor self )
			{
			}

			public Order Order( Actor self, Game game, int2 xy )
			{
				// TODO: check that there's enough space at the destination.
				if( xy == self.Location )
					return new DeployMcvOrder( self );

				return null;
			}

			public void Tick( Actor self, Game game, int dt )
			{
				if( !Deploying )
					return;

				if( self.traits.Get<Mobile>().Turn( 12 ) )
					return;

				game.world.AddFrameEndTask( _ =>
				{
					game.world.Remove( self );
					game.world.Add( new Actor( "fact", self.Location - new int2( 1, 1 ), self.Owner ) );
				} );
			}
		}

		class Turreted
			: ITick // temporary.
		{
			public int turretFacing = 24;

			public Turreted( Actor self )
			{
			}

			// temporary.
			public void Tick( Actor self, Game game, int dt )
			{
				turretFacing = ( turretFacing + 1 ) % 32;
			}
		}

		class Building : ITick
		{
			public Building( Actor self )
			{
			}

			bool first = true;
			public void Tick( Actor self, Game game, int dt )
			{
				if( first && self.Owner == game.LocalPlayer )
				{
					self.Owner.TechTree.Build( self.unitInfo.Name, true );
					self.CenterLocation = 24 * (float2)self.Location + 0.5f * self.SelectedSize;
				}
				first = false;
			}
		}

		class Tree : IRender
		{
			Sprite Image;

			public Tree( Sprite treeImage )
			{
				Image = treeImage;
			}

			public IEnumerable<Pair<Sprite, float2>> Render( Actor self )
			{
				yield return Pair.New( Image, 24 * (float2)self.Location );
			}
		}

		//class WarFactory : Building
		//{
		//    Animation roof;

		//    public WarFactory( int2 location, Player owner, Game game )
		//        : base( "weap", location, owner, game )
		//    {

		//        animation.PlayThen( "make", () =>
		//        {
		//            roof = new Animation( "weap" );
		//            animation.PlayRepeating( "idle" );
		//            roof.PlayRepeating( "idle-top" );
		//        } );
		//    }

		//    public override IEnumerable<Pair<Sprite, float2>> CurrentImages
		//    {
		//        get
		//        {
		//            return ( roof == null )
		//                ? base.CurrentImages
		//                : ( base.CurrentImages.Concat(
		//                new[] { Pair.New( roof.Image, 24 * (float2)location ) } ) );
		//        }
		//    }

		//    public override void Tick( Game game, int t )
		//    {
		//        base.Tick( game, t );
		//        if( roof != null ) roof.Tick( t );
		//    }
		//}
	}
}
