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
	public class ModContentLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ModContentLogic(ModData modData)
		{
			var content = modData.Manifest.Get<ModContent>();
			if (!IsModInstalled(content))
			{
				var widgetArgs = new WidgetArgs
				{
					{ "continueLoading", () => Game.RunAfterTick(() => Game.InitializeMod(content.Mod, new Arguments())) },
					{ "content", content },
				};

				Ui.OpenWindow("CONTENT_PROMPT_PANEL", widgetArgs);
			}
			else
			{
				var widgetArgs = new WidgetArgs
				{
					{ "onCancel", () => Game.RunAfterTick(() => Game.InitializeMod(content.Mod, new Arguments())) },
					{ "content", content },
				};

				Ui.OpenWindow("CONTENT_PANEL", widgetArgs);
			}
		}

		static bool IsModInstalled(ModContent content)
		{
			return content.Packages
				.Where(p => p.Value.Required)
				.All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));
		}
	}

	public class ModContentInstallerLogic : ChromeLogic
	{
		[FluentReference]
		const string ManualInstall = "button-manual-install";

		readonly ModContent content;
		readonly ScrollPanelWidget scrollPanel;
		readonly Widget template;

		readonly Dictionary<string, ModContent.ModSource> sources = [];
		readonly Dictionary<string, ModContent.ModDownload> downloads = [];

		bool sourceAvailable;

		[ObjectCreator.UseCtor]
		public ModContentInstallerLogic(ModData modData, Widget widget, ModContent content, Action onCancel)
		{
			this.content = content;

			var panel = widget.Get("CONTENT_PANEL");

			var sourceYaml = MiniYaml.Load(modData.DefaultFileSystem, content.Sources, null);
			foreach (var s in sourceYaml)
				sources.Add(s.Key, new ModContent.ModSource(s.Value));

			var downloadYaml = MiniYaml.Load(modData.DefaultFileSystem, content.Downloads, null);
			foreach (var d in downloadYaml)
				downloads.Add(d.Key, new ModContent.ModDownload(d.Value));

			scrollPanel = panel.Get<ScrollPanelWidget>("PACKAGES");
			template = scrollPanel.Get<ContainerWidget>("PACKAGE_TEMPLATE");
			var headerLabel = panel.Get<LabelWidget>("HEADER_LABEL");
			headerLabel.IncreaseHeightToFitCurrentText();
			var headerHeight = headerLabel.Bounds.Height;

			panel.Bounds.Height += headerHeight;
			panel.Bounds.Y -= headerHeight / 2;
			scrollPanel.Bounds.Y += headerHeight;

			var sourceButton = panel.Get<ButtonWidget>("CHECK_SOURCE_BUTTON");
			sourceButton.Bounds.Y += headerHeight;
			sourceButton.IsVisible = () => sourceAvailable;

			sourceButton.OnClick = () => Ui.OpenWindow("SOURCE_INSTALL_PANEL", new WidgetArgs
			{
				{ "sources", sources },
				{ "content", content },
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
				var title = FluentProvider.GetMessage(p.Value.Title);
				titleWidget.GetText = () => title;

				var requiredWidget = container.Get<LabelWidget>("REQUIRED");
				requiredWidget.IsVisible = () => p.Value.Required;

				var sourceWidget = container.Get<ImageWidget>("SOURCE");
				var sourceList = p.Value.Sources.Select(s => sources[s].Title).Distinct().JoinWith("\n");
				var isSourceAvailable = sourceList.Length != 0;
				sourceWidget.GetTooltipText = () => sourceList;
				sourceWidget.IsVisible = () => isSourceAvailable;

				var installed = p.Value.IsInstalled();
				var downloadButton = container.Get<ButtonWidget>("DOWNLOAD");
				var downloadEnabled = !installed && p.Value.Download != null;
				downloadButton.IsVisible = () => downloadEnabled;

				if (downloadEnabled)
				{
					var widgetArgs = new WidgetArgs
					{
						{ "download", downloads[p.Value.Download] },
						{ "onSuccess", () => { } }
					};

					downloadButton.OnClick = () => Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", widgetArgs);
				}

				var installedWidget = container.Get<LabelWidget>("INSTALLED");
				installedWidget.IsVisible = () => installed;

				var requiresSourceWidget = container.Get<LabelWidget>("REQUIRES_SOURCE");
				requiresSourceWidget.IsVisible = () => !installed && !downloadEnabled;
				if (!isSourceAvailable)
				{
					var manualInstall = FluentProvider.GetMessage(ManualInstall);
					requiresSourceWidget.GetText = () => manualInstall;
				}

				scrollPanel.AddChild(container);
			}

			sourceAvailable = content.Packages.Values.Any(p => p.Sources.Length > 0 && !p.IsInstalled());
		}
	}
}
