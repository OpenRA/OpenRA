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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class CreditsLogic : ChromeLogic
	{
		readonly ModData modData;
		readonly ScrollPanelWidget scrollPanel;
		readonly LabelWidget template;

		readonly IEnumerable<string> modLines;
		readonly IEnumerable<string> engineLines;
		bool showMod = false;

		[ObjectCreator.UseCtor]
		public CreditsLogic(Widget widget, ModData modData, Action onExit)
		{
			this.modData = modData;

			var panel = widget.Get("CREDITS_PANEL");

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			engineLines = ParseLines("AUTHORS");

			var tabContainer = panel.Get("TAB_CONTAINER");
			var modTab = tabContainer.Get<ButtonWidget>("MOD_TAB");
			modTab.IsHighlighted = () => showMod;
			modTab.OnClick = () => ShowCredits(true);

			var engineTab = tabContainer.Get<ButtonWidget>("ENGINE_TAB");
			engineTab.IsHighlighted = () => !showMod;
			engineTab.OnClick = () => ShowCredits(false);

			scrollPanel = panel.Get<ScrollPanelWidget>("CREDITS_DISPLAY");
			template = scrollPanel.Get<LabelWidget>("CREDITS_TEMPLATE");

			var hasModCredits = modData.Manifest.Contains<ModCredits>();
			if (hasModCredits)
			{
				var modCredits = modData.Manifest.Get<ModCredits>();
				modLines = ParseLines(modCredits.ModCreditsFile);
				modTab.GetText = () => modCredits.ModTabTitle;

				// Make space to show the tabs
				tabContainer.IsVisible = () => true;
				scrollPanel.Bounds.Y += tabContainer.Bounds.Height;
				scrollPanel.Bounds.Height -= tabContainer.Bounds.Height;
			}

			ShowCredits(hasModCredits);
		}

		void ShowCredits(bool modCredits)
		{
			showMod = modCredits;

			scrollPanel.RemoveChildren();
			foreach (var line in showMod ? modLines : engineLines)
			{
				var label = template.Clone() as LabelWidget;
				label.GetText = () => line;
				scrollPanel.AddChild(label);
			}
		}

		IEnumerable<string> ParseLines(string file)
		{
			return modData.DefaultFileSystem.Open(file)
				.ReadAllLines()
				.Select(l => l.Replace("\t", "    ").Replace("*", "\u2022"))
				.ToList();
		}
	}
}
