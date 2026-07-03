#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LobbyOptionsLogic : ChromeLogic
	{
		[FluentReference]
		const string NotAvailable = "label-not-available";

		[FluentReference]
		const string AdaptiveAiControlsTitle = "label-adaptive-ai-controls";

		[FluentReference]
		const string InstantBuildingOptionsTitle = "label-instant-building-options";

		readonly ScrollPanelWidget panel;
		readonly Widget optionsContainer;
		readonly Widget checkboxRowTemplate;
		readonly Widget dropdownRowTemplate;
		readonly Widget adaptiveSeparatorTemplate;
		readonly Widget adaptiveTitleTemplate;
		readonly Widget checkboxSeparatorTemplate;
		readonly Widget instantBuildingSubCheckboxRowTemplate;
		readonly int yMargin;

		readonly Func<MapPreview> getMap;
		readonly OrderManager orderManager;
		readonly Func<bool> configurationDisabled;
		MapPreview mapPreview;
		MapStatus mapStatus;
		bool hadAdaptiveBot;
		bool hadInstantBuilding;

		[ObjectCreator.UseCtor]
		internal LobbyOptionsLogic(Widget widget, OrderManager orderManager, Func<MapPreview> getMap, Func<bool> configurationDisabled)
		{
			this.getMap = getMap;
			this.orderManager = orderManager;
			this.configurationDisabled = configurationDisabled;

			panel = (ScrollPanelWidget)widget;
			optionsContainer = widget.Get("LOBBY_OPTIONS");
			yMargin = optionsContainer.Bounds.Y;
			checkboxRowTemplate = optionsContainer.Get("CHECKBOX_ROW_TEMPLATE");
			dropdownRowTemplate = optionsContainer.Get("DROPDOWN_ROW_TEMPLATE");
			adaptiveSeparatorTemplate = optionsContainer.GetOrNull("ADAPTIVE_SECTION_SEPARATOR_TEMPLATE");
			adaptiveTitleTemplate = optionsContainer.GetOrNull("ADAPTIVE_SECTION_TITLE_TEMPLATE");
			checkboxSeparatorTemplate = optionsContainer.GetOrNull("CHECKBOX_SECTION_SEPARATOR_TEMPLATE");
			instantBuildingSubCheckboxRowTemplate = optionsContainer.GetOrNull("INSTANT_BUILDING_SUBCHECKBOX_ROW_TEMPLATE");

			mapPreview = getMap();
			mapStatus = mapPreview.Status;
			hadInstantBuilding = orderManager.LobbyInfo.GlobalSettings.OptionOrDefault(InstantBuilding.MainOptionId, false);
			RebuildOptions();
		}

		public override void Tick()
		{
			var newMapPreview = getMap();
			var hasAdaptiveBot = orderManager.LobbyInfo.Clients.Any(c => c.Bot == "adaptive");
			var hasInstantBuilding = orderManager.LobbyInfo.GlobalSettings.OptionOrDefault(InstantBuilding.MainOptionId, false);
			if (newMapPreview == mapPreview && mapStatus == mapPreview.Status
				&& hasAdaptiveBot == hadAdaptiveBot && hasInstantBuilding == hadInstantBuilding)
				return;

			// We are currently enumerating the widget tree and so can't modify any layout
			// Defer it to the end of tick instead
			Game.RunAfterTick(() =>
			{
				mapPreview = newMapPreview;
				mapStatus = mapPreview.Status;
				hadAdaptiveBot = hasAdaptiveBot;
				hadInstantBuilding = hasInstantBuilding;
				RebuildOptions();
			});
		}

		void RebuildOptions()
		{
			if (mapPreview == null || mapPreview.WorldActorInfo == null)
				return;

			optionsContainer.RemoveChildren();
			optionsContainer.Bounds.Height = 0;
			var allOptions = mapPreview.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(mapPreview.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(mapPreview))
					.Where(o => o.IsVisible)
					.OrderBy(o => o.DisplayOrder)
					.ToArray();

			var hasAdaptiveBot = orderManager.LobbyInfo.Clients.Any(c => c.Bot == "adaptive");
			var adaptiveDropdownOptions = allOptions
				.Where(o => o is not LobbyBooleanOption && o.Id.StartsWith("adaptive-"))
				.ToArray();
			var standardDropdownOptions = allOptions
				.Where(o => o is not LobbyBooleanOption && !o.Id.StartsWith("adaptive-"))
				.ToArray();
			var standardCheckboxOptions = allOptions
				.Where(o => o is LobbyBooleanOption && !InstantBuilding.SubOptionIds.Contains(o.Id))
				.ToArray();
			var instantBuildingSubOptions = allOptions
				.Where(o => o is LobbyBooleanOption && InstantBuilding.SubOptionIds.Contains(o.Id))
				.OrderBy(o => o.DisplayOrder)
				.ToArray();
			var showAdaptiveSection = adaptiveDropdownOptions.Length > 0 && adaptiveSeparatorTemplate != null;

			Widget row = null;
			var checkboxColumns = new Queue<CheckboxWidget>();
			var dropdownColumns = new Queue<DropDownButtonWidget>();
			Widget dropdownRow = null;

			foreach (var option in standardCheckboxOptions)
			{
				if (checkboxColumns.Count == 0)
				{
					row = checkboxRowTemplate.Clone();
					row.Bounds.Y = optionsContainer.Bounds.Height;
					optionsContainer.Bounds.Height += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is CheckboxWidget childCheckbox)
							checkboxColumns.Enqueue(childCheckbox);

					optionsContainer.AddChild(row);
				}

				SetupCheckboxOption(option, checkboxColumns.Dequeue());
			}

			var instantBuildingEnabled = orderManager.LobbyInfo.GlobalSettings.OptionOrDefault(InstantBuilding.MainOptionId, false);

			if (instantBuildingEnabled && checkboxSeparatorTemplate != null && instantBuildingSubOptions.Length > 0)
				AddCheckboxSeparator();

			if (instantBuildingEnabled && instantBuildingSubOptions.Length > 0)
				AddInstantBuildingTitle();

			AddInstantBuildingSubCheckboxes(instantBuildingSubOptions, instantBuildingEnabled);

			if (checkboxSeparatorTemplate != null && standardDropdownOptions.Length > 0)
				AddCheckboxSeparator();

			foreach (var option in standardDropdownOptions)
				SetupDropdownOption(option, ref dropdownRow, dropdownColumns);

			if (showAdaptiveSection)
			{
				HideUnusedDropdownColumns(dropdownRow, dropdownColumns);
				dropdownColumns.Clear();
				dropdownRow = null;

				AddAdaptiveSeparator();

				if (hasAdaptiveBot)
				{
					AddAdaptiveTitle();
					foreach (var option in adaptiveDropdownOptions)
						SetupDropdownOption(option, ref dropdownRow, dropdownColumns);
				}
			}
			else
			{
				foreach (var option in adaptiveDropdownOptions)
					SetupDropdownOption(option, ref dropdownRow, dropdownColumns);
			}

			panel.ContentHeight = yMargin + optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Y = yMargin;

			panel.ScrollToTop();
		}

		void SetupCheckboxOption(LobbyOption option, CheckboxWidget checkbox)
		{
			var optionEnabled = new PredictedCachedTransform<Session.Global, bool>(
				gs => gs.LobbyOptions[option.Id].IsEnabled);

			var optionLocked = new CachedTransform<Session.Global, bool>(
				gs => gs.LobbyOptions[option.Id].IsLocked);

			checkbox.GetText = () => option.Name;
			if (option.Description != null)
			{
				var (text, desc) = LobbyUtils.SplitOnFirstToken(option.Description);
				checkbox.GetTooltipText = () => text;
				checkbox.GetTooltipDesc = () => desc;
			}

			checkbox.IsVisible = () => true;
			checkbox.IsChecked = () => optionEnabled.Update(orderManager.LobbyInfo.GlobalSettings);
			checkbox.IsDisabled = () => configurationDisabled() || optionLocked.Update(orderManager.LobbyInfo.GlobalSettings);
			checkbox.OnClick = () =>
			{
				var state = !optionEnabled.Update(orderManager.LobbyInfo.GlobalSettings);
				orderManager.IssueOrder(Order.Command($"option {option.Id} {state}"));
				optionEnabled.Predict(state);

				if (option.Id == InstantBuilding.MainOptionId)
				{
					hadInstantBuilding = state;
					Game.RunAfterTick(RebuildOptions);
				}
			};
		}

		void AddInstantBuildingSubCheckboxes(LobbyOption[] subOptions, bool enabled)
		{
			if (!enabled || instantBuildingSubCheckboxRowTemplate == null || subOptions.Length == 0)
				return;

			var checkboxColumns = new Queue<CheckboxWidget>();

			foreach (var option in subOptions)
			{
				if (checkboxColumns.Count == 0)
				{
					var row = instantBuildingSubCheckboxRowTemplate.Clone();
					row.IsVisible = () => true;
					row.Bounds.Y = optionsContainer.Bounds.Height;
					optionsContainer.Bounds.Height += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is CheckboxWidget childCheckbox)
							checkboxColumns.Enqueue(childCheckbox);

					optionsContainer.AddChild(row);
				}

				SetupCheckboxOption(option, checkboxColumns.Dequeue());
			}

			while (checkboxColumns.Count > 0)
				checkboxColumns.Dequeue().IsVisible = () => false;
		}

		void AddCheckboxSeparator()
		{
			var separator = checkboxSeparatorTemplate.Clone();
			separator.Bounds.Y = optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Height += separator.Bounds.Height;
			optionsContainer.AddChild(separator);
		}

		void SetupDropdownOption(LobbyOption option, ref Widget dropdownRow, Queue<DropDownButtonWidget> dropdownColumns)
		{
			if (dropdownColumns.Count == 0)
			{
				dropdownRow = dropdownRowTemplate.Clone();
				dropdownRow.Bounds.Y = optionsContainer.Bounds.Height;
				optionsContainer.Bounds.Height += dropdownRow.Bounds.Height;
				foreach (var child in dropdownRow.Children)
					if (child is DropDownButtonWidget dropDown)
						dropdownColumns.Enqueue(dropDown);

				optionsContainer.AddChild(dropdownRow);
			}

			var dropdown = dropdownColumns.Dequeue();
			var optionValue = new CachedTransform<Session.Global, Session.LobbyOptionState>(
				gs => gs.LobbyOptions[option.Id]);

			var getOptionLabel = new CachedTransform<string, string>(id =>
			{
				if (id == null || !option.Values.TryGetValue(id, out var value))
					return FluentProvider.GetMessage(NotAvailable);

				return value;
			});

			dropdown.GetText = () => getOptionLabel.Update(optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value);
			if (option.Description != null)
			{
				var (text, desc) = LobbyUtils.SplitOnFirstToken(option.Description);
				dropdown.GetTooltipText = () => text;
				dropdown.GetTooltipDesc = () => desc;
			}

			dropdown.IsVisible = () => true;
			dropdown.IsDisabled = () => configurationDisabled() ||
				optionValue.Update(orderManager.LobbyInfo.GlobalSettings).IsLocked;

			dropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(KeyValuePair<string, string> c, ScrollItemWidget template)
				{
					bool IsSelected() => optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value == c.Key;
					void OnClick() => orderManager.IssueOrder(Order.Command($"option {option.Id} {c.Key}"));

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => c.Value;
					return item;
				}

				dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", option.Values.Count * 30, option.Values, SetupItem);
			};

			var label = dropdownRow.GetOrNull<LabelWidget>(dropdown.Id + "_DESC");
			if (label != null)
			{
				label.GetText = () => option.Name + ":";
				label.IsVisible = () => true;
			}
		}

		static void HideUnusedDropdownColumns(Widget dropdownRow, Queue<DropDownButtonWidget> dropdownColumns)
		{
			if (dropdownRow == null)
				return;

			while (dropdownColumns.Count > 0)
			{
				var dropdown = dropdownColumns.Dequeue();
				dropdown.IsVisible = () => false;

				var label = dropdownRow.GetOrNull<LabelWidget>(dropdown.Id + "_DESC");
				if (label != null)
					label.IsVisible = () => false;
			}
		}

		void AddAdaptiveSeparator()
		{
			var separator = adaptiveSeparatorTemplate.Clone();
			separator.Bounds.Y = optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Height += separator.Bounds.Height;
			optionsContainer.AddChild(separator);
		}

		void AddAdaptiveTitle()
		{
			if (adaptiveTitleTemplate == null)
				return;

			var title = (LabelWidget)adaptiveTitleTemplate.Clone();
			title.Bounds.Y = optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Height += title.Bounds.Height;
			title.GetText = () => FluentProvider.GetMessage(AdaptiveAiControlsTitle);
			optionsContainer.AddChild(title);
		}

		void AddInstantBuildingTitle()
		{
			if (adaptiveTitleTemplate == null)
				return;

			var title = (LabelWidget)adaptiveTitleTemplate.Clone();
			title.Bounds.Y = optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Height += title.Bounds.Height;
			title.GetText = () => FluentProvider.GetMessage(InstantBuildingOptionsTitle);
			optionsContainer.AddChild(title);
		}
	}
}
