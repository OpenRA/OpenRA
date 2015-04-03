#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class EditorShroudSelectorLogic
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
		public EditorShroudSelectorLogic(Widget widget, World world)
		{
			var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");
			shroudSelector.GetText = () => LabelForPlayer(world.RenderPlayer);
			shroudSelector.OnMouseDown = _ =>
			{
				var views = world.Players.Where(p => p.InternalName != "Everyone" && !p.InternalName.StartsWith("Multi")) // TODO: unhide when these work as expected
						.Concat(new[] { (Player)null }).Select(
							p => new CameraOption(LabelForPlayer(p),
								() => world.RenderPlayer == p,
								() => { world.RenderPlayer = p;
									shroudSelector.TextColor = world.RenderPlayer != null ? world.RenderPlayer.Color.RGB
									: ChromeMetrics.Get<Color>("ButtonTextColor"); })).ToArray();

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