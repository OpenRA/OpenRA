#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentPromptLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ModContentPromptLogic(Widget widget, string modId, Action continueLoading)
		{
			var panel = widget.Get("CONTENT_PROMPT_PANEL");

			var mod = ModMetadata.AllMods[modId];
			var content = ModMetadata.AllMods[modId].ModContent;

			var headerTemplate = panel.Get<LabelWidget>("HEADER_TEMPLATE");
			var headerLines = !string.IsNullOrEmpty(content.InstallPromptMessage) ? content.InstallPromptMessage.Replace("\\n", "\n").Split('\n') : new string[0];
			var headerHeight = 0;
			foreach (var l in headerLines)
			{
				var line = (LabelWidget)headerTemplate.Clone();
				line.GetText = () => l;
				line.Bounds.Y += headerHeight;
				panel.AddChild(line);

				headerHeight += headerTemplate.Bounds.Height;
			}

			panel.Bounds.Height += headerHeight;
			panel.Bounds.Y -= headerHeight / 2;

			var advancedButton = panel.Get<ButtonWidget>("ADVANCED_BUTTON");
			advancedButton.Bounds.Y += headerHeight;
			advancedButton.OnClick = () =>
			{
				Ui.OpenWindow("CONTENT_PANEL", new WidgetArgs
				{
					{ "modId", modId },
					{ "onCancel", Ui.CloseWindow }
				});
			};

			var quickButton = panel.Get<ButtonWidget>("QUICK_BUTTON");
			quickButton.IsVisible = () => !string.IsNullOrEmpty(content.QuickDownload);
			quickButton.Bounds.Y += headerHeight;
			quickButton.OnClick = () =>
			{
				Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", new WidgetArgs
				{
					{ "download", content.Downloads[content.QuickDownload] },
					{ "onSuccess", continueLoading }
				});
			};

			var backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.Bounds.Y += headerHeight;
			backButton.OnClick = Ui.CloseWindow;
			Game.RunAfterTick(Ui.ResetTooltips);
		}
	}
}
