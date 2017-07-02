#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Overrides the default Tooltip when this actor is disguised (aids in deceiving enemy players).")]
	class DisguiseTooltipInfo : TooltipInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new DisguiseTooltip(init.Self, this); }
	}

	class DisguiseTooltip : ITooltip
	{
		readonly Actor self;
		readonly Disguise disguise;
		TooltipInfo info;

		public DisguiseTooltip(Actor self, TooltipInfo info)
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

	[Flags]
	public enum RevealDisguiseType
	{
		None = 0,
		Attack = 1,
		Damaged = 2,
		SelfHeal = 4,
		Heal = 8,
		Infiltrate = 16,
		Demolish = 32,
		Move = 64,
		Unload = 128,
		Dock = 256
	}

	[Desc("Provides access to the disguise command, which makes the actor appear to be another player's actor.")]
	class DisguiseInfo : ITraitInfo
	{
		[VoiceReference] public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while disguised.")]
		public readonly string DisguisedCondition = null;

		//Added this for more control over when disguises break
		[Desc("Events leading to the actor breaking Disguise. Possible values are: Attack, Move, Unload, Infiltrate, Demolish, Dock, Damaged, Heal and SelfHeal.")]
		public readonly RevealDisguiseType RevealDisguiseOn = RevealDisguiseType.Attack;

		//This is to help narrow down the list of types an actor can be further.
		//It helps prevent cases of say a spy trying to disguise as a tank or a tree when it doesn't have the needed traits to do so.
		[Desc("This is to limit the range of types an actor with the Disguise trait can turn into.",
			"Leave list empty to allow for any type that is targetable by Disguise to be used.",
			"ValidTargets here has the same targets as warhead and autotarget.")]
		public readonly HashSet<string> ValidTargets = new HashSet<string>();

		public object Create(ActorInitializer init) { return new Disguise(init.Self, this); }
	}

	class Disguise : INotifyCreated, IEffectiveOwner, IIssueOrder, IResolveOrder, IOrderVoice, IRadarColorModifier, INotifyAttack, INotifyDamage, ITick, INotifyHarvesterAction
	{
		public Player AsPlayer { get; private set; }
		public string AsSprite { get; private set; }
		public ITooltipInfo AsTooltipInfo { get; private set; }

		public bool Disguised { get { return AsPlayer != null; } }
		public Player Owner { get { return AsPlayer; } }

		readonly Actor self;
		readonly DisguiseInfo info;

		CPos? lastPos;
		
		ConditionManager conditionManager;
		int disguisedToken = ConditionManager.InvalidConditionToken;

		public Disguise(Actor self, DisguiseInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DisguiseOrderTargeter(info) { ForceAttack = false };
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

			if (Disguised != oldDisguiseSetting && conditionManager != null)
			{
				if (Disguised && disguisedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.DisguisedCondition))
					disguisedToken = conditionManager.GrantCondition(self, info.DisguisedCondition);
				else if (!Disguised && disguisedToken != ConditionManager.InvalidConditionToken)
					disguisedToken = conditionManager.RevokeCondition(self, disguisedToken);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel) { if (info.RevealDisguiseOn.HasFlag(RevealDisguiseType.Attack)) DisguiseAs(null); }

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value == 0)
				return;
			
			if ( (info.RevealDisguiseOn.HasFlag(RevealDisguiseType.Damaged) && e.Damage.Value > 0) || 
				(e.Attacker == self && e.Damage.Value < 0 && (info.RevealDisguiseOn.HasFlag(RevealDisguiseType.SelfHeal) || info.RevealDisguiseOn.HasFlag(RevealDisguiseType.Heal)) ))
				DisguiseAs(null);
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDisabled())
				DisguiseAs(null);

			if (info.RevealDisguiseOn.HasFlag(RevealDisguiseType.Move) && (lastPos == null || lastPos.Value != self.Location))
			{
				DisguiseAs(null);
				lastPos = self.Location;
			}
		}

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell, Activity next){}

		void INotifyHarvesterAction.MovingToRefinery(Actor self, CPos targetCell, Activity next){}

		void INotifyHarvesterAction.MovementCancelled(Actor self){}

		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource){}

		void INotifyHarvesterAction.Docked()
		{
			if (info.RevealDisguiseOn.HasFlag(RevealDisguiseType.Dock))
			{
				DisguiseAs(null);
			}
		}

		void INotifyHarvesterAction.Undocked(){}

		class DisguiseOrderTargeter : TargetTypeOrderTargeter
		{
			readonly DisguiseInfo disguisinginfo;

			public DisguiseOrderTargeter(DisguiseInfo info)
				: base(new HashSet<string> { "Disguise" }, "Disguise", 7, "ability", true, true)
			{
				disguisinginfo = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				
				//Does the original check first than checks the list to see if it has anything in it or not, 
				//than if it does it checks to see if the target type name matches anything in the list
				return base.CanTargetActor(self, target, modifiers, ref cursor) && 
					(!disguisinginfo.ValidTargets.Any() || (disguisinginfo.ValidTargets.Any() && 
					disguisinginfo.ValidTargets.Overlaps(target.GetEnabledTargetTypes())));

			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				
				//Does the original check first than checks the list to see if it has anything in it or not, 
				//than if it does it checks to see if the target type name matches anything in the list
				return base.CanTargetFrozenActor(self, target, modifiers, ref cursor) &&
					(!disguisinginfo.ValidTargets.Any() || (disguisinginfo.ValidTargets.Any() &&
					disguisinginfo.ValidTargets.Overlaps(target.TargetTypes)));
			}
		}
	}
}
