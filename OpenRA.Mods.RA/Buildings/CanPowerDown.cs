#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class CanPowerDownInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new CanPowerDown(init); }
	}

	public class CanPowerDown : IResolveOrder, IDisable, INotifyCapture, ISync
	{
		[Sync]
		bool disabled = false;
		int normalPower = 0;
		PowerManager PowerManager;
		TechTree TechTree;
		
		public CanPowerDown(ActorInitializer init)
		{
			PowerManager = init.self.Owner.PlayerActor.Trait<PowerManager>();
			TechTree = init.self.Owner.PlayerActor.Trait<TechTree>();
			normalPower = init.self.Info.Traits.Get<BuildingInfo>().Power;
		}
		public bool Disabled { get { return disabled; } }
				
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PowerDown")
			{
				disabled = !disabled;
				var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.PlayToPlayer(self.Owner, disabled ? eva.EnablePower : eva.DisablePower);
				
				PowerManager.UpdateActor(self, disabled ? 0 : normalPower);
				
				// Rebuild the tech tree; some support powers require active buildings
				TechTree.Update();
			}
		}
		
		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			PowerManager = newOwner.PlayerActor.Trait<PowerManager>();
			TechTree = newOwner.PlayerActor.Trait<TechTree>();
		}
	}
}
