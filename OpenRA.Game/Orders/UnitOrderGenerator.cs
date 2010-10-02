#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Orders
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order( World world, int2 xy, MouseInput mi )
		{		
			var underCursor = world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.Info.Traits.Contains<TargetableInfo>())
				.OrderByDescending(a => a.Info.Traits.Contains<SelectableInfo>() ? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
				.FirstOrDefault();
			
			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, xy, mi, underCursor))
				.Where(o => o != null)
				.ToArray();

			var actorsInvolved = orders.Select(o => o.self).Distinct();
			if (actorsInvolved.Any())
				yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor,
					string.Join(",", actorsInvolved.Select(a => a.ActorID.ToString()).ToArray()));

			foreach( var o in orders )
				yield return CheckSameOrder( o.iot, o.trait.IssueOrder( o.self, o.iot, o.target ) );
		}

		public void Tick( World world ) {}

		public void RenderBeforeWorld(World world)
		{
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(a);

			Game.Renderer.Flush();
		}

		public void RenderAfterWorld( World world )
		{
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPostRenderSelection>())
						t.RenderAfterWorld(a);

			Game.Renderer.Flush();
		}

		public string GetCursor( World world, int2 xy, MouseInput mi )
		{		
			var underCursor = world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.Info.Traits.Contains<TargetableInfo>())
				.OrderByDescending(a => a.Info.Traits.Contains<SelectableInfo>() ? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
				.FirstOrDefault();
			
			if( mi.Modifiers.HasModifier( Modifiers.Shift ) || !world.Selection.Actors.Any() )
				if( underCursor != null )
					return "select";
			
			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, xy, mi, underCursor))
				.Where(o => o != null)
				.ToArray();

			if( orders.Length == 0 ) return "default";

			return orders[ 0 ].cursor ?? "default";
		}

		static UnitOrderResult OrderForUnit( Actor self, int2 xy, MouseInput mi, Actor underCursor )
		{
			if (self.Owner != self.World.LocalPlayer)
				return null;

			if (!self.World.Map.IsInMap(xy.X, xy.Y))
				return null;

			if (self.Destroyed)
				return null;

			//var old = self.TraitsImplementing<IIssueOrder>()
			//    .OrderByDescending( x => x.OrderPriority( self, xy, mi, underCursor ) )
			//    .Select( x => x.IssueOrder( self, xy, mi, underCursor ) )
			//    .FirstOrDefault( x => x != null );
			//if( old != null )
			//    return old;

			if( mi.Button == MouseButton.Right )
			{
				var uim = self.World.WorldActor.Trait<UnitInfluence>();
				foreach( var o in self.TraitsImplementing<IIssueOrder>()
					.SelectMany( trait => trait.Orders
						.Select( x => new { Trait = trait, Order = x } ) )
					.OrderByDescending( x => x.Order.OrderPriority ) )
				{
					var actorsAt = uim.GetUnitsAt( xy ).ToList();

					string cursor = null;
					if( underCursor != null )
						if( o.Order.CanTargetUnit( self, underCursor, mi.Modifiers.HasModifier( Modifiers.Ctrl ), mi.Modifiers.HasModifier( Modifiers.Alt ), ref cursor ) )
							return new UnitOrderResult( self, o.Order, o.Trait, cursor, Target.FromActor( underCursor ) );
					if( o.Order.CanTargetLocation( self, xy, actorsAt, mi.Modifiers.HasModifier( Modifiers.Ctrl ), mi.Modifiers.HasModifier( Modifiers.Alt ), ref cursor ) )
						return new UnitOrderResult( self, o.Order, o.Trait, cursor, Target.FromCell( xy ) );
				}
			}

			return null;
		}

		static Order CheckSameOrder( IOrderTargeter iot, Order order )
		{
			if( order == null && iot.OrderID != null )
				Game.Debug( "BUG: in order targeter - decided on {0} but then didn't order", iot.OrderID );
			else if( iot.OrderID != order.OrderString )
				Game.Debug( "BUG: in order targeter - decided on {0} but ordered {1}", iot.OrderID, order.OrderString );
			return order;
		}

		class UnitOrderResult
		{
			public readonly Actor self;
			public readonly IOrderTargeter iot;
			public readonly IIssueOrder trait;
			public readonly string cursor;
			public readonly Target target;

			public UnitOrderResult( Actor self, IOrderTargeter iot, IIssueOrder trait, string cursor, Target target )
			{
				this.self = self;
				this.iot = iot;
				this.trait = trait;
				this.cursor = cursor;
				this.target = target;
			}
		}
	}
}
