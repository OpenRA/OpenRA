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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncModBrowserLogic : IWidgetDelegate
	{
		Mod currentMod;
		
		[ObjectCreator.UseCtor]
		public CncModBrowserLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] Action onSwitch,
		                            [ObjectCreator.Param] Action onExit)
		{
			var panel = widget.GetWidget("MODS_PANEL");
			var modList = panel.GetWidget<ScrollPanelWidget>("MOD_LIST");
			var loadButton = panel.GetWidget<ButtonWidget>("LOAD_BUTTON");
			loadButton.OnClick = () =>
			{
				// TODO: This is crap
				var mods = new List<string>() { currentMod.Id };
				var m = currentMod;
				while (!string.IsNullOrEmpty(m.Requires))
				{
					m = Mod.AllMods[currentMod.Requires];
					mods.Add(m.Id);
				}
				
				Game.RunAfterTick(() => 
				{
					Widget.CloseWindow();
					onSwitch();
					Game.InitializeWithMods(mods.ToArray());
				});
			};
			loadButton.IsDisabled = () => currentMod.Id == Game.CurrentMods.Keys.First();
						
			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
			currentMod = Mod.AllMods[Game.modData.Manifest.Mods[0]];
			
			// Mod list
			var modTemplate = modList.GetWidget<ScrollItemWidget>("MOD_TEMPLATE");
			
			foreach (var m in Mod.AllMods)
			{
				var mod = m.Value;
				var item = ScrollItemWidget.Setup(modTemplate, () => currentMod == mod, () => currentMod = mod);
				item.GetWidget<LabelWidget>("TITLE").GetText = () => mod.Title;
				item.GetWidget<LabelWidget>("VERSION").GetText = () => mod.Version;
				item.GetWidget<LabelWidget>("AUTHOR").GetText = () => mod.Author;
				modList.AddChild(item);
			}
				
				
			/*
			// Server info
			var infoPanel = panel.GetWidget("SERVER_INFO");
			infoPanel.IsVisible = () => currentServer != null;
			infoPanel.GetWidget<LabelWidget>("SERVER_IP").GetText = () => currentServer.Address;
			infoPanel.GetWidget<LabelWidget>("SERVER_MODS").GetText = () => ServerBrowserDelegate.GenerateModsLabel(currentServer);
			infoPanel.GetWidget<LabelWidget>("MAP_TITLE").GetText = () => (CurrentMap() != null) ? CurrentMap().Title : "Unknown";
			infoPanel.GetWidget<LabelWidget>("MAP_PLAYERS").GetText = () => GetPlayersLabel(currentServer);
			*/
		}
	}
}
