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
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{	
	public class CncDiplomacyLogic
	{
		World world;
		
		[ObjectCreator.UseCtor]
		public CncDiplomacyLogic([ObjectCreator.Param] Widget widget,
		                         [ObjectCreator.Param] Action onExit,
		                         [ObjectCreator.Param] World world)
		{
			this.world = world;
			var panel = widget.GetWidget("DIPLOMACY_PANEL");
			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
			
			var scrollpanel = panel.GetWidget<ScrollPanelWidget>("PLAYER_LIST");
			var itemTemplate = scrollpanel.GetWidget("PLAYER_TEMPLATE");
			scrollpanel.RemoveChildren();
			
			foreach (var p in world.Players.Where(a => a != world.LocalPlayer && !a.NonCombatant))
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
			
			var stances = Enum.GetValues(typeof(Stance)).OfType<Stance>().ToList();
			Func<Stance, ScrollItemWidget, ScrollItemWidget> setupItem = (s, template) =>
			{
				var item = ScrollItemWidget.Setup(template,
				                                  () => s == world.LocalPlayer.Stances[ pp ],
				                                  () => world.IssueOrder(new Order("SetStance", world.LocalPlayer.PlayerActor, false)
				                                  		{ TargetLocation = new int2((int)s, 0), TargetString = pp.InternalName }));
				
				item.GetWidget<LabelWidget>("LABEL").GetText = () => s.ToString();
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, stances, setupItem);
			return true;
		}
	}
}
