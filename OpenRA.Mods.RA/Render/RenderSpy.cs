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
using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.RA.Render
{
	class RenderSpyInfo : RenderInfantryInfo
	{
		public override object Create(ActorInitializer init) { return new RenderSpy(init.self); }
	}

	class RenderSpy : RenderInfantry, IRenderModifier, IIssueOrder2, IResolveOrder, IOrderVoice
	{
		Player disguisedAsPlayer;
		string disguisedAsSprite;

		public RenderSpy(Actor self) : base(self) { }

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return disguisedAsPlayer != null ? r.Select(a => a.WithPalette(disguisedAsPlayer.Palette)) : r;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Disguise")
			{
				var target = order.TargetActor == self ? null : order.TargetActor;
				if (target != null && target.IsInWorld)
				{
					disguisedAsPlayer = target.Owner;
					disguisedAsSprite = target.Trait<RenderSimple>().GetImage(target);
					anim.ChangeImage(disguisedAsSprite);
				}
				else
				{
					disguisedAsPlayer = null;
					disguisedAsSprite = null;
					anim.ChangeImage(GetImage(self));
				}
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new UnitTraitOrderTargeter<RenderInfantry>( "Disguise", 5, "ability", true, true ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "Disguise" )
				return new Order( order.OrderID, self, target.Actor );

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Disguise" ? "Attack" : null;
		}
	}
}
