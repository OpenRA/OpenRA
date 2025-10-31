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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class GameplaySettingsLogic : ChromeLogic
	{
		[FluentReference]
		const string AutoSaveIntervalOptions = "auto-save-interval.options";

		[FluentReference]
		const string AutoSaveIntervalDisabled = "auto-save-interval.disabled";

		[FluentReference]
		const string AutoSaveIntervalMinuteOptions = "auto-save-interval.minute-options";

		[FluentReference]
		const string AutoSaveMaxFileNumber = "auto-save-max-file-number";
		readonly int[] autoSaveSeconds = [0, 10, 30, 45, 60, 120, 180, 300, 600];

		readonly int[] autoSaveFileNumbers = [3, 5, 10, 20, 50, 100];

		[ObjectCreator.UseCtor]
		public GameplaySettingsLogic(Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel, string panelID, string label)
		{
			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");
			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			// Setup dropdown for auto-save interval
			var autoSaveIntervalDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_INTERVAL_DROP_DOWN");

			autoSaveIntervalDropDown.OnClick = () =>
				ShowAutoSaveIntervalDropdown(autoSaveIntervalDropDown, autoSaveSeconds);

			autoSaveIntervalDropDown.GetText = () => GetMessageForAutoSaveInterval(Game.Settings.SinglePlayerSettings.AutoSaveInterval);

			// Setup dropdown for auto-save number.
			var autoSaveNoDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_FILE_NUMBER_DROP_DOWN");

			autoSaveNoDropDown.OnMouseDown = _ =>
				ShowAutoSaveFileNumberDropdown(autoSaveNoDropDown, autoSaveFileNumbers);

			autoSaveNoDropDown.GetText = () => FluentProvider.GetMessage(AutoSaveMaxFileNumber, "saves", Game.Settings.SinglePlayerSettings.AutoSaveMaxFileCount);

			autoSaveNoDropDown.IsDisabled = () => Game.Settings.SinglePlayerSettings.AutoSaveInterval <= 0;

			return () => false;
		}

		Action ResetPanel(Widget panel)
		{
			return () => { };
		}

		void ShowAutoSaveIntervalDropdown(DropDownButtonWidget dropdown, IEnumerable<int> options)
		{
			var gsp = Game.Settings.SinglePlayerSettings;

			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gsp.AutoSaveInterval == o,
					() =>
					{
						gsp.AutoSaveInterval = o;
						Game.Settings.Save();
					});

				var deviceLabel = item.Get<LabelWidget>("LABEL");
				deviceLabel.GetText = () => GetMessageForAutoSaveInterval(o);

				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, SetupItem);
		}

		void ShowAutoSaveFileNumberDropdown(DropDownButtonWidget dropdown, IEnumerable<int> options)
		{
			var gsp = Game.Settings.SinglePlayerSettings;

			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gsp.AutoSaveMaxFileCount == o,
					() =>
					{
						gsp.AutoSaveMaxFileCount = o;
						Game.Settings.Save();
					});

				var deviceLabel = item.Get<LabelWidget>("LABEL");

				deviceLabel.GetText = () => FluentProvider.GetMessage(AutoSaveMaxFileNumber, "saves", o);

				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, SetupItem);
		}

		static string GetMessageForAutoSaveInterval(int value) =>
			value switch
			{
				0 => FluentProvider.GetMessage(AutoSaveIntervalDisabled),
				< 60 => FluentProvider.GetMessage(AutoSaveIntervalOptions, "seconds", value),
				_ => FluentProvider.GetMessage(AutoSaveIntervalMinuteOptions, "minutes", value / 60)
			};
	}
}
