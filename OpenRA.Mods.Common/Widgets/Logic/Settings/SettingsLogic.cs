#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class SettingsLogic : ChromeLogic
	{
		readonly Dictionary<string, Func<bool>> leavePanelActions = new Dictionary<string, Func<bool>>();
		readonly Dictionary<string, Action> resetPanelActions = new Dictionary<string, Action>();

		readonly Widget panelContainer, tabContainer;
		readonly ButtonWidget tabTemplate;
		readonly int2 buttonStride;
		readonly List<ButtonWidget> buttons = new List<ButtonWidget>();
		readonly Dictionary<string, string> panels = new Dictionary<string, string>();
		string activePanel;

		bool needsRestart = false;

		static SettingsLogic() { }

		[ObjectCreator.UseCtor]
		public SettingsLogic(Widget widget, Action onExit, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
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
					var container = panelTemplate.Clone() as ContainerWidget;
					container.Id = panel.Key;
					panelContainer.AddChild(container);

					Game.LoadWidget(worldRenderer.World, panel.Key, container, new WidgetArgs()
					{
						{ "registerPanel", (Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>>)RegisterSettingsPanel },
						{ "panelID", panel.Key },
						{ "label", panel.Value }
					});
				}
			}

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				needsRestart |= leavePanelActions[activePanel]();
				var current = Game.Settings;
				current.Save();

				Action closeAndExit = () => { Ui.CloseWindow(); onExit(); };
				if (needsRestart)
				{
					Action noRestart = () => ConfirmationDialogs.ButtonPrompt(
						title: "Restart Required",
						text: "Some changes will not be applied until\nthe game is restarted.",
						onCancel: closeAndExit,
						cancelText: "Continue");

					if (!Game.ExternalMods.TryGetValue(ExternalMod.MakeKey(Game.ModData.Manifest), out var external))
					{
						noRestart();
						return;
					}

					ConfirmationDialogs.ButtonPrompt(
						title: "Restart Now?",
						text: "Some changes will not be applied until\nthe game is restarted. Restart now?",
						onConfirm: () => Game.SwitchToExternalMod(external, null, noRestart),
						onCancel: closeAndExit,
						confirmText: "Restart Now",
						cancelText: "Restart Later");
				}
				else
					closeAndExit();
			};

			widget.Get<ButtonWidget>("RESET_BUTTON").OnClick = () =>
			{
				Action reset = () =>
				{
					resetPanelActions[activePanel]();
					Game.Settings.Save();
				};

				ConfirmationDialogs.ButtonPrompt(
					title: $"Reset \"{panels[activePanel]}\"",
					text: "Are you sure you want to reset\nall settings in this panel?",
					onConfirm: reset,
					onCancel: () => { },
					confirmText: "Reset",
					cancelText: "Cancel");
			};
		}

		public void RegisterSettingsPanel(string panelID, string label, Func<Widget, Func<bool>> init, Func<Widget, Action> reset)
		{
			var panel = panelContainer.Get(panelID);

			if (activePanel == null)
				activePanel = panelID;

			panel.IsVisible = () => activePanel == panelID;

			leavePanelActions.Add(panelID, init(panel));
			resetPanelActions.Add(panelID, reset(panel));

			AddSettingsTab(panelID, label);
		}

		ButtonWidget AddSettingsTab(string id, string label)
		{
			var tab = tabTemplate.Clone() as ButtonWidget;
			var lastButton = buttons.LastOrDefault();
			if (lastButton != null)
			{
				tab.Bounds.X = lastButton.Bounds.X + buttonStride.X;
				tab.Bounds.Y = lastButton.Bounds.Y + buttonStride.Y;
			}

			tab.Id = id;
			tab.GetText = () => label;
			tab.IsHighlighted = () => activePanel == id;
			tab.OnClick = () =>
			{
				needsRestart |= leavePanelActions[activePanel]();
				Game.Settings.Save();
				activePanel = id;
			};

			tabContainer.AddChild(tab);
			buttons.Add(tab);

			return tab;
		}
	}
}
