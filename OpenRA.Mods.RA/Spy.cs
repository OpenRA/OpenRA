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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SpyToolTipInfo : TooltipInfo, Requires<SpyInfo>
	{
		public override object Create (ActorInitializer init) { return new SpyToolTip(init.self, this); }
	}

	class SpyToolTip : IToolTip
	{
		Actor self;
		TooltipInfo Info;
		Spy spy;

		public string Name()
		{
			if (spy.Disguised)
			{
				if (self.Owner == self.World.LocalPlayer)
					return "{0} ({1})".F(Info.Name, spy.disguisedAsName);
				return spy.disguisedAsName;
			}
			return Info.Name;
		}

		public Player Owner()
		{
			if (spy.Disguised)
			{
				if (self.Owner == self.World.LocalPlayer)
					return self.Owner;
				return spy.disguisedAsPlayer;
			}
			return self.Owner;
		}

		public SpyToolTip( Actor self, TooltipInfo info )
		{
			this.self = self;
			Info = info;
			spy = self.Trait<Spy>();
		}
	}


	class SpyInfo : TraitInfo<Spy> { }

	class Spy : IIssueOrder, IResolveOrder, IOrderVoice, IRadarColorModifier, INotifyAttack
	{
		public Player disguisedAsPlayer;
		public string disguisedAsSprite, disguisedAsName;

		public bool Disguised {  get { return disguisedAsPlayer != null; }	}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter("Disguise", "Disguise", 7, "ability", true, true) { ForceAttack=false };
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Disguise" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Disguise")
			{
				var target = order.TargetActor == self ? null : order.TargetActor;

				if (target != null && target.IsInWorld)
					DisguiseAs(target);
				else
					DropDisguise();
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Disguise") ? "Attack" : null;
		}

		public Color RadarColorOverride(Actor self)
		{
			if (!Disguised || self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return self.Owner.Color.RGB;

			return disguisedAsPlayer.Color.RGB;
		}

		void DisguiseAs(Actor target)
		{
			var tooltip = target.TraitsImplementing<IToolTip>().FirstOrDefault();
			disguisedAsName = tooltip.Name();
			disguisedAsPlayer = tooltip.Owner();
			disguisedAsSprite = target.Trait<RenderSprites>().GetImage(target);
		}

		void DropDisguise()
		{
			disguisedAsName = null;
			disguisedAsPlayer = null;
			disguisedAsSprite = null;
		}

		/* lose our disguise if we attack anything */
		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { DropDisguise(); }
	}

	class IgnoresDisguiseInfo : TraitInfo<IgnoresDisguise> {}
	class IgnoresDisguise {}
}
