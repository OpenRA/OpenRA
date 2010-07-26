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
		[Sync]
		public bool FastCharge;
		public bool FastBuild;
		public bool DisableShroud;
		public bool PathDebug;
		
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
				case "DevFastCharge":
					{
						FastCharge ^= true;
						IssueNotification(FastCharge);
						break;
					}
				case "DevFastBuild":
					{
						FastBuild ^= true;
						IssueNotification(FastBuild);
						break;
					}
				case "DevGiveCash":
					{
						self.traits.Get<PlayerResources>().GiveCash(Info.Cash);
						IssueNotification(true);
						break;
					}
				case "DevShroud":
					{
						DisableShroud ^= true;
						Game.world.LocalPlayer.Shroud.Disabled = DisableShroud;
						IssueNotification(DisableShroud);
						break;	
					}
				case "DevPathDebug":
					{
						PathDebug ^= true;
						IssueNotification(PathDebug);
						break;
					}
				case "DevIndexDebug":
					{
						Game.Settings.IndexDebug ^= true;
						IssueNotification(Game.Settings.IndexDebug);
						break;
					}
				case "DevUnitDebug":
					{
						Game.Settings.UnitDebug ^= true;
						IssueNotification(Game.Settings.UnitDebug);
						break;
					}
			}
		}
		
		void IssueNotification(bool enabled)
		{
			if (enabled)
				Game.IssueOrder(Order.Chat("I used a devmode option"));
		}
		
	}
}

