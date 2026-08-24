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

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.FileSystem;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ContentSelectorLogic : ChromeLogic
	{
		protected ContentSource selected;

		protected readonly ContentSourcesModContent Content;
		protected readonly ContentSourcesFileSystemLoader.ContentSourceSettings SourceSettings;
		protected readonly FrozenDictionary<ContentSource, FrozenDictionary<string, ImmutableArray<string>>> SourceAttributes;
		readonly ScrollPanelWidget scrollPanel;

		[ObjectCreator.UseCtor]
		public ContentSelectorLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
		{
			Content = modData.GetOrCreate<ContentSourcesModContent>();
			SourceSettings = Game.Settings.GetOrCreate<ContentSourcesFileSystemLoader.ContentSourceSettings>(modData.ObjectCreator, Content.Mod);
			scrollPanel = widget.Get<ScrollPanelWidget>("SCROLL_PANEL");

			var availableSources = new List<string>();
			var attr = new Dictionary<ContentSource, FrozenDictionary<string, ImmutableArray<string>>>();
			foreach (var source in Content.ContentSources)
			{
				if (!source.Value.CanMount(modData.ModFiles, modData.ObjectCreator, out var attributes))
					continue;

				availableSources.Add(source.Key);
				attr.Add(source.Value, attributes);
			}

			SourceAttributes = attr.ToFrozenDictionary();

			var sourceDropdown = widget.Get<DropDownButtonWidget>("SOURCE_DROPDOWN");
			sourceDropdown.Disabled = availableSources.Count < 2;

			if (availableSources.Count == 0)
				return;

			var continueButton = widget.Get<ButtonWidget>("CONTINUE_BUTTON");
			if (Game.Mods.TryGetValue(Content.Mod, out var mod))
				continueButton.OnClick = () => Game.RunAfterTick(() => Game.InitializeMod(mod, new Arguments()));

			if (SourceSettings.ContentSource == null || !availableSources.Contains(SourceSettings.ContentSource))
				SourceSettings.ContentSource = availableSources[0];

			ScrollItemWidget SetupSource(string source, ScrollItemWidget template)
			{
				var item = ScrollItemWidget.Setup(template,
					() => SourceSettings.ContentSource == source,
					() => SelectSource(source));

				var label = FluentProvider.GetMessage(Content.ContentSources[source].Title);
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			var sourceLabel = new CachedTransform<string, string>(s => FluentProvider.GetMessage(Content.ContentSources[s].Title));
			sourceDropdown.GetText = () => sourceLabel.Update(SourceSettings.ContentSource);
			sourceDropdown.OnClick = () => sourceDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, availableSources, SetupSource);

			if (logicArgs.TryGetValue("Attributes", out var yaml))
				foreach (var node in yaml.Nodes)
					BindAttributeDropdown(widget, node.Key, node.Value.Value);

			if (logicArgs.TryGetValue("Sources", out yaml))
				foreach (var node in yaml.Nodes)
					widget.Get(node.Value.Value).IsVisible = () => selected == Content.ContentSources[node.Key];

			SelectSource(SourceSettings.ContentSource);

			var downloadButton = widget.Get<ButtonWidget>("DOWNLOAD_BUTTON");
			var quickInstallDownloaded = Content.QuickInstall.Path != null && Path.Exists(Platform.ResolvePath(Content.QuickInstall.Path));
			downloadButton.IsVisible = () => selected.RequiresQuickInstall && !quickInstallDownloaded;
			continueButton.IsVisible = () => !downloadButton.IsVisible();
			var widgetArgs = new WidgetArgs
			{
				{ "download", Content.QuickInstall },
				{ "onSuccess", continueButton.OnClick }
			};

			downloadButton.OnClick = () => Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", widgetArgs);

			var faqButton = widget.Get<ButtonWidget>("FAQ_BUTTON");
			faqButton.Visible = Content.FaqUrl != null;
			faqButton.OnClick = () => Game.Renderer.TryOpenUrl(Content.FaqUrl);

			var quitButton = widget.Get<ButtonWidget>("QUIT_BUTTON");
			quitButton.OnClick = Game.Exit;
		}

		void BindAttributeDropdown(Widget parent, string attribute, string widgetPrefix)
		{
			var container = parent.GetOrNull(widgetPrefix + "_CONTAINER");
			if (container == null)
				return;

			container.IsVisible = () => SourceAttributes[selected][attribute].Length > 0;
			ScrollItemWidget SetupOption(string option, ScrollItemWidget template)
			{
				var item = ScrollItemWidget.Setup(template,
					() => SourceSettings[attribute] == option,
					() =>
					{
						SourceSettings[attribute] = option;
						SourceSettings.Save();
					});

				var label = FluentProvider.GetMessage(Content.Attributes[attribute][option]);
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			var label = new CachedTransform<string, string>(v => FluentProvider.GetMessage(Content.Attributes[attribute][v]));
			var dropdown = container.GetOrNull<DropDownButtonWidget>(widgetPrefix + "_DROPDOWN");
			dropdown.GetText = () => label.Update(SourceSettings[attribute]);
			dropdown.OnClick = () => dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, SourceAttributes[selected][attribute], SetupOption);
			dropdown.IsDisabled = () => SourceAttributes[selected][attribute].Length < 2;
		}

		void SelectSource(string source)
		{
			selected = Content.ContentSources[source];
			SourceSettings.ContentSource = source;
			foreach (var kv in SourceAttributes[selected])
			{
				if (!Content.Attributes.ContainsKey(kv.Key))
					continue;

				if (!SourceSettings.TryGetValue(kv.Key, out var c) || !kv.Value.Contains(c))
					SourceSettings[kv.Key] = kv.Value.FirstOrDefault();
			}

			SourceSettings.Save();
			scrollPanel.Layout.AdjustChildren();
		}
	}

	public class ContentLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ContentLogic()
		{
			Ui.OpenWindow("CONTENT_PROMPT_PANEL");
		}
	}
}
