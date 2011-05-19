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
using System.Drawing;
using OpenRA.Mods.RA;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using System.Linq;
using System.Collections.Generic;

namespace OpenRA.Mods.Cnc.Widgets
{	
	public class CncDiplomacyLogic : IWidgetDelegate
	{
		World world;
		
		[ObjectCreator.UseCtor]
		public CncDiplomacyLogic([ObjectCreator.Param] Widget widget,
		                          [ObjectCreator.Param] World world)
		{
			this.world = world;
			var panel = widget.GetWidget("DIPLOMACY_PANEL");
			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = Widget.CloseWindow;
			
			var scrollpanel = panel.GetWidget<ScrollPanelWidget>("PLAYER_LIST");
			var itemTemplate = scrollpanel.GetWidget("PLAYER_TEMPLATE");
			scrollpanel.RemoveChildren();
			
			foreach (var p in world.players.Values.Where(a => a != world.LocalPlayer && !a.NonCombatant))
			{
				Player pp = p;
				var item = itemTemplate.Clone();
				var nameLabel = item.GetWidget<LabelWidget>("NAME");
				nameLabel.GetText = () => pp.PlayerName;
				nameLabel.Color = pp.ColorRamp.GetColor(0);
				
				var flag = item.GetWidget<ImageWidget>("FACTIONFLAG");
				flag.GetImageName = () => pp.Country.Race;
				flag.GetImageCollection = () => "flags";
				item.GetWidget<LabelWidget>("FACTION").GetText = () => pp.Country.Name;
				
				var stance = item.GetWidget<DropDownButtonWidget>("STANCE");
				stance.GetText = () => world.LocalPlayer.Stances[ pp ].ToString();
				stance.OnMouseDown = _ => ShowStanceDropDown(stance, pp);
				stance.IsDisabled = () => pp.IsBot || world.LobbyInfo.GlobalSettings.LockTeams;
				scrollpanel.AddChild(item);
			}
		}
		
		bool ShowStanceDropDown(DropDownButtonWidget dropdown, Player pp)
		{
			if (dropdown.IsDisabled())
				return true;
			
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", dropdown.Bounds.Width }};
			var panel = (ScrollPanelWidget)Widget.LoadWidget("LABEL_DROPDOWN_TEMPLATE", null, new WidgetArgs()
			{
				{ "substitutions", substitutions }
			});
			
			var itemTemplate = panel.GetWidget<ScrollItemWidget>("TEMPLATE");

			foreach (var option in Enum.GetValues(typeof(Stance)).OfType<Stance>())
			{
				var o = option;
				var item = ScrollItemWidget.Setup(itemTemplate, () => o == world.LocalPlayer.Stances[ pp ],
				                                  () => {
														world.IssueOrder(new Order("SetStance", world.LocalPlayer.PlayerActor,
															false) { TargetLocation = new int2(pp.Index, (int)o) });
														dropdown.RemovePanel();
												  });
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o.ToString();
				panel.AddChild(item);
			}
			
			panel.Bounds.Height = Math.Min(150, panel.ContentHeight);
			dropdown.AttachPanel(panel);
			return true;
		}
	}
}
