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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public interface ISettingsLogic
	{
		void RegisterSettingsPanel(string panelID, string label, Func<Widget, Func<bool>> init, Func<Widget, Action> reset);
	}

	[IncludeChromeLogicArgsFluentReferences(nameof(DynamicFluentReferences))]
	public class SettingsLogic : ChromeLogic, ISettingsLogic
	{
		[FluentReference]
		const string SettingsSaveTitle = "dialog-settings-save.title";

		[FluentReference]
		const string SettingsSavePrompt = "dialog-settings-save.prompt";

		[FluentReference]
		const string SettingsSaveCancel = "dialog-settings-save.cancel";

		[FluentReference]
		const string RestartTitle = "dialog-settings-restart.title";

		[FluentReference]
		const string RestartPrompt = "dialog-settings-restart.prompt";

		[FluentReference]
		const string RestartAccept = "dialog-settings-restart.confirm";

		[FluentReference]
		const string RestartCancel = "dialog-settings-restart.cancel";

		[FluentReference("panel")]
		const string ResetTitle = "dialog-settings-reset.title";

		[FluentReference]
		const string ResetPrompt = "dialog-settings-reset.prompt";

		[FluentReference]
		const string ResetAccept = "dialog-settings-reset.confirm";

		[FluentReference]
		const string ResetCancel = "dialog-settings-reset.cancel";

		public static IEnumerable<(string Key, FluentReferenceAttribute Reference)> DynamicFluentReferences(Dictionary<string, MiniYaml> logicArgs)
		{
			if (logicArgs.TryGetValue("Panels", out var settingsPanels))
				foreach (var node in settingsPanels.Nodes)
					yield return (node.Value.Value, new FluentReferenceAttribute());
		}

		readonly Dictionary<string, Func<bool>> leavePanelActions = [];
		readonly Dictionary<string, Action> resetPanelActions = [];

		readonly Widget panelContainer, tabContainer;
		readonly ButtonWidget tabTemplate;
		readonly int2 buttonStride;
		readonly List<ButtonWidget> buttons = [];
		readonly Dictionary<string, string> panels = [];
		string activePanel;

		bool needsRestart = false;

		[ObjectCreator.UseCtor]
		public SettingsLogic(Widget widget, Action onExit, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs, ModData modData)
		{
			panelContainer = widget.Get("PANEL_CONTAINER");
			var panelTemplate = panelContainer.Get<ContainerWidget>("PANEL_TEMPLATE");
			panelContainer.RemoveChild(panelTemplate);

			tabContainer = widget.Get("SETTINGS_TAB_CONTAINER");
			tabTemplate = tabContainer.Get<ButtonWidget>("BUTTON_TEMPLATE");
			tabContainer.RemoveChild(tabTemplate);

			if (logicArgs.TryGetValue("ButtonStride", out var buttonStrideNode))
				buttonStride = FieldLoader.GetValue<int2>("ButtonStride", buttonStrideNode.Value);

			if (logicArgs.TryGetValue("Panels", out var settingsPanels))
			{
				panels = settingsPanels.ToDictionary(kv => kv.Value);

				foreach (var panel in panels)
				{
					var container = panelTemplate.Clone();
					container.Id = panel.Key;
					panelContainer.AddChild(container);

					Game.LoadWidget(worldRenderer.World, panel.Key, container, new WidgetArgs()
					{
						{ "settingsLogic", this },
						{ "panelID", panel.Key },
						{ "label", FluentProvider.GetMessage(panel.Value) }
					});
				}
			}

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				needsRestart |= leavePanelActions[activePanel]();
				Game.Settings.Save();

				void CloseAndExit() { Ui.CloseWindow(); onExit(); }
				if (needsRestart)
				{
					void NoRestart() => ConfirmationDialogs.ButtonPrompt(modData,
						title: SettingsSaveTitle,
						text: SettingsSavePrompt,
						onCancel: CloseAndExit,
						cancelText: SettingsSaveCancel);

					if (!Game.ExternalMods.TryGetValue(ExternalMod.MakeKey(Game.ModData.Manifest), out var external))
					{
						NoRestart();
						return;
					}

					ConfirmationDialogs.ButtonPrompt(modData,
						title: RestartTitle,
						text: RestartPrompt,
						onConfirm: () => Game.SwitchToExternalMod(external, null, NoRestart),
						confirmText: RestartAccept,
						onCancel: CloseAndExit,
						cancelText: RestartCancel);
				}
				else
					CloseAndExit();
			};

			widget.Get<ButtonWidget>("RESET_BUTTON").OnClick = () =>
			{
				void Reset()
				{
					resetPanelActions[activePanel]();
					Game.Settings.Save();
				}

				ConfirmationDialogs.ButtonPrompt(modData,
					title: ResetTitle,
					text: ResetPrompt,
					titleArguments: ["panel", panels[activePanel]],
					onConfirm: Reset,
					confirmText: ResetAccept,
					onCancel: () => { },
					cancelText: ResetCancel);
			};
		}

		public void RegisterSettingsPanel(string panelID, string label, Func<Widget, Func<bool>> init, Func<Widget, Action> reset)
		{
			var panel = panelContainer.Get(panelID);

			activePanel ??= panelID;

			panel.IsVisible = () => activePanel == panelID;

			leavePanelActions.Add(panelID, init(panel));
			resetPanelActions.Add(panelID, reset(panel));

			var tab = tabTemplate.Clone();
			var lastButton = buttons.LastOrDefault();
			if (lastButton != null)
			{
				tab.Bounds.X = lastButton.Bounds.X + buttonStride.X;
				tab.Bounds.Y = lastButton.Bounds.Y + buttonStride.Y;
			}

			tab.Id = panelID;
			tab.GetText = () => label;
			tab.IsHighlighted = () => activePanel == panelID;
			tab.OnClick = () =>
			{
				needsRestart |= leavePanelActions[activePanel]();
				Game.Settings.Save();
				activePanel = panelID;
			};

			tabContainer.AddChild(tab);
			buttons.Add(tab);
		}
	}
}
