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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Overrides the default ToolTip when this actor is disguised (aids in deceiving enemy players).")]
	class DisguiseToolTipInfo : TooltipInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new DisguiseToolTip(init.self, this); }
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

		public ITooltipInfo TooltipInfo
		{
			get
			{
				return disguise.Disguised ? disguise.AsTooltipInfo : info;
			}
		}

		public Player Owner
		{
			get
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
	}

	[Desc("Provides access to the disguise command, which makes the actor appear to be another player's actor.")]
	class DisguiseInfo : ITraitInfo
	{
		[Desc("Acceptable stances of target's owner.")]
		public readonly Stance TargetPlayers = Stance.All;

		public object Create(ActorInitializer init) { return new Disguise(this); }
	}

	class Disguise : IEffectiveOwner, IIssueOrder, IResolveOrder, IOrderVoice, IRadarColorModifier, INotifyAttack
	{
		readonly DisguiseInfo info;
		public Player AsPlayer { get; private set; }
		public string AsSprite { get; private set; }
		public ITooltipInfo AsTooltipInfo { get; private set; }

		public bool Disguised { get { return AsPlayer != null; } }
		public Player Owner { get { return AsPlayer; } }

		public Disguise(DisguiseInfo info) { this.info = info; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new[] { "Disguise" }, "Disguise", 7, "ability", info.TargetPlayers) { ForceAttack = false };
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
				AsTooltipInfo = tooltip.TooltipInfo;
				AsPlayer = tooltip.Owner;
				AsSprite = target.Trait<RenderSprites>().GetImage(target);
			}
			else
			{
				AsTooltipInfo = null;
				AsPlayer = null;
				AsSprite = null;
			}

			foreach (var t in self.TraitsImplementing<INotifyEffectiveOwnerChanged>())
				t.OnEffectiveOwnerChanged(self, oldEffectiveOwner, AsPlayer);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { DisguiseAs(self, null); }
	}

	[Desc("Allows automatic targeting of disguised actors.")]
	class IgnoresDisguiseInfo : TraitInfo<IgnoresDisguise> { }
	class IgnoresDisguise { }
}