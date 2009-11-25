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
using OpenRa.Game.Traits;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game
{
	class Actor
	{
		public readonly TypeDictionary traits = new TypeDictionary();
		public readonly UnitInfo unitInfo;

		public readonly uint ActorID;
		public int2 Location;
		public Player Owner;
		public int Health;
		IActivity currentActivity;

		public Actor( string name, int2 location, Player owner )
		{
			ActorID = Game.world.NextAID();
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

		public Actor( TreeReference tree, TreeCache treeRenderer )
		{
			ActorID = 0xffffffff;
			Location = new int2( tree.Location );
			traits.Add( new Traits.Tree( treeRenderer.GetImage( tree.Image ) ) );
		}

		public void Tick()
		{
			var nextActivity = currentActivity;
			while( nextActivity != null )
			{
				currentActivity = nextActivity;
				nextActivity = nextActivity.Tick( this );
			}

			foreach (var tick in traits.WithInterface<Traits.ITick>())
				tick.Tick(this);
		}

		public float2 CenterLocation;
		public float2 SelectedSize { get { return Render().First().a.size; } }

		public IEnumerable<Tuple<Sprite, float2, int>> Render()
		{
			return traits.WithInterface<Traits.IRender>().SelectMany( x => x.Render( this ) );
		}

		public Order Order( int2 xy, bool lmb )
		{
			if (Owner != Game.LocalPlayer)
				return null;

			var underCursor = Game.UnitInfluence.GetUnitAt( xy ) ?? Game.BuildingInfluence.GetBuildingAt( xy );

			return traits.WithInterface<Traits.IOrder>()
				.Select( x => x.Order( this, xy, lmb, underCursor ) )
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

			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			Health -= damage;
			if (Health <= 0)
			{
				Health = 0;
				if (attacker.Owner != null)
					attacker.Owner.Kills++;

				Game.world.AddFrameEndTask(w => w.Remove(this));

				if (Owner == Game.LocalPlayer && !traits.Contains<Building>()) 
					Game.PlaySound("unitlst1.aud", false);

				if (traits.Contains<Building>())
					Game.PlaySound("kaboom22.aud", false);
			}

			var halfStrength = unitInfo.Strength * Rules.General.ConditionYellow;
			if (Health < halfStrength && (Health + damage) >= halfStrength)
			{
				/* we just went below half health! */
				foreach (var nd in traits.WithInterface<INotifyDamage>())
					nd.Damaged(this, DamageState.Half);
			}

			foreach (var ndx in traits.WithInterface<INotifyDamageEx>())
				ndx.Damaged(this, damage);
		}

		public void QueueActivity( IActivity nextActivity )
		{
			if( currentActivity == null )
			{
				currentActivity = nextActivity;
				return;
			}
			var act = currentActivity;
			while( act.NextActivity != null )
			{
				act = act.NextActivity;
			}
			act.NextActivity = nextActivity;
		}

		public void CancelActivity( Actor self )
		{
			if( currentActivity != null )
				currentActivity.Cancel( self );
		}

		// For pathdebug, et al
		public IActivity GetCurrentActivity()
		{
			return currentActivity;
		}
	}
}
