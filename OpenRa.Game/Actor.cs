using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using System.Drawing;

namespace OpenRa.Game
{
	class Actor
	{
		public readonly TypeDictionary traits = new TypeDictionary();
		public readonly UnitInfo unitInfo;

		public int2 Location;
		public Player Owner;

		public Actor( string name, int2 location, Player owner )
		{
			unitInfo = Rules.UnitInfo[ name ];
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
			//TODO: SBAG, BRIK, FENC, etc
			case "iron":
			case "pdox":
			case "mslo":
			case "atek":
			case "stek":
			case "fact":
			case "proc":
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
			case "gap":
			case "pbox":
			case "hbox":
			case "tsla":
			case "ftur":
			case "facf":
			case "syrf":
			case "spef":
			case "domf":
				traits.Add( new Traits.Building( this ) );
				traits.Add( new Traits.RenderBuilding( this ) );
				break;
			case "weap":
			case "weaf":
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
			case "silo":
				traits.Add(new Traits.Building(this));
				traits.Add(new Traits.RenderBuildingOre(this));
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
		public float2 SelectedSize { get { return Render().LastOrDefault().First.size; } }

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

		public RectangleF Bounds
		{
			get
			{
				var size = SelectedSize;
				var loc = CenterLocation - 0.5f * size;
				return new RectangleF(loc.X, loc.Y, size.X, size.Y);
			}
		}
	}
}
