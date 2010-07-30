#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Traits.Activities
{
	class Sell : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool started;

		int framesRemaining;

		void DoSell(Actor self)
		{
			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			var cost = csv != null ? csv.Value : self.Info.Traits.Get<ValuedInfo>().Cost;
			
			var health = self.traits.GetOrDefault<Health>();
			var refundFraction = self.Info.Traits.Get<BuildingInfo>().RefundPercent * (health == null ? 1f : health.HPFraction);

			self.Owner.PlayerActor.traits.Get<PlayerResources>().GiveCash((int)(refundFraction * cost));
			
			foreach (var ns in self.traits.WithInterface<INotifySold>())
				ns.Sold(self);
			self.World.AddFrameEndTask( _ => self.World.Remove( self ) );
		}

		public IActivity Tick(Actor self)
		{
			if( !started )
			{
				framesRemaining = self.traits.Get<RenderSimple>().anim.HasSequence("make") 
					? self.traits.Get<RenderSimple>().anim.GetSequence( "make" ).Length : 0;

				foreach( var ns in self.traits.WithInterface<INotifySold>() )
					ns.Selling( self );

				started = true;
			}
			else if( framesRemaining <= 0 )
				DoSell( self );

			else
				--framesRemaining;

			return this;
		}

		public void Cancel(Actor self) { /* never gonna give you up.. */ }
	}
}
