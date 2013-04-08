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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
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

		[ObjectCreator.UseCtor]
		public ObserverShroudSelectorLogic(Widget widget, World world)
		{
			var views = world.Players.Where(p => !p.NonCombatant).ToDictionary(p => p.PlayerName,
				p => new CameraOption("{0}'s view".F(p.PlayerName),
				      () => world.RenderedPlayer == p,
					  () => { world.RenderedPlayer = p; world.RenderedShroud.Dirty(); }
			));
			views.Add("", new CameraOption("World view",
				() => world.RenderedPlayer == null,
				() => { world.RenderedPlayer = null; world.RenderedShroud.Dirty(); }
			));

			var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");
			shroudSelector.GetText = () => views[world.RenderedPlayer == null ? "" : world.RenderedPlayer.PlayerName].Label;
			shroudSelector.OnMouseDown = _ =>
			{
				Func<CameraOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Label;
					return item;
				};
				shroudSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", views.Count() * 30, views.Values, setupItem);
			};
		}
	}
}
