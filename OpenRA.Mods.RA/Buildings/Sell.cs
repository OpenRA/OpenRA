#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA.Buildings
{
	class Sell : IActivity
	{
		IActivity NextActivity { get; set; }

		bool started;

		int framesRemaining;

		void DoSell(Actor self)
		{
			var h = self.TraitOrDefault<Health>();
			var si = self.Info.Traits.Get<SellableInfo>();
			var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			
			var cost = csv != null ? csv.Value : self.Info.Traits.Get<ValuedInfo>().Cost;
			
			var refund = (cost * si.RefundPercent * (h == null ? 1 : h.HP)) / (100 * (h == null ? 1 : h.MaxHP));			
			pr.GiveCash(refund);
			
			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Sold(self);

            if (self.World.LocalPlayer != null && self.Owner.Stances[self.World.LocalPlayer] == Stance.Ally)
                self.World.AddFrameEndTask(w => w.Add(new CashTick(refund, 30, 2, self.CenterLocation, self.Owner.ColorRamp.GetColor(0))));
			
			self.Destroy();
		}

		public IActivity Tick(Actor self)
		{
			if( !started )
			{
				framesRemaining = self.Trait<RenderSimple>().anim.HasSequence("make") 
					? self.Trait<RenderSimple>().anim.GetSequence( "make" ).Length : 0;

				foreach( var ns in self.TraitsImplementing<INotifySold>() )
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

		public void Queue( IActivity activity )
		{
			if( NextActivity != null )
				NextActivity.Queue( activity );
			else
				NextActivity = activity;
		}

		public IEnumerable<float2> GetCurrentPath()
		{
			yield break;
		}
	}
}
