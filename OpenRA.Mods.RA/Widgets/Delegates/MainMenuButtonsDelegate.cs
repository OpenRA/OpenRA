#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;
using System;
using System.Drawing;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class MainMenuButtonsDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public MainMenuButtonsDelegate([ObjectCreator.Param] Widget widget)
		{
			Game.modData.WidgetLoader.LoadWidget( new Dictionary<string,object>(), Widget.RootWidget, "PERF_BG" );
			widget.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp = mi => { Widget.OpenWindow("JOINSERVER_BG"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_CREATE").OnMouseUp = mi => { Widget.OpenWindow("CREATESERVER_BG"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_SETTINGS").OnMouseUp = mi => { Widget.OpenWindow("SETTINGS_MENU"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_MUSIC").OnMouseUp = mi => { Widget.OpenWindow("MUSIC_MENU"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_REPLAY_VIEWER").OnMouseUp = mi => { Widget.OpenWindow("REPLAYBROWSER_BG"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_QUIT").OnMouseUp = mi => { Game.Exit(); return true; };
			
			var selector = widget.GetWidget<ButtonWidget>("QUICKMODSWITCHER");
			selector.OnMouseDown = _ => ShowModsDropDown(selector);
			selector.GetText = ActiveModTitle;
		}
		
		string ActiveModTitle()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Title;
		}
		
		bool ShowModsDropDown(ButtonWidget selector)
		{
			var dropDownOptions = new List<Pair<string, Action>>();
			
			foreach (var kv in Mod.AllMods)
			{
				var modList = new List<string>() { kv.Key };
				var m = kv.Key;
				while (!string.IsNullOrEmpty(Mod.AllMods[m].Requires))
				{
					m = Mod.AllMods[m].Requires;
					modList.Add(m);
				}
					
				dropDownOptions.Add(new Pair<string, Action>( kv.Value.Title,
					() => Game.InitializeWithMods( modList.ToArray() ) ));
			}
				                    
			DropDownButtonWidget.ShowDropDown( selector,
				dropDownOptions,
				(ac, w) => new LabelWidget
				{
					Bounds = new Rectangle(0, 0, w, 24),
					Text = "  {0}".F(ac.First),
					OnMouseUp = mi => { ac.Second(); return true; },
				});
			return true;
		}
	}
}
