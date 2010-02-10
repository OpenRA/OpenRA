using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	class Sell : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool started;

		int framesRemaining;

		void DoSell(Actor self)
		{
			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			var cost = csv != null ? csv.Value : self.Info.Traits.Get<BuildableInfo>().Cost;
			var hp = self.Info.Traits.Get<OwnedActorInfo>().HP;
			var refund = Rules.General.RefundPercent * self.Health * cost / hp;

			self.Owner.GiveCash((int)refund);
			self.Health = 0;
			foreach (var ns in self.traits.WithInterface<INotifySold>())
				ns.Sold(self);
			self.World.AddFrameEndTask( _ => self.World.Remove( self ) );

			// todo: give dudes
		}

		public IActivity Tick(Actor self)
		{
			if( !started )
			{
				framesRemaining = self.traits.Get<RenderSimple>().anim.GetSequence( "make" ).Length;
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
