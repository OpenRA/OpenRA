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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
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
		readonly AutoSaveSettings autoSaveSettings;
		readonly GameSettings gameSettings;
		readonly PlayerSettings playerSettings;
		readonly WorldRenderer worldRenderer;
		readonly ModData modData;

		TextFieldWidget nameTextfield;

		[ObjectCreator.UseCtor]
		public GameplaySettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.worldRenderer = worldRenderer;

			autoSaveSettings = modData.GetSettings<AutoSaveSettings>();
			gameSettings = modData.GetSettings<GameSettings>();
			playerSettings = modData.GetSettings<PlayerSettings>();
			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");
			var world = worldRenderer.World;

			var escPressed = false;
			nameTextfield = panel.Get<TextFieldWidget>("PLAYERNAME");
			nameTextfield.IsDisabled = () => world.Type != WorldType.Shellmap;
			nameTextfield.Text = Settings.SanitizedPlayerName(playerSettings.Name);
			nameTextfield.OnLoseFocus = () =>
			{
				if (escPressed)
				{
					escPressed = false;
					return;
				}

				nameTextfield.Text = nameTextfield.Text.Trim();
				if (nameTextfield.Text.Length == 0)
					nameTextfield.Text = Settings.SanitizedPlayerName(playerSettings.Name);
				else
				{
					nameTextfield.Text = Settings.SanitizedPlayerName(nameTextfield.Text);
					playerSettings.Name = nameTextfield.Text;
				}
			};

			nameTextfield.OnEnterKey = _ => { nameTextfield.YieldKeyboardFocus(); return true; };
			nameTextfield.OnEscKey = _ =>
			{
				nameTextfield.Text = Settings.SanitizedPlayerName(playerSettings.Name);
				escPressed = true;
				nameTextfield.YieldKeyboardFocus();
				return true;
			};

			var colorManager = modData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();

			var colorDropdown = panel.Get<DropDownButtonWidget>("PLAYERCOLOR");
			colorDropdown.IsDisabled = () => world.Type != WorldType.Shellmap;
			colorDropdown.OnMouseDown = _ => colorManager.ShowColorDropDown(colorDropdown, playerSettings.Color, null, worldRenderer, color =>
			{
				playerSettings.Color = color;
				playerSettings.Save();
			});
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerSettings.Color;

			SettingsUtils.BindCheckboxPref(panel, "HIDE_REPLAY_CHAT_CHECKBOX", gameSettings, "HideReplayChat");

			var autoSaveIntervalDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_INTERVAL_DROP_DOWN");
			autoSaveIntervalDropDown.OnClick = () =>
				ShowAutoSaveIntervalDropdown(autoSaveIntervalDropDown, autoSaveSeconds);
			autoSaveIntervalDropDown.GetText = () => GetMessageForAutoSaveInterval(autoSaveSettings.AutoSaveInterval);

			// Setup dropdown for auto-save number.
			var autoSaveNoDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_FILE_NUMBER_DROP_DOWN");

			autoSaveNoDropDown.OnMouseDown = _ => ShowAutoSaveFileNumberDropdown(autoSaveNoDropDown, autoSaveFileNumbers);
			autoSaveNoDropDown.GetText = () => FluentProvider.GetMessage(AutoSaveMaxFileNumber, "saves", autoSaveSettings.AutoSaveMaxFileCount);
			autoSaveNoDropDown.IsDisabled = () => autoSaveSettings.AutoSaveInterval <= 0;

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () =>
			{
				nameTextfield.YieldKeyboardFocus();
				return false;
			};
		}

		Action ResetPanel(Widget panel)
		{
			var defaultAutoSaveSettings = new AutoSaveSettings();
			var defaultGameSettings = new GameSettings();
			var defaultPlayerSettings = new PlayerSettings();
			return () =>
			{
				nameTextfield.Text = playerSettings.Name = defaultPlayerSettings.Name;
				playerSettings.Color = defaultPlayerSettings.Color;
				autoSaveSettings.AutoSaveInterval = defaultAutoSaveSettings.AutoSaveInterval;
				autoSaveSettings.AutoSaveMaxFileCount = defaultAutoSaveSettings.AutoSaveMaxFileCount;
				gameSettings.HideReplayChat = defaultGameSettings.HideReplayChat;
			};
		}

		void ShowAutoSaveIntervalDropdown(DropDownButtonWidget dropdown, IEnumerable<int> options)
		{
			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => autoSaveSettings.AutoSaveInterval == o,
					() =>
					{
						autoSaveSettings.AutoSaveInterval = o;
						autoSaveSettings.Save();
					});

				var deviceLabel = item.Get<LabelWidget>("LABEL");
				deviceLabel.GetText = () => GetMessageForAutoSaveInterval(o);

				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, SetupItem);
		}

		void ShowAutoSaveFileNumberDropdown(DropDownButtonWidget dropdown, IEnumerable<int> options)
		{
			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => autoSaveSettings.AutoSaveMaxFileCount == o,
					() =>
					{
						autoSaveSettings.AutoSaveMaxFileCount = o;
						autoSaveSettings.Save();
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
