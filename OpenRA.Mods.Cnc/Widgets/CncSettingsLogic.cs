#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncSettingsLogic : IWidgetDelegate
	{	
		enum PanelType
		{
			General,
			Input
		}
		PanelType Settings = PanelType.General;
		
		[ObjectCreator.UseCtor]
		public CncSettingsLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] Action onExit)
		{
			
			var panel = widget.GetWidget("SETTINGS_PANEL");
			
			panel.GetWidget<CncMenuButtonWidget>("SAVE_BUTTON").OnClick = () => {
				Widget.CloseWindow();
				onExit();
			};
			
			panel.GetWidget("GENERAL_CONTROLS").IsVisible = () => Settings == PanelType.General;
			panel.GetWidget("INPUT_CONTROLS").IsVisible = () => Settings == PanelType.Input;
			
			var inputButton = panel.GetWidget<CncMenuButtonWidget>("INPUT_BUTTON");
			inputButton.OnClick = () => Settings = PanelType.Input;
			inputButton.IsDisabled = () => Settings == PanelType.Input;
			
			var generalButton = panel.GetWidget<CncMenuButtonWidget>("GENERAL_BUTTON");
			generalButton.OnClick = () => Settings = PanelType.General;
			generalButton.IsDisabled = () => Settings == PanelType.General;
		}
	}
}
