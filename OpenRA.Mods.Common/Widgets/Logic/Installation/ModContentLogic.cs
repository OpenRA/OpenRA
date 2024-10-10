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
using OpenRA.FileSystem;
using OpenRA.Widgets;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentLogic : ChromeLogic
	{
		[FluentReference]
		const string ManualInstall = "button-manual-install";

		readonly ModContent content;
		readonly ScrollPanelWidget scrollPanel;
		readonly Widget template;

		readonly Dictionary<string, ModContent.ModSource> sources = new();
		readonly Dictionary<string, ModContent.ModDownload> downloads = new();

		readonly FluentBundle externalFluentBundle;

		bool sourceAvailable;

		[ObjectCreator.UseCtor]
		public ModContentLogic(Widget widget, Manifest mod, ModContent content, Action onCancel, string translationFilePath)
		{
			this.content = content;

			var panel = widget.Get("CONTENT_PANEL");

			var modObjectCreator = new ObjectCreator(mod, Game.Mods);
			var modPackageLoaders = modObjectCreator.GetLoaders<IPackageLoader>(mod.PackageFormats, "package");
			var modFileSystem = new FS(mod.Id, Game.Mods, modPackageLoaders);

			var modFileSystemLoader = modObjectCreator.GetLoader<IFileSystemLoader>(mod.FileSystem.Value, "filesystem");
			FieldLoader.Load(modFileSystemLoader, mod.FileSystem);
			modFileSystemLoader.Mount(modFileSystem, modObjectCreator);
			modFileSystem.TrimExcess();

			var sourceYaml = MiniYaml.Load(modFileSystem, content.Sources, null);
			foreach (var s in sourceYaml)
				sources.Add(s.Key, new ModContent.ModSource(s.Value, modObjectCreator));

			var downloadYaml = MiniYaml.Load(modFileSystem, content.Downloads, null);
			foreach (var d in downloadYaml)
				downloads.Add(d.Key, new ModContent.ModDownload(d.Value, modObjectCreator));

			modFileSystem.UnmountAll();

			externalFluentBundle = new FluentBundle(Game.Settings.Player.Language, File.ReadAllText(translationFilePath), _ => { });

			scrollPanel = panel.Get<ScrollPanelWidget>("PACKAGES");
			template = scrollPanel.Get<ContainerWidget>("PACKAGE_TEMPLATE");

			var headerTemplate = panel.Get<LabelWidget>("HEADER_TEMPLATE");
			var headerLines =
				!string.IsNullOrEmpty(content.HeaderMessage)
					? externalFluentBundle.GetString(content.HeaderMessage)
					: null;
			var headerHeight = 0;
			if (headerLines != null)
			{
				var label = (LabelWidget)headerTemplate.Clone();
				label.GetText = () => headerLines;
				label.IncreaseHeightToFitCurrentText();
				panel.AddChild(label);

				headerHeight += label.Bounds.Height;
			}

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
				{ "externalFluentBundle", externalFluentBundle },
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
				var title = externalFluentBundle.GetString(p.Value.Title);
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
					var manualInstall = FluentProvider.GetString(ManualInstall);
					requiresSourceWidget.GetText = () => manualInstall;
				}

				scrollPanel.AddChild(container);
			}

			sourceAvailable = content.Packages.Values.Any(p => p.Sources.Length > 0 && !p.IsInstalled());
		}
	}
}
