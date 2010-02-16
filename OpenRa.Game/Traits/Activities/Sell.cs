#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

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
