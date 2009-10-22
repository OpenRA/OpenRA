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
		public int Health;

		public Actor( string name, int2 location, Player owner )
		{
			unitInfo = Rules.UnitInfo[ name ];
			Location = location;
			CenterLocation = new float2( 12, 12 ) + Game.CellSize * (float2)Location;
			Owner = owner;
			Health = unitInfo.Strength;	/* todo: handle cases where this is not true! */

			if( unitInfo.Traits != null )
			{
				foreach( var traitName in unitInfo.Traits )
				{
					var type = typeof( Traits.Mobile ).Assembly.GetType( typeof( Traits.Mobile ).Namespace + "." + traitName, true, false );
					var ctor = type.GetConstructor( new[] { typeof( Actor ) } );
					traits.Add( type, ctor.Invoke( new object[] { this } ) );
				}
			}
			else
				throw new InvalidOperationException( "No Actor traits for " + unitInfo.Name 
					+ "; add Traits= to units.ini for appropriate unit" );
		}

		public Actor( TreeReference tree, TreeCache treeRenderer, int2 mapOffset )
		{
			Location = new int2( tree.Location ) - mapOffset;
			traits.Add( new Traits.Tree( treeRenderer.GetImage( tree.Image ) ) );
		}

		public void Tick()
		{
			foreach (var tick in traits.WithInterface<Traits.ITick>())
				tick.Tick(this);
		}

		public float2 CenterLocation;
		public float2 SelectedSize { get { return Render().LastOrDefault().First.size; } }

		public IEnumerable<Pair<Sprite, float2>> Render()
		{
			return traits.WithInterface<Traits.IRender>().SelectMany( x => x.Render( this ) );
		}

		public Order Order( int2 xy )
		{
			return traits.WithInterface<Traits.IOrder>()
				.Select( x => x.Order( this, xy ) )
				.FirstOrDefault( x => x != null );
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

		public bool IsDead { get { return Health <= 0; } }

		public void InflictDamage(Actor attacker, Bullet inflictor, int damage)
		{
			/* todo: auto-retaliate, etc */
			/* todo: death sequence for infantry based on inflictor */
			/* todo: start smoking if < conditionYellow and took damage, and not already smoking */

			if (Health <= 0) return;		/* overkill! don't count extra hits as more kills! */

			Health -= damage;
			if (Health <= 0)
			{
				Health = 0;
				if (attacker.Owner != null)
					attacker.Owner.Kills++;

				Game.world.AddFrameEndTask(w => w.Remove(this));

				/* todo: explosion */
			}
		}
	}
}
