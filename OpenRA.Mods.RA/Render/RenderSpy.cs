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

namespace OpenRA.Mods.RA.Render
{
	class RenderSpyInfo : RenderInfantryInfo
	{
		public override object Create(ActorInitializer init) { return new RenderSpy(init.self); }
	}

	class RenderSpy : RenderInfantry, IRenderModifier, IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		Actor disguisedAs;

		public RenderSpy(Actor self) : base(self) { }

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return disguisedAs != null ? r.Select(a => a.WithPalette(disguisedAs.Owner.Palette)) : r;
		}

		public override void Tick(Actor self)
		{
			if (disguisedAs != null)
				anim.ChangeImage(disguisedAs.Trait<RenderSimple>().GetImage(disguisedAs));
			else
				anim.ChangeImage(GetImage(self));

			base.Tick(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Disguise")
				disguisedAs = order.TargetActor == self ? null : order.TargetActor;
		}
		
		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return 5;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (underCursor != null && underCursor.HasTrait<RenderInfantry>())
				return new Order("Disguise", self, underCursor);

			return null;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return order.OrderString == "Disguise" ? "ability" : null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Disguise" ? "Attack" : null;
		}
	}
}
