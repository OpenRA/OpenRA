#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CanPowerDownInfo : TraitInfo<CanPowerDown> { }

	public class CanPowerDown : IDisable, IPowerModifier, IResolveOrder
	{
		[Sync]
		bool disabled = false;

		public bool Disabled { get { return disabled; } }
		
		public float GetPowerModifier() { return (disabled) ? 0.0f : 1.0f; }	
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PowerDown")
			{
				disabled = !disabled;
				var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.PlayToPlayer(self.Owner, disabled ? eva.EnablePower : eva.DisablePower);
			}
		}
	}
}
