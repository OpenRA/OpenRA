#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Power
{
	[Desc("The player can disable the power individually on this actor.")]
	public class CanPowerDownInfo : ITraitInfo, Requires<PowerInfo>
	{
		public object Create(ActorInitializer init) { return new CanPowerDown(init.self); }
	}

	public class CanPowerDown : IPowerModifier, IResolveOrder, IDisable, ISync
	{
		[Sync] bool disabled = false;
		readonly Power power;

		public CanPowerDown(Actor self)
		{
			power = self.Trait<Power>();
		}

		public bool Disabled { get { return disabled; } }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PowerDown")
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
			return disabled ? 0 : 100;
		}
	}
}
