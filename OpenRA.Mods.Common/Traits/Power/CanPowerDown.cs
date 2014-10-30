#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Power
{
	[Desc("The player can disable the power individually on this actor.")]
	public class CanPowerDownInfo : UpgradableTraitInfo, ITraitInfo, Requires<PowerInfo>
	{
		public object Create(ActorInitializer init) { return new CanPowerDown(init.self, this); }
	}

	public class CanPowerDown : UpgradableTrait<CanPowerDownInfo>, IPowerModifier, IResolveOrder, IDisable
	{
		[Sync] bool disabled = false;
		readonly Power power;

		public CanPowerDown(Actor self, CanPowerDownInfo info)
			: base(info)
		{
			power = self.Trait<Power>();
		}

		public bool Disabled { get { return disabled; } }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsTraitDisabled && order.OrderString == "PowerDown")
			{
				disabled = !disabled;
				Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", disabled ? "EnablePower" : "DisablePower", self.Owner.Country.Race);
				power.PlayerPower.UpdateActor(self);

				if (disabled)
					self.World.AddFrameEndTask(w => w.Add(new PowerdownIndicator(self)));
			}
		}

		public int GetPowerModifier()
		{
			return !IsTraitDisabled && disabled ? 0 : 100;
		}
	}
}
