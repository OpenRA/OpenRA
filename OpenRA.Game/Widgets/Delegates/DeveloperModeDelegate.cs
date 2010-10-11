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

namespace OpenRA.Widgets.Delegates
{
	public class DeveloperModeDelegate : IWidgetDelegate
	{
		readonly World world;
		[ObjectCreator.UseCtor]
		public DeveloperModeDelegate( [ObjectCreator.Param] World world )
		{
			this.world = world;
			var devmodeBG = Widget.RootWidget.GetWidget("INGAME_ROOT").GetWidget("DEVELOPERMODE_BG");
			var devModeButton = Widget.RootWidget.GetWidget<ButtonWidget>("INGAME_DEVELOPERMODE_BUTTON");
			
			devModeButton.OnMouseUp = mi =>
			{	
				devmodeBG.Visible ^= true;
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_SHROUD").Checked = 
				() => world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().DisableShroud;
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_SHROUD").OnMouseDown = mi => 
			{
				Game.IssueOrder(new Order("DevShroud", world.LocalPlayer.PlayerActor));
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_UNITDEBUG").Checked = 
				() => Game.Settings.Debug.ShowCollisions;
			devmodeBG.GetWidget("CHECKBOX_UNITDEBUG").OnMouseDown = mi => 
			{
				Game.IssueOrder(new Order("DevUnitDebug", world.LocalPlayer.PlayerActor));
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("CHECKBOX_PATHDEBUG").Checked = 
				() => world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug;
			devmodeBG.GetWidget("CHECKBOX_PATHDEBUG").OnMouseDown = mi => 
			{
				Game.IssueOrder(new Order("DevPathDebug", world.LocalPlayer.PlayerActor));
				return true;
			};
			
			devmodeBG.GetWidget<ButtonWidget>("GIVE_CASH").OnMouseUp = mi =>
			{
				Game.IssueOrder(new Order("DevGiveCash", world.LocalPlayer.PlayerActor));
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_BUILD").Checked =
				() => world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().FastBuild;
			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_BUILD").OnMouseDown = mi =>
			{
				Game.IssueOrder(new Order("DevFastBuild", world.LocalPlayer.PlayerActor));
				return true;
			};	

			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_CHARGE").Checked = 
				() => world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().FastCharge;
			devmodeBG.GetWidget<CheckboxWidget>("INSTANT_CHARGE").OnMouseDown = mi =>
			{
				Game.IssueOrder(new Order("DevFastCharge", world.LocalPlayer.PlayerActor));
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("ENABLE_TECH").Checked = 
				() => world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().AllTech;
			devmodeBG.GetWidget<CheckboxWidget>("ENABLE_TECH").OnMouseDown = mi =>
			{
				Game.IssueOrder(new Order("DevEnableTech", world.LocalPlayer.PlayerActor));
				return true;
			};

			devmodeBG.GetWidget<ButtonWidget>("GIVE_EXPLORATION").OnMouseUp = mi =>
			{
				Game.IssueOrder(new Order("DevGiveExploration", world.LocalPlayer.PlayerActor));
				return true;
			};
				
			devModeButton.IsVisible = () => { return world.LobbyInfo.GlobalSettings.AllowCheats; };
		}
	}
}
