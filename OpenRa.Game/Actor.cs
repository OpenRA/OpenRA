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
			CenterLocation = new float2( 12, 12 ) + Game.CellSize * (float2)Location;
			Owner = owner;

			if( unitInfo.Traits != null )
			{
				foreach( var traitName in unitInfo.Traits )
				{
					var type = typeof( Traits.Mobile ).Assembly.GetType( typeof( Traits.Mobile ).Namespace + "." + traitName, true, false );
					var ctor = type.GetConstructor( new Type[] { typeof( Actor ) } );
					traits.Add( type, ctor.Invoke( new object[] { this } ) );
				}
			}
			else
				throw new InvalidOperationException( "No Actor traits for " + unitInfo.Name + "; add Traits= to units.ini for appropriate unit" );
		}

		public Actor( TreeReference tree, TreeCache treeRenderer, int2 mapOffset )
		{
			Location = new int2( tree.Location ) - mapOffset;
			traits.Add( new Traits.Tree( treeRenderer.GetImage( tree.Image ) ) );
		}

		public void Tick( Game game )
		{
			foreach( var tick in traits.WithInterface<Traits.ITick>() )
				tick.Tick( this, game );
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
