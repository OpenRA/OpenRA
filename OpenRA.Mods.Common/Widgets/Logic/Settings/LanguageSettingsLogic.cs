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
	public class LanguageSettingsLogic : ChromeLogic
	{
		// Language names are in native script so users can find their language
		// regardless of what the UI currently displays.
		static readonly Dictionary<string, string> LanguageNativeNames = new()
		{
			{ "en", "English" },
		};

		readonly GameSettings gameSettings;
		static string originalLanguage;

		[ObjectCreator.UseCtor]
		public LanguageSettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label)
		{
			gameSettings = modData.GetSettings<GameSettings>();
			originalLanguage ??= gameSettings.Language;

			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			var languageDropdown = panel.Get<DropDownButtonWidget>("LANGUAGE_DROPDOWN");
			languageDropdown.OnMouseDown = _ => ShowLanguageDropdown(languageDropdown, gameSettings);
			languageDropdown.GetText = () =>
			{
				if (LanguageNativeNames.TryGetValue(gameSettings.Language, out var name))
					return name;
				return gameSettings.Language;
			};

			var restartDesc = panel.Get("LANGUAGE_RESTART_REQUIRED_DESC");
			restartDesc.IsVisible = () => gameSettings.Language != originalLanguage;

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () => gameSettings.Language != originalLanguage;
		}

		Action ResetPanel(Widget panel)
		{
			return () => gameSettings.Language = new GameSettings().Language;
		}

		static void ShowLanguageDropdown(DropDownButtonWidget dropdown, GameSettings gameSettings)
		{
			ScrollItemWidget SetupItem(string langCode, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gameSettings.Language == langCode,
					() => gameSettings.Language = langCode);

				var displayName = LanguageNativeNames.TryGetValue(langCode, out var name) ? name : langCode;
				item.Get<LabelWidget>("LABEL").GetText = () => displayName;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, Game.ModData.Languages, SetupItem);
		}
	}
}
