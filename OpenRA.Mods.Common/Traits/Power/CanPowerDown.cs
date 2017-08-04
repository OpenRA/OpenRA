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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The player can disable the power individually on this actor.")]
	public class CanPowerDownInfo : ConditionalTraitInfo, Requires<PowerInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string PowerdownCondition = null;

		[Desc("Restore power when this trait is disabled.")]
		public readonly bool CancelWhenDisabled = false;

		public readonly string PowerupSound = null;
		public readonly string PowerdownSound = null;

		public readonly string PowerupSpeech = null;
		public readonly string PowerdownSpeech = null;

		public override object Create(ActorInitializer init) { return new CanPowerDown(init.Self, this); }
	}

	public class CanPowerDown : ConditionalTrait<CanPowerDownInfo>, IPowerModifier, IResolveOrder, INotifyOwnerChanged
	{
		[Sync] bool poweredDown = false;
		PowerManager power;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public CanPowerDown(Actor self, CanPowerDownInfo info)
			: base(info)
		{
			power = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		protected override void TraitEnabled(Actor self)
		{
			Update(self);
			power.UpdateActor(self);
		}

		void Update(Actor self)
		{
			if (conditionManager == null)
				return;

			if (poweredDown && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.PowerdownCondition);
			else if (!poweredDown && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (!IsTraitDisabled && order.OrderString == "PowerDown")
			{
				poweredDown = !poweredDown;

				if (Info.PowerupSound != null && poweredDown)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.PowerupSound, self.Owner.Faction.InternalName);

				if (Info.PowerdownSound != null && !poweredDown)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.PowerdownSound, self.Owner.Faction.InternalName);

				if (Info.PowerupSpeech != null && poweredDown)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PowerupSpeech, self.Owner.Faction.InternalName);

				if (Info.PowerdownSpeech != null && !poweredDown)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PowerdownSpeech, self.Owner.Faction.InternalName);

				Update(self);
				power.UpdateActor(self);
			}
		}

		int IPowerModifier.GetPowerModifier()
		{
			return !IsTraitDisabled && poweredDown ? 0 : 100;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!poweredDown || !Info.CancelWhenDisabled)
				return;

			poweredDown = false;

			if (Info.PowerupSound != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sound", Info.PowerupSound, self.Owner.Faction.InternalName);

			if (Info.PowerupSpeech != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PowerupSpeech, self.Owner.Faction.InternalName);

			Update(self);
			power.UpdateActor(self);
		}
	}
}
