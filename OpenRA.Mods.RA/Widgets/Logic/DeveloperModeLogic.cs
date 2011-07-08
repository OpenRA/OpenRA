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

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DeveloperModeLogic
	{
		[ObjectCreator.UseCtor]
		public DeveloperModeLogic( [ObjectCreator.Param] World world )
		{
			var devmodeBG = Widget.RootWidget.GetWidget("INGAME_ROOT").GetWidget("DEVELOPERMODE_BG");
			var devModeButton = Widget.RootWidget.GetWidget<ButtonWidget>("INGAME_DEVELOPERMODE_BUTTON");
			devModeButton.OnClick = () => devmodeBG.Visible ^= true;

			
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			
			var shroudCheckbox = devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_SHROUD");
			shroudCheckbox.IsChecked = () => devTrait.DisableShroud;
			shroudCheckbox.OnClick = () => Order(world, "DevShroud");
			
			var pathCheckbox = devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_PATHDEBUG");
			pathCheckbox.IsChecked = () => devTrait.PathDebug;
			pathCheckbox.OnClick = () => Order(world, "DevPathDebug");
			
			var fastBuildCheckbox = devmodeBG.GetWidget<CheckboxWidget>("INSTANT_BUILD");
			fastBuildCheckbox.IsChecked = () => devTrait.FastBuild;
			fastBuildCheckbox.OnClick = () => Order(world, "DevFastBuild");
			
			var fastChargeCheckbox = devmodeBG.GetWidget<CheckboxWidget>("INSTANT_CHARGE");
			fastChargeCheckbox.IsChecked = () => devTrait.FastCharge;
			fastChargeCheckbox.OnClick = () => Order(world, "DevFastCharge");

			var allTechCheckbox = devmodeBG.GetWidget<CheckboxWidget>("ENABLE_TECH");
			allTechCheckbox.IsChecked = () => devTrait.AllTech;
			allTechCheckbox.OnClick = () => Order(world, "DevEnableTech");
			
			var powerCheckbox = devmodeBG.GetWidget<CheckboxWidget>("UNLIMITED_POWER");
			powerCheckbox.IsChecked = () => devTrait.UnlimitedPower;
			powerCheckbox.OnClick = () => Order(world, "DevUnlimitedPower");

			var buildAnywhereCheckbox = devmodeBG.GetWidget<CheckboxWidget>("BUILD_ANYWHERE");
			buildAnywhereCheckbox.IsChecked = () => devTrait.BuildAnywhere;
			buildAnywhereCheckbox.OnClick = () => Order(world, "DevBuildAnywhere");
			
			devmodeBG.GetWidget<ButtonWidget>("GIVE_CASH").OnClick = () => 
				world.IssueOrder(new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false));
			
			devmodeBG.GetWidget<ButtonWidget>("GIVE_EXPLORATION").OnClick = () =>
				world.IssueOrder(new Order("DevGiveExploration", world.LocalPlayer.PlayerActor, false));
				
			devModeButton.IsVisible = () => { return world.LobbyInfo.GlobalSettings.AllowCheats; };
		}
		
		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));	
		}
	}
}
