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
using System.IO;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentLogic : ChromeLogic
	{
		readonly ModContent content;
		readonly ScrollPanelWidget scrollPanel;
		readonly Widget template;
		bool discAvailable;

		[ObjectCreator.UseCtor]
		public ModContentLogic(Widget widget, string modId, Action onCancel)
		{
			var panel = widget.Get("CONTENT_PANEL");

			content = ModMetadata.AllMods[modId].ModContent;
			scrollPanel = panel.Get<ScrollPanelWidget>("PACKAGES");
			template = scrollPanel.Get<ContainerWidget>("PACKAGE_TEMPLATE");

			var headerTemplate = panel.Get<LabelWidget>("HEADER_TEMPLATE");
			var headerLines = !string.IsNullOrEmpty(content.HeaderMessage) ? content.HeaderMessage.Replace("\\n", "\n").Split('\n') : new string[0];
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
			scrollPanel.Bounds.Y += headerHeight;

			var discButton = panel.Get<ButtonWidget>("CHECK_DISC_BUTTON");
			discButton.Bounds.Y += headerHeight;
			discButton.IsVisible = () => discAvailable;

			discButton.OnClick = () => Ui.OpenWindow("DISC_INSTALL_PANEL", new WidgetArgs
			{
				{ "afterInstall", () => { } },
				{ "content", content }
			});

			var backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.Bounds.Y += headerHeight;
			backButton.OnClick = () => { Ui.CloseWindow(); onCancel(); };

			PopulateContentList();
			Game.RunAfterTick(Ui.ResetTooltips);
		}

		public override void BecameVisible()
		{
			PopulateContentList();
		}

		void PopulateContentList()
		{
			scrollPanel.RemoveChildren();

			foreach (var p in content.Packages)
			{
				var container = template.Clone();
				var titleWidget = container.Get<LabelWidget>("TITLE");
				var title = p.Value.Title;
				titleWidget.GetText = () => title;

				var requiredWidget = container.Get<LabelWidget>("REQUIRED");
				requiredWidget.IsVisible = () => p.Value.Required;

				var sourceWidget = container.Get<ImageWidget>("DISC");
				var sources = p.Value.Sources.Select(s => content.Sources[s].Title).Distinct();
				var sourceList = sources.JoinWith("\n");
				var isSourceAvailable = sources.Any();
				sourceWidget.GetTooltipText = () => sourceList;
				sourceWidget.IsVisible = () => isSourceAvailable;

				var installed = p.Value.IsInstalled();
				var downloadButton = container.Get<ButtonWidget>("DOWNLOAD");
				var downloadEnabled = !installed && p.Value.Download != null;
				downloadButton.IsVisible = () => downloadEnabled;

				if (downloadEnabled)
				{
					var download = content.Downloads[p.Value.Download];
					var widgetArgs = new WidgetArgs
					{
						{ "download", download },
						{ "onSuccess", () => { } }
					};

					downloadButton.OnClick = () => Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", widgetArgs);
				}

				var installedWidget = container.Get<LabelWidget>("INSTALLED");
				installedWidget.IsVisible = () => installed;

				var requiresDiscWidget = container.Get<LabelWidget>("REQUIRES_DISC");
				requiresDiscWidget.IsVisible = () => !installed && !downloadEnabled;
				if (!isSourceAvailable)
					requiresDiscWidget.GetText = () => "Manual Install";

				scrollPanel.AddChild(container);
			}

			discAvailable = content.Packages.Values.Any(p => p.Sources.Any() && !p.IsInstalled());
		}
	}
}
