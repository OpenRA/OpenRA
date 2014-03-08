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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ObserverShroudSelectorLogic
	{
		class CameraOption
		{
			public string Label;
			public Func<bool> IsSelected;
			public Action OnClick;

			public CameraOption(string label, Func<bool> isSelected, Action onClick)
			{
				Label = label;
				IsSelected = isSelected;
				OnClick = onClick;
			}
		}

		static string LabelForPlayer(Player p)
		{
			return p != null ? p.PlayerName == "Everyone" ? "Combined view" : "{0}'s view".F(p.PlayerName) : "World view";
		}

		[ObjectCreator.UseCtor]
		public ObserverShroudSelectorLogic(Widget widget, World world)
		{
			var views = world.Players.Where(p => (p.NonCombatant && p.Spectating)
				|| !p.NonCombatant).Concat(new[] { (Player)null }).Select(
					p => new CameraOption(LabelForPlayer(p),
						() => world.RenderPlayer == p,
						() => world.RenderPlayer = p
				)).ToArray();

			var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");
			shroudSelector.GetText = () => LabelForPlayer(world.RenderPlayer);
			shroudSelector.OnMouseDown = _ =>
			{
				Func<CameraOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Label;
					return item;
				};
				shroudSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", views.Length * 30, views, setupItem);
			};
		}
	}
}
