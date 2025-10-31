#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Converts damage to a charge level of a GrantPrerequisiteChargeDrainPower.")]
	public class DrainPrerequisitePowerOnDamageInfo : ConditionalTraitInfo
	{
		[Desc("The OrderName of the GrantPrerequisiteChargeDrainPower to drain.")]
		public readonly string OrderName = "GrantPrerequisiteChargeDrainPowerInfoOrder";

		[Desc("Damage is multiplied by this number when converting damage to drain ticks.")]
		public readonly int DamageMultiplier = 1;

		[Desc("Damage is divided by this number when converting damage to drain ticks.")]
		public readonly int DamageDivisor = 600;

		public override object Create(ActorInitializer init) { return new DrainPrerequisitePowerOnDamage(this); }
	}

	public class DrainPrerequisitePowerOnDamage : ConditionalTrait<DrainPrerequisitePowerOnDamageInfo>, INotifyOwnerChanged, IDamageModifier
	{
		SupportPowerManager spm;

		public DrainPrerequisitePowerOnDamage(DrainPrerequisitePowerOnDamageInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			spm = self.Owner.PlayerActor.Trait<SupportPowerManager>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			spm = newOwner.PlayerActor.Trait<SupportPowerManager>();
		}

		int IDamageModifier.GetDamageModifier(Actor self, Damage damage)
		{
			if (!IsTraitDisabled && damage != null)
			{
				var damageSubTicks = (int)(damage.Value * 100L * Info.DamageMultiplier / Info.DamageDivisor);
				if (spm.Powers.TryGetValue(Info.OrderName, out var spi))
					(spi as GrantPrerequisiteChargeDrainPower.DischargeableSupportPowerInstance)?.Discharge(damageSubTicks);
			}

			return 100;
		}
	}
}
