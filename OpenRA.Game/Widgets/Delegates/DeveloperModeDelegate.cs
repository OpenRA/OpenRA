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
		
		float oldBuildSpeed = 0;
		bool slowed = false;

		public DeveloperModeDelegate ()
		{
			var devmodeBG = Chrome.rootWidget.GetWidget("INGAME_ROOT").GetWidget("DEVELOPERMODE_BG");
			var devModeButton = Chrome.rootWidget.GetWidget<ButtonWidget>("INGAME_DEVELOPERMODE_BUTTON");
			
			devModeButton.OnMouseUp = mi =>
			{	
				devmodeBG.Visible ^= true;
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_SHROUD").Checked = 
				() => Game.world.LocalPlayer.Shroud.Disabled;
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_SHROUD").OnMouseDown = mi => 
			{
				Game.world.LocalPlayer.Shroud.Disabled ^= true;
				TriggerCheatingMessage();
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_UNITDEBUG").Checked = 
				() => {return Game.Settings.UnitDebug;};
			devmodeBG.GetWidget("SETTINGS_CHECKBOX_UNITDEBUG").OnMouseDown = mi => {
				Game.Settings.UnitDebug ^= true;
				TriggerCheatingMessage();
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_PATHDEBUG").Checked = 
				() => {return Game.Settings.PathDebug;};
			devmodeBG.GetWidget("SETTINGS_CHECKBOX_PATHDEBUG").OnMouseDown = mi => {
				Game.Settings.PathDebug ^= true;
				TriggerCheatingMessage();
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_INDEXDEBUG").Checked = 
				() => {return Game.Settings.IndexDebug;};
			devmodeBG.GetWidget("SETTINGS_CHECKBOX_INDEXDEBUG").OnMouseDown = mi => {
				Game.Settings.IndexDebug ^= true;
				TriggerCheatingMessage();
				return true;
			};
			
			devmodeBG.GetWidget<ButtonWidget>("SETTINGS_GIVE_CASH").OnMouseUp = mi =>
			{
				Game.world.AddFrameEndTask(w =>
				{
					Game.world.LocalPlayer.PlayerActor.traits.Get<PlayerResources>().GiveCash(5000);
				});
				TriggerCheatingMessage();
				return true;
			};
			
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_BUILD_SPEED").OnMouseDown = mi =>
			{
				oldBuildSpeed = (!slowed)? Game.world.LocalPlayer.PlayerActor.Info.Traits.Get<ProductionQueueInfo>().BuildSpeed : oldBuildSpeed;
				Game.world.LocalPlayer.PlayerActor.Info.Traits.Get<ProductionQueueInfo>().BuildSpeed = (slowed)? oldBuildSpeed : 0;
				slowed ^= true;
				TriggerCheatingMessage();
				return true;	
			};
			devmodeBG.GetWidget<CheckboxWidget>("SETTINGS_BUILD_SPEED").Checked =
				() => {return slowed;};
				
			devModeButton.IsVisible = () => { return Game.Settings.DeveloperMode; };
		}
		
		void TriggerCheatingMessage()
		{
			Game.Debug("{0} has used a developer mode option that is considered a cheat!".F(Game.world.LocalPlayer.PlayerName.ToString()));
		}
	}
}
