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
		public int Cash = 20000;
		public bool FastBuild = false;
		public bool FastCharge = false;
		
		public object Create (ActorInitializer init) { return new DeveloperMode(this); }
	}
	
	public class DeveloperMode : IResolveOrder
	{
		DeveloperModeInfo Info;
		[Sync]
		public bool FastCharge;
		public bool FastBuild;
		
		public DeveloperMode(DeveloperModeInfo info)
		{
			Info = info;
			FastBuild = Info.FastBuild;
			FastCharge = Info.FastCharge;
		}
		
		public void ResolveOrder (Actor self, Order order)
		{
			switch(order.OrderString)
			{
			case "DevModeFastCharge":
				{
					FastCharge ^= true;
					break;
				}
			case "DevModeFastBuild":
				{
					FastBuild ^= true;
					break;
				}
			}
		}
		
	}
}

