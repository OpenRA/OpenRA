#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LobbyOptionsLogic : ChromeLogic
	{
		readonly ScrollPanelWidget panel;
		readonly Widget optionsContainer;
		readonly Widget checkboxRowTemplate;
		readonly Widget dropdownRowTemplate;
		readonly int yMargin;

		readonly Func<MapPreview> getMap;
		readonly OrderManager orderManager;
		readonly Func<bool> configurationDisabled;
		MapPreview mapPreview;
		bool validOptions;

		[ObjectCreator.UseCtor]
		internal LobbyOptionsLogic(Widget widget, OrderManager orderManager, Func<MapPreview> getMap, Func<bool> configurationDisabled)
		{
			this.getMap = getMap;
			this.orderManager = orderManager;
			this.configurationDisabled = configurationDisabled;

			panel = (ScrollPanelWidget)widget;
			optionsContainer = widget.Get("LOBBY_OPTIONS");
			yMargin = optionsContainer.Bounds.Y;
			optionsContainer.IsVisible = () => validOptions;
			checkboxRowTemplate = optionsContainer.Get("CHECKBOX_ROW_TEMPLATE");
			dropdownRowTemplate = optionsContainer.Get("DROPDOWN_ROW_TEMPLATE");

			mapPreview = getMap();
			RebuildOptions();
		}

		public override void Tick()
		{
			var newMapPreview = getMap();
			if (newMapPreview == mapPreview)
				return;

			if (newMapPreview.RulesLoaded)
			{
				// We are currently enumerating the widget tree and so can't modify any layout
				// Defer it to the end of tick instead
				Game.RunAfterTick(() =>
				{
					mapPreview = newMapPreview;
					RebuildOptions();
					validOptions = true;
				});
			}
			else
				validOptions = false;
		}

		void RebuildOptions()
		{
			if (mapPreview == null || mapPreview.Rules == null || mapPreview.InvalidCustomRules)
				return;

			optionsContainer.RemoveChildren();
			optionsContainer.Bounds.Height = 0;
			var allOptions = mapPreview.Rules.Actors["player"].TraitInfos<ILobbyOptions>()
					.Concat(mapPreview.Rules.Actors["world"].TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(mapPreview.Rules))
					.Where(o => o.IsVisible)
					.OrderBy(o => o.DisplayOrder)
					.ToArray();

			Widget row = null;
			var checkboxColumns = new Queue<CheckboxWidget>();
			var dropdownColumns = new Queue<DropDownButtonWidget>();

			foreach (var option in allOptions.Where(o => o is LobbyBooleanOption))
			{
				if (!checkboxColumns.Any())
				{
					row = checkboxRowTemplate.Clone();
					row.Bounds.Y = optionsContainer.Bounds.Height;
					optionsContainer.Bounds.Height += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is CheckboxWidget)
							checkboxColumns.Enqueue((CheckboxWidget)child);

					optionsContainer.AddChild(row);
				}

				var checkbox = checkboxColumns.Dequeue();
				var optionValue = new CachedTransform<Session.Global, Session.LobbyOptionState>(
					gs => gs.LobbyOptions[option.Id]);

				checkbox.GetText = () => option.Name;
				if (option.Description != null)
					checkbox.GetTooltipText = () => option.Description;

				checkbox.IsVisible = () => true;
				checkbox.IsChecked = () => optionValue.Update(orderManager.LobbyInfo.GlobalSettings).IsEnabled;
				checkbox.IsDisabled = () => configurationDisabled() || optionValue.Update(orderManager.LobbyInfo.GlobalSettings).IsLocked;
				checkbox.OnClick = () => orderManager.IssueOrder(Order.Command(
					"option {0} {1}".F(option.Id, !optionValue.Update(orderManager.LobbyInfo.GlobalSettings).IsEnabled)));
			}

			foreach (var option in allOptions.Where(o => !(o is LobbyBooleanOption)))
			{
				if (!dropdownColumns.Any())
				{
					row = dropdownRowTemplate.Clone() as Widget;
					row.Bounds.Y = optionsContainer.Bounds.Height;
					optionsContainer.Bounds.Height += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is DropDownButtonWidget)
							dropdownColumns.Enqueue((DropDownButtonWidget)child);

					optionsContainer.AddChild(row);
				}

				var dropdown = dropdownColumns.Dequeue();
				var optionValue = new CachedTransform<Session.Global, Session.LobbyOptionState>(
					gs => gs.LobbyOptions[option.Id]);

				var getOptionLabel = new CachedTransform<string, string>(id =>
				{
					string value;
					if (id == null || !option.Values.TryGetValue(id, out value))
						return "Not Available";

					return value;
				});

				dropdown.GetText = () => getOptionLabel.Update(optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value);
				if (option.Description != null)
					dropdown.GetTooltipText = () => option.Description;
				dropdown.IsVisible = () => true;
				dropdown.IsDisabled = () => configurationDisabled() ||
					optionValue.Update(orderManager.LobbyInfo.GlobalSettings).IsLocked;

				dropdown.OnMouseDown = _ =>
				{
					Func<KeyValuePair<string, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (c, template) =>
					{
						Func<bool> isSelected = () => optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value == c.Key;
						Action onClick = () => orderManager.IssueOrder(Order.Command("option {0} {1}".F(option.Id, c.Key)));

						var item = ScrollItemWidget.Setup(template, isSelected, onClick);
						item.Get<LabelWidget>("LABEL").GetText = () => c.Value;
						return item;
					};

					dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", option.Values.Count() * 30, option.Values, setupItem);
				};

				var label = row.GetOrNull<LabelWidget>(dropdown.Id + "_DESC");
				if (label != null)
				{
					label.GetText = () => option.Name;
					label.IsVisible = () => true;
				}
			}

			panel.ContentHeight = yMargin + optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Y = yMargin;
			if (panel.ContentHeight < panel.Bounds.Height)
				optionsContainer.Bounds.Y += (panel.Bounds.Height - panel.ContentHeight) / 2;

			panel.ScrollToTop();
		}
	}
}
