#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Collections.Generic;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA
{
	class SpyInfo : TraitInfo<Spy> { }

	class Spy : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public Player disguisedAsPlayer;
		public string disguisedAsSprite;
		
		public IEnumerable<IOrderTargeter> Orders
		{
			get 
			{ 
				yield return new UnitTraitOrderTargeter<IAcceptSpy>( "SpyInfiltrate", 5, "enter", true, false ); 
				yield return new UnitTraitOrderTargeter<RenderInfantry>( "Disguise", 5, "ability", true, true );
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "SpyInfiltrate" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
			if( order.OrderID == "Disguise" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpyInfiltrate")
			{
				self.SetTargetLine(Target.FromOrder(order), Color.Red);
				
				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor));
				self.QueueActivity(new Infiltrate(order.TargetActor));
			}
			if (order.OrderString == "Disguise")
			{
				var target = order.TargetActor == self ? null : order.TargetActor;
				if (target != null && target.IsInWorld)
				{
					disguisedAsPlayer = target.Owner;
					disguisedAsSprite = target.Trait<RenderSimple>().GetImage(target);
				}
				else
				{
					disguisedAsPlayer = null;
					disguisedAsSprite = null;
				}
			}
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Disguise" ? "Attack" : null;
		}
	}
}
