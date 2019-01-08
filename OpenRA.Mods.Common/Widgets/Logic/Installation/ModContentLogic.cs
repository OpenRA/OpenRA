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
using OpenRA.FileSystem;
using OpenRA.Widgets;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentLogic : ChromeLogic
	{
		readonly ModContent content;
		readonly ScrollPanelWidget scrollPanel;
		readonly Widget template;

		readonly Dictionary<string, ModContent.ModSource> sources = new Dictionary<string, ModContent.ModSource>();
		readonly Dictionary<string, ModContent.ModDownload> downloads = new Dictionary<string, ModContent.ModDownload>();

		bool discAvailable;

		[ObjectCreator.UseCtor]
		public ModContentLogic(Widget widget, ModData modData, Manifest mod, ModContent content, Action onCancel)
		{
			this.content = content;

			var panel = widget.Get("CONTENT_PANEL");

			var modObjectCreator = new ObjectCreator(mod, Game.Mods);
			var modPackageLoaders = modObjectCreator.GetLoaders<IPackageLoader>(mod.PackageFormats, "package");
			var modFileSystem = new FS(mod.Id, Game.Mods, modPackageLoaders);
			modFileSystem.LoadFromManifest(mod);

			var sourceYaml = MiniYaml.Load(modFileSystem, content.Sources, null);
			foreach (var s in sourceYaml)
				sources.Add(s.Key, new ModContent.ModSource(s.Value));

			var downloadYaml = MiniYaml.Load(modFileSystem, content.Downloads, null);
			foreach (var d in downloadYaml)
				downloads.Add(d.Key, new ModContent.ModDownload(d.Value));

			modFileSystem.UnmountAll();

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
				{ "sources", sources },
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
				var sourceTitles = p.Value.Sources.Select(s => sources[s].Title).Distinct();
				var sourceList = sourceTitles.JoinWith("\n");
				var isSourceAvailable = sourceTitles.Any();
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
