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
		public bool DisableShroud = false;
		public bool PathDebug = false;
		
		public object Create (ActorInitializer init) { return new DeveloperMode(this); }
	}
	
	public class DeveloperMode : IResolveOrder
	{
		DeveloperModeInfo Info;
		[Sync] public bool FastCharge;
		[Sync] public bool AllTech;
		[Sync] public bool FastBuild;
		[Sync] public bool DisableShroud;
		[Sync] public bool PathDebug;
		
		public DeveloperMode(DeveloperModeInfo info)
		{
			Info = info;
			FastBuild = Info.FastBuild;
			FastCharge = Info.FastCharge;
			DisableShroud = Info.DisableShroud;
			PathDebug = Info.PathDebug;
		}
		
		public void ResolveOrder (Actor self, Order order)
		{
			if (!Game.LobbyInfo.GlobalSettings.AllowCheats) return;
			
			switch(order.OrderString)
			{
				case "DevEnableTech":
					{
						AllTech ^= true;
						break;
					}
				case "DevFastCharge":
					{
						FastCharge ^= true;
						break;
					}
				case "DevFastBuild":
					{
						FastBuild ^= true;
						break;
					}
				case "DevGiveCash":
					{
						self.Trait<PlayerResources>().GiveCash(Info.Cash);
						break;
					}
				case "DevShroud":
					{
						if (self.World.LocalPlayer == self.Owner)
						{
							DisableShroud ^= true;
							Game.world.LocalPlayer.Shroud.Disabled = DisableShroud;
						}
						break;	
					}
				case "DevPathDebug":
					{
						if (self.World.LocalPlayer == self.Owner)
							PathDebug ^= true;
						break;
					}
				case "DevUnitDebug":
					{
						if (self.World.LocalPlayer == self.Owner)
							Game.Settings.Debug.ShowCollisions ^= true;
						break;
					}
			}
		}
	}
}

