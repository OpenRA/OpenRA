#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
namespace OpenRA.Traits
{
	public class DeveloperModeInfo : ITraitInfo
	{
		public int InitialCash = 20000;
		public int BuildSpeed = 1;
		public int ChargeTime = 1;
		
		public object Create(ActorInitializer init) { return new DeveloperMode(this); }
		
	}
	
	public class DeveloperMode : IResolveOrder
	{
		DeveloperModeInfo Info;
		public DeveloperMode (DeveloperModeInfo info)
		{
			Info = info;
		}
	
		public void ResolveOrder (Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "DevModeGiveCash":
					self.World.AddFrameEndTask( w =>
					{
						self.Owner.PlayerActor.traits.Get<PlayerResources>().GiveCash(Info.InitialCash);
					});
				break;
			}
		}
	}
}

