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
using System.IO;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class CreditsLogic : ChromeLogic
	{
		readonly ScrollPanelWidget scrollPanel;
		readonly LabelWidget template;

		readonly bool showModTab;
		readonly bool showEngineTab;
		bool isShowingModTab;
		readonly IEnumerable<string> modLines;
		readonly IEnumerable<string> engineLines;

		[ObjectCreator.UseCtor]
		public CreditsLogic(Widget widget, ModData modData, Action onExit)
		{
			var panel = widget.Get("CREDITS_PANEL");

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			var modCredits = modData.Manifest.Get<ModCredits>();
			var tabContainer = panel.Get("TAB_CONTAINER");

			if (modCredits.ModCreditsFile != null)
			{
				showModTab = true;
				modLines = ParseLines(modData.DefaultFileSystem.Open(modCredits.ModCreditsFile));

				var modTab = tabContainer.Get<ButtonWidget>("MOD_TAB");
				modTab.IsHighlighted = () => isShowingModTab;
				modTab.OnClick = () => ShowCredits(true);
				modTab.GetText = () => modCredits.ModTabTitle;
			}

			if (modCredits.EngineCreditsFile != null)
			{
				showEngineTab = true;
				engineLines = ParseLines(File.OpenRead(Platform.ResolvePath(modCredits.EngineCreditsFile)));

				var engineTab = tabContainer.Get<ButtonWidget>("ENGINE_TAB");
				engineTab.IsHighlighted = () => !isShowingModTab;
				engineTab.OnClick = () => ShowCredits(false);
			}

			scrollPanel = panel.Get<ScrollPanelWidget>("CREDITS_DISPLAY");
			template = scrollPanel.Get<LabelWidget>("CREDITS_TEMPLATE");

			// Make space to show the tabs
			tabContainer.IsVisible = () => showModTab && showEngineTab;
			if (showModTab && showEngineTab)
			{
				scrollPanel.Bounds.Y += tabContainer.Bounds.Height;
				scrollPanel.Bounds.Height -= tabContainer.Bounds.Height;
			}

			ShowCredits(showModTab);
		}

		void ShowCredits(bool modCredits)
		{
			isShowingModTab = modCredits;

			scrollPanel.RemoveChildren();
			foreach (var line in modCredits ? modLines : engineLines)
			{
				var label = template.Clone() as LabelWidget;
				label.GetText = () => line;
				scrollPanel.AddChild(label);
			}
		}

		static IEnumerable<string> ParseLines(Stream file)
		{
			return file.ReadAllLines().Select(l => l.Replace("\t", "    ").Replace("*", "\u2022")).ToList();
		}
	}
}
