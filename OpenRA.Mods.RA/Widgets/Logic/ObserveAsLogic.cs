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
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ObserveAsLogic
	{
		[ObjectCreator.UseCtor]
		public ObserveAsLogic(World world)
		{
			var r = Widget.RootWidget;
			var gameRoot = r.GetWidget("OBSERVER_ROOT");
			if(gameRoot == null) { 
				gameRoot = r.GetWidget("INGAME_ROOT");
			}
			var selector = gameRoot.GetWidget<DropDownButtonWidget>("OBSERVEAS_DROPDOWN");
			
			selector.OnMouseDown = _ => ShowWindowModeDropdown(selector, world);
		}
		
		public static bool ShowWindowModeDropdown(DropDownButtonWidget selector, World world)
		{
			/**
				Select all the 'active' players in this world and push them into a dictionary
				using their name as a key.
			**/
			var options = world.Players.Where(a => !a.NonCombatant).ToDictionary(p => p.PlayerName);
			options.Add("[Global View]", null);
			
			// This Func tells each option how to conduct itself: text & actions
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				// params: dropdown id, "selected", "onclick action"
				var s = (options[o] == null) ? null : options[o];
				var item = ScrollItemWidget.Setup(itemTemplate, () => world.RenderedPlayer == s, () => { world.RenderedPlayer = s; world.RenderedShroud.Jank(); } );
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};
			
			selector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}
	}
}