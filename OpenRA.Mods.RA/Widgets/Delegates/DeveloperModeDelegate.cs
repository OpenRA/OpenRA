#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class DeveloperModeDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public DeveloperModeDelegate( [ObjectCreator.Param] World world )
		{
			var devmodeBG = Widget.RootWidget.GetWidget("INGAME_ROOT").GetWidget("DEVELOPERMODE_BG");
			var devModeButton = Widget.RootWidget.GetWidget<ButtonWidget>("INGAME_DEVELOPERMODE_BUTTON");
			
			devModeButton.OnMouseUp = mi =>
			{	
				devmodeBG.Visible ^= true;
				return true;
			};
			
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_SHROUD").BindReadOnly(devTrait, "DisableShroud");
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_SHROUD").OnChange += _ => Order(world, "DevShroud");
	
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_UNITDEBUG").BindReadOnly(devTrait, "UnitInfluenceDebug");
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_UNITDEBUG").OnChange += _ => Order(world, "DevUnitDebug");
			
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_PATHDEBUG").BindReadOnly(devTrait, "PathDebug");
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_PATHDEBUG").OnChange += _ => Order(world, "DevPathDebug");

			devmodeBG.GetWidget<ButtonWidget>("GIVE_CASH").OnMouseUp = mi =>
			{
				world.IssueOrder(new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false));
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_BUILD").BindReadOnly(devTrait, "FastBuild");
			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_BUILD").OnChange += _ => Order(world, "DevFastBuild");

			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_CHARGE").BindReadOnly(devTrait, "FastCharge");
			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_CHARGE").OnChange += _ => Order(world, "DevFastCharge");

			devmodeBG.GetWidget<CheckboxWidget>("ENABLE_TECH").BindReadOnly(devTrait, "AllTech");
			devmodeBG.GetWidget<CheckboxWidget>("ENABLE_TECH").OnChange += _ => Order(world, "DevEnableTech");
			
			devmodeBG.GetWidget<CheckboxWidget>("UNLIMITED_POWER").BindReadOnly(devTrait, "UnlimitedPower");
			devmodeBG.GetWidget<CheckboxWidget>("UNLIMITED_POWER").OnChange += _ => Order(world, "DevUnlimitedPower");

            devmodeBG.GetWidget<CheckboxWidget>("BUILD_ANYWHERE").BindReadOnly(devTrait, "BuildAnywhere");
            devmodeBG.GetWidget<CheckboxWidget>("BUILD_ANYWHERE").OnChange += _ => Order(world, "DevBuildAnywhere");

			devmodeBG.GetWidget<ButtonWidget>("GIVE_EXPLORATION").OnMouseUp = mi =>
			{
				world.IssueOrder(new Order("DevGiveExploration", world.LocalPlayer.PlayerActor, false));
				return true;
			};
				
			devModeButton.IsVisible = () => { return world.LobbyInfo.GlobalSettings.AllowCheats; };
		}
		
		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));	
		}
	}
}
