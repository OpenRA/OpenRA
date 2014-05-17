#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA.Buildings
{
	public class CanPowerDownInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new CanPowerDown(init); }
	}

	public class CanPowerDown : IResolveOrder, IDisable, INotifyCapture, ISync
	{
		[Sync] bool disabled = false;
		int normalPower = 0;
		PowerManager PowerManager;

		public CanPowerDown(ActorInitializer init)
		{
			PowerManager = init.self.Owner.PlayerActor.Trait<PowerManager>();
			normalPower = init.self.Info.Traits.Get<BuildingInfo>().Power;
		}

		public bool Disabled { get { return disabled; } }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PowerDown")
			{
				disabled = !disabled;
				Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", (disabled ? "EnablePower" : "DisablePower"), self.Owner.Country.Race);
				PowerManager.UpdateActor(self, disabled ? 0 : normalPower);

				if (disabled)
					self.World.AddFrameEndTask(
						w => w.Add(new PowerdownIndicator(self)));
			}
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			PowerManager = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
