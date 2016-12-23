#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Overrides the default ToolTip when this actor is disguised (aids in deceiving enemy players).")]
	class DisguiseToolTipInfo : TooltipInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new DisguiseToolTip(init.Self, this); }
	}

	class DisguiseToolTip : ITooltip
	{
		readonly Actor self;
		readonly Disguise disguise;
		TooltipInfo info;

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
				if (!disguise.Disguised || self.Owner.IsAlliedWith(self.World.RenderPlayer))
					return self.Owner;

				return disguise.AsPlayer;
			}
		}
	}

	[Desc("Provides access to the disguise command, which makes the actor appear to be another player's actor.")]
	class DisguiseInfo : ITraitInfo
	{
		[VoiceReference] public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while disguised.")]
		public readonly string DisguisedCondition = null;

		public object Create(ActorInitializer init) { return new Disguise(init.Self, this); }
	}

	class Disguise : INotifyCreated, IEffectiveOwner, IIssueOrder, IResolveOrder, IOrderVoice, IRadarColorModifier, INotifyAttack
	{
		public Player AsPlayer { get; private set; }
		public string AsSprite { get; private set; }
		public ITooltipInfo AsTooltipInfo { get; private set; }

		public bool Disguised { get { return AsPlayer != null; } }
		public Player Owner { get { return AsPlayer; } }

		readonly Actor self;
		readonly DisguiseInfo info;

		ConditionManager um;
		int disguisedToken = ConditionManager.InvalidConditionToken;

		public Disguise(Actor self, DisguiseInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			um = self.TraitOrDefault<ConditionManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new HashSet<string> { "Disguise" }, "Disguise", 7, "ability", true, true) { ForceAttack = false };
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
				DisguiseAs(target);
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Disguise" ? info.Voice : null;
		}

		public Color RadarColorOverride(Actor self, Color color)
		{
			if (!Disguised || self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return color;

			return color = Game.Settings.Game.UsePlayerStanceColors ? AsPlayer.PlayerStanceColor(self) : AsPlayer.Color.RGB;
		}

		public void DisguiseAs(Actor target)
		{
			var oldDisguiseSetting = Disguised;
			var oldEffectiveOwner = AsPlayer;

			if (target != null)
			{
				// Take the image of the target's disguise, if it exists.
				// E.g., SpyA is disguised as a rifle infantry. SpyB then targets SpyA. We should use the rifle infantry image.
				var targetDisguise = target.TraitOrDefault<Disguise>();
				if (targetDisguise != null && targetDisguise.Disguised)
				{
					AsSprite = targetDisguise.AsSprite;
					AsPlayer = targetDisguise.AsPlayer;
					AsTooltipInfo = targetDisguise.AsTooltipInfo;
				}
				else
				{
					AsSprite = target.Trait<RenderSprites>().GetImage(target);
					var tooltip = target.TraitsImplementing<ITooltip>().FirstOrDefault();
					AsPlayer = tooltip.Owner;
					AsTooltipInfo = tooltip.TooltipInfo;
				}
			}
			else
			{
				AsTooltipInfo = null;
				AsPlayer = null;
				AsSprite = null;
			}

			HandleDisguise(oldEffectiveOwner, oldDisguiseSetting);
		}

		public void DisguiseAs(ActorInfo actorInfo, Player newOwner)
		{
			var oldDisguiseSetting = Disguised;
			var oldEffectiveOwner = AsPlayer;

			var renderSprites = actorInfo.TraitInfoOrDefault<RenderSpritesInfo>();
			AsSprite = renderSprites == null ? null : renderSprites.GetImage(actorInfo, self.World.Map.Rules.Sequences, newOwner.Faction.InternalName);
			AsPlayer = newOwner;
			AsTooltipInfo = actorInfo.TraitInfos<TooltipInfo>().FirstOrDefault();

			HandleDisguise(oldEffectiveOwner, oldDisguiseSetting);
		}

		void HandleDisguise(Player oldEffectiveOwner, bool oldDisguiseSetting)
		{
			foreach (var t in self.TraitsImplementing<INotifyEffectiveOwnerChanged>())
				t.OnEffectiveOwnerChanged(self, oldEffectiveOwner, AsPlayer);

			if (Disguised != oldDisguiseSetting && um != null)
			{
				if (Disguised && disguisedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.DisguisedCondition))
					disguisedToken = um.GrantCondition(self, info.DisguisedCondition);
				else if (!Disguised && disguisedToken != ConditionManager.InvalidConditionToken)
					disguisedToken = um.RevokeCondition(self, disguisedToken);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel) { DisguiseAs(null); }
	}
}