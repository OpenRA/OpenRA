#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;
using System;
using System.Drawing;
using System.Linq;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MainMenuButtonsLogic
	{
		[ObjectCreator.UseCtor]
		public MainMenuButtonsLogic([ObjectCreator.Param] Widget widget)
		{
			Game.modData.WidgetLoader.LoadWidget( new WidgetArgs(), Widget.RootWidget, "PERF_BG" );
			widget.GetWidget<ButtonWidget>("MAINMENU_BUTTON_JOIN").OnClick = () => Widget.OpenWindow("JOINSERVER_BG");
			widget.GetWidget<ButtonWidget>("MAINMENU_BUTTON_CREATE").OnClick = () => Widget.OpenWindow("CREATESERVER_BG");
			widget.GetWidget<ButtonWidget>("MAINMENU_BUTTON_SETTINGS").OnClick = () => Widget.OpenWindow("SETTINGS_MENU");
			widget.GetWidget<ButtonWidget>("MAINMENU_BUTTON_MUSIC").OnClick = () => Widget.OpenWindow("MUSIC_MENU");
			widget.GetWidget<ButtonWidget>("MAINMENU_BUTTON_REPLAY_VIEWER").OnClick = () => Widget.OpenWindow("REPLAYBROWSER_BG");
			widget.GetWidget<ButtonWidget>("MAINMENU_BUTTON_QUIT").OnClick = () => Game.Exit();
			
			DisplayModSelector();
		}
		
		public static void DisplayModSelector()
		{
			var selector = Game.modData.WidgetLoader.LoadWidget( new WidgetArgs(), Widget.RootWidget, "QUICKMODSWITCHER" );
			var switcher = selector.GetWidget<DropDownButtonWidget>("SWITCHER");
			switcher.OnMouseDown = _ => ShowModsDropDown(switcher);
			switcher.GetText = ActiveModTitle;
			selector.GetWidget<LabelWidget>("VERSION").GetText = ActiveModVersion;	
		}
		
		static string ActiveModTitle()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Title;
		}
		
		static string ActiveModVersion()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Version;
		}
		
		static void LoadMod(string mod)
		{
			var mods = new List<string>();
			while (!string.IsNullOrEmpty(mod))
			{
				mods.Add(mod);
				mod = Mod.AllMods[mod].Requires;
			}
			
			if (!Game.CurrentMods.Keys.ToArray().SymmetricDifference(mods.ToArray()).Any()) 
				return;
			
			Game.RunAfterTick(() => 
			{
				Game.InitializeWithMods(mods.ToArray());
			});
		}
		
		static void ShowModsDropDown(DropDownButtonWidget dropdown)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (m, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => m == Game.CurrentMods.Keys.First(), 
				                                  () => LoadMod(m));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => Mod.AllMods[m].Title;
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, Mod.AllMods.Keys.ToList(), setupItem);
		}
	}
}
