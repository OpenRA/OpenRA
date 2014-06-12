#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class DisguiseToolTipInfo : TooltipInfo, Requires<DisguiseInfo>
	{
		public override object Create (ActorInitializer init) { return new DisguiseToolTip(init.self, this); }
	}

	class DisguiseToolTip : IToolTip
	{
		Actor self;
		TooltipInfo info;
		Disguise disguise;

		public DisguiseToolTip(Actor self, TooltipInfo info)
		{
			this.self = self;
			this.info = info;
			disguise = self.Trait<Disguise>();
		}

		public string Name()
		{
			if (disguise.Disguised)
			{
				if (self.Owner == self.World.LocalPlayer)
					return "{0} ({1})".F(info.Name, disguise.AsName);

				return disguise.AsName;
			}
			return info.Name;
		}

		public Player Owner()
		{
			if (disguise.Disguised)
			{
				if (self.Owner == self.World.LocalPlayer)
					return self.Owner;

				return disguise.AsPlayer;
			}
			return self.Owner;
		}
	}

	public class DisguiseInfo : TraitInfo<Disguise> { }

	public class Disguise : IEffectiveOwner, IIssueOrder, IResolveOrder, IOrderVoice, IRadarColorModifier, INotifyAttack
	{
		public Player AsPlayer;
		public string AsSprite;
		public string AsName;

		public bool Disguised { get { return AsPlayer != null; } }
		public Player Owner { get { return AsPlayer; } }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter("Disguise", "Disguise", 7, "ability", true, true) { ForceAttack = false };
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Disguise")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Disguise")
			{
				var target = order.TargetActor != self && order.TargetActor.IsInWorld ? order.TargetActor : null;
				DisguiseAs(self, target);
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Disguise" ? "Attack" : null;
		}

		public Color RadarColorOverride(Actor self)
		{
			if (!Disguised || self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return self.Owner.Color.RGB;

			return AsPlayer.Color.RGB;
		}

		void DisguiseAs(Actor self, Actor target)
		{
			var oldEffectiveOwner = AsPlayer;

			if (target != null)
			{
				var tooltip = target.TraitsImplementing<IToolTip>().FirstOrDefault();
				AsName = tooltip.Name();
				AsPlayer = tooltip.Owner();
				AsSprite = target.Trait<RenderSprites>().GetImage(target);
			}
			else
			{
				AsName = null;
				AsPlayer = null;
				AsSprite = null;
			}

			foreach (var t in self.TraitsImplementing<INotifyEffectiveOwnerChanged>())
				t.OnEffectiveOwnerChanged(self, oldEffectiveOwner, AsPlayer);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { DisguiseAs(self, null); }
	}

	class IgnoresDisguiseInfo : TraitInfo<IgnoresDisguise> {}
	class IgnoresDisguise {}
}