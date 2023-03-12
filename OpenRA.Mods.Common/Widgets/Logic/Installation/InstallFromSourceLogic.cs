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
using System.Threading.Tasks;
using OpenRA.Mods.Common.Installer;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallFromSourceLogic : ChromeLogic
	{
		[TranslationReference]
		const string DetectingSources = "label-detecting-sources";

		[TranslationReference]
		const string CheckingSources = "label-checking-sources";

		[TranslationReference("title")]
		const string SearchingSourceFor = "label-searching-source-for";

		[TranslationReference]
		const string ContentPackageInstallation = "label-content-package-installation";

		[TranslationReference]
		const string GameSources = "label-game-sources";

		[TranslationReference]
		const string DigitalInstalls = "label-digital-installs";

		[TranslationReference]
		const string GameContentNotFound = "label-game-content-not-found";

		[TranslationReference]
		const string AlternativeContentSources = "label-alternative-content-sources";

		[TranslationReference]
		const string InstallingContent = "label-installing-content";

		[TranslationReference("filename")]
		public const string CopyingFilename = "label-copying-filename";

		[TranslationReference("filename", "progress")]
		public const string CopyingFilenameProgress = "label-copying-filename-progress";

		[TranslationReference]
		const string InstallationFailed = "label-installation-failed";

		[TranslationReference]
		const string CheckInstallLog = "label-check-install-log";

		[TranslationReference("filename")]
		public const string Extracing = "label-extracting-filename";

		[TranslationReference("filename", "progress")]
		public const string ExtractingProgress = "label-extracting-filename-progress";

		[TranslationReference]
		public const string Continue = "button-continue";

		[TranslationReference]
		const string Cancel = "button-cancel";

		[TranslationReference]
		const string Retry = "button-retry";

		[TranslationReference]
		const string Back = "button-back";

		// Hide percentage indicators for files smaller than 25 MB
		public const int ShowPercentageThreshold = 26214400;

		enum Mode { Progress, Message, List }

		readonly ModData modData;
		readonly ModContent content;
		readonly Dictionary<string, ModContent.ModSource> sources;

		readonly Widget panel;
		readonly LabelWidget titleLabel;
		readonly ButtonWidget primaryButton;
		readonly ButtonWidget secondaryButton;

		// Progress panel
		readonly Widget progressContainer;
		readonly ProgressBarWidget progressBar;
		readonly LabelWidget progressLabel;

		// Message panel
		readonly Widget messageContainer;
		readonly LabelWidget messageLabel;

		// List Panel
		readonly Widget listContainer;
		readonly ScrollPanelWidget listPanel;
		readonly Widget listHeaderTemplate;
		readonly LabelWidget labelListTemplate;
		readonly ContainerWidget checkboxListTemplate;
		readonly LabelWidget listLabel;

		ModContent.ModPackage[] availablePackages;
		IDictionary<string, bool> selectedPackages;

		Mode visible = Mode.Progress;

		[ObjectCreator.UseCtor]
		public InstallFromSourceLogic(Widget widget, ModData modData, ModContent content, Dictionary<string, ModContent.ModSource> sources)
		{
			this.modData = modData;
			this.content = content;
			this.sources = sources;

			Log.AddChannel("install", "install.log");

			panel = widget.Get("SOURCE_INSTALL_PANEL");

			titleLabel = panel.Get<LabelWidget>("TITLE");

			primaryButton = panel.Get<ButtonWidget>("PRIMARY_BUTTON");
			secondaryButton = panel.Get<ButtonWidget>("SECONDARY_BUTTON");

			// Progress view
			progressContainer = panel.Get("PROGRESS");
			progressContainer.IsVisible = () => visible == Mode.Progress;
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			progressLabel = panel.Get<LabelWidget>("PROGRESS_MESSAGE");
			progressLabel.IsVisible = () => visible == Mode.Progress;

			// Message view
			messageContainer = panel.Get("MESSAGE");
			messageContainer.IsVisible = () => visible == Mode.Message;
			messageLabel = messageContainer.Get<LabelWidget>("MESSAGE_MESSAGE");

			// List view
			listContainer = panel.Get("LIST");
			listContainer.IsVisible = () => visible == Mode.List;

			listPanel = listContainer.Get<ScrollPanelWidget>("LIST_PANEL");
			listHeaderTemplate = listPanel.Get("LIST_HEADER_TEMPLATE");
			labelListTemplate = listPanel.Get<LabelWidget>("LABEL_LIST_TEMPLATE");
			checkboxListTemplate = listPanel.Get<ContainerWidget>("CHECKBOX_LIST_TEMPLATE");
			listPanel.RemoveChildren();

			listLabel = listContainer.Get<LabelWidget>("LIST_MESSAGE");

			DetectContentSources();
		}

		void DetectContentSources()
		{
			var message = TranslationProvider.GetString(DetectingSources);
			ShowProgressbar(TranslationProvider.GetString(CheckingSources), () => message);
			ShowBackRetry(DetectContentSources);

			new Task(() =>
			{
				foreach (var kv in sources)
				{
					message = TranslationProvider.GetString(SearchingSourceFor, Translation.Arguments("title", kv.Value.Title));

					var sourceResolver = kv.Value.ObjectCreator.CreateObject<ISourceResolver>($"{kv.Value.Type.Value}SourceResolver");

					var path = sourceResolver.FindSourcePath(kv.Value);
					if (path != null)
					{
						Log.Write("install", $"Using installer `{kv.Key}: {kv.Value.Title}` of type `{kv.Value.Type.Value}`:");

						availablePackages = content.Packages.Values
							.Where(p => p.Sources.Contains(kv.Key) && !p.IsInstalled())
							.ToArray();

						selectedPackages = availablePackages.ToDictionary(x => x.Identifier, y => y.Required);

						// Ignore source if content is already installed
						if (availablePackages.Any())
						{
							Game.RunAfterTick(() =>
							{
								ShowList(kv.Value, TranslationProvider.GetString(ContentPackageInstallation));
								ShowContinueCancel(() => InstallFromSource(path, kv.Value));
							});

							return;
						}
					}
				}

				var missingSources = content.Packages.Values
					.Where(p => !p.IsInstalled())
					.SelectMany(p => p.Sources)
					.Select(d => sources[d]);

				var gameSources = new HashSet<string>();
				var digitalInstalls = new HashSet<string>();

				foreach (var source in missingSources)
				{
					var sourceResolver = source.ObjectCreator.CreateObject<ISourceResolver>($"{source.Type.Value}SourceResolver");

					var availability = sourceResolver.GetAvailability();

					if (availability == Availability.GameSource)
						gameSources.Add(source.Title);
					else if (availability == Availability.DigitalInstall)
						digitalInstalls.Add(source.Title);
				}

				var options = new Dictionary<string, IEnumerable<string>>();

				if (gameSources.Any())
					options.Add(TranslationProvider.GetString(GameSources), gameSources);

				if (digitalInstalls.Any())
					options.Add(TranslationProvider.GetString(DigitalInstalls), digitalInstalls);

				Game.RunAfterTick(() =>
				{
					ShowList(TranslationProvider.GetString(GameContentNotFound), TranslationProvider.GetString(AlternativeContentSources), options);
					ShowBackRetry(DetectContentSources);
				});
			}).Start();
		}

		void InstallFromSource(string path, ModContent.ModSource modSource)
		{
			var message = "";
			ShowProgressbar(TranslationProvider.GetString(InstallingContent), () => message);
			ShowDisabledCancel();

			new Task(() =>
			{
				var extracted = new List<string>();

				try
				{
					void RunSourceActions(MiniYamlNode contentPackageYaml)
					{
						var sourceActionListYaml = contentPackageYaml.Value.Nodes.FirstOrDefault(x => x.Key == "Actions");
						if (sourceActionListYaml == null)
							return;

						foreach (var sourceActionNode in sourceActionListYaml.Value.Nodes)
						{
							var sourceAction = modSource.ObjectCreator.CreateObject<ISourceAction>($"{sourceActionNode.Key}SourceAction");
							sourceAction.RunActionOnSource(sourceActionNode.Value, path, modData, extracted, m => message = m);
						}
					}

					var beforeInstall = modSource.Install.FirstOrDefault(x => x.Key == "BeforeInstall");
					if (beforeInstall != null)
						RunSourceActions(beforeInstall);

					foreach (var packageInstallationNode in modSource.Install.Where(x => x.Key == "ContentPackage"))
					{
						var packageName = packageInstallationNode.Value.Nodes.SingleOrDefault(x => x.Key == "Name")?.Value.Value;
						if (!string.IsNullOrEmpty(packageName) && selectedPackages.TryGetValue(packageName, out var required) && required)
							RunSourceActions(packageInstallationNode);
					}

					var afterInstall = modSource.Install.FirstOrDefault(x => x.Key == "AfterInstall");
					if (afterInstall != null)
						RunSourceActions(afterInstall);

					Game.RunAfterTick(Ui.CloseWindow);
				}
				catch (Exception e)
				{
					Log.Write("install", e.ToString());

					foreach (var f in extracted)
					{
						Log.Write("install", "Deleting " + f);
						File.Delete(f);
					}

					Game.RunAfterTick(() =>
					{
						ShowMessage(TranslationProvider.GetString(InstallationFailed), TranslationProvider.GetString(CheckInstallLog));
						ShowBackRetry(() => InstallFromSource(path, modSource));
					});
				}
			}).Start();
		}

		void ShowMessage(string title, string message)
		{
			visible = Mode.Message;
			titleLabel.Text = title;
			messageLabel.Text = message;

			primaryButton.Bounds.Y += messageContainer.Bounds.Height - panel.Bounds.Height;
			secondaryButton.Bounds.Y += messageContainer.Bounds.Height - panel.Bounds.Height;
			panel.Bounds.Y -= (messageContainer.Bounds.Height - panel.Bounds.Height) / 2;
			panel.Bounds.Height = messageContainer.Bounds.Height;
		}

		void ShowProgressbar(string title, Func<string> getMessage)
		{
			visible = Mode.Progress;
			titleLabel.Text = title;
			progressBar.IsIndeterminate = () => true;

			var font = Game.Renderer.Fonts[progressLabel.Font];
			var status = new CachedTransform<string, string>(s => WidgetUtils.TruncateText(s, progressLabel.Bounds.Width, font));
			progressLabel.GetText = () => status.Update(getMessage());

			primaryButton.Bounds.Y += progressContainer.Bounds.Height - panel.Bounds.Height;
			secondaryButton.Bounds.Y += progressContainer.Bounds.Height - panel.Bounds.Height;
			panel.Bounds.Y -= (progressContainer.Bounds.Height - panel.Bounds.Height) / 2;
			panel.Bounds.Height = progressContainer.Bounds.Height;
		}

		void ShowList(ModContent.ModSource source, string message)
		{
			visible = Mode.List;
			titleLabel.Text = source.Title;
			listLabel.Text = message;

			listPanel.RemoveChildren();
			foreach (var package in availablePackages)
			{
				var containerWidget = (ContainerWidget)checkboxListTemplate.Clone();
				var checkboxWidget = containerWidget.Get<CheckboxWidget>("PACKAGE_CHECKBOX");
				checkboxWidget.GetText = () => package.Title;
				checkboxWidget.IsDisabled = () => package.Required;
				checkboxWidget.IsChecked = () => selectedPackages[package.Identifier];
				checkboxWidget.OnClick = () => selectedPackages[package.Identifier] = !selectedPackages[package.Identifier];

				var contentPackageNode = source.Install.FirstOrDefault(x =>
					x.Value.Nodes.FirstOrDefault(y => y.Key == "Name")?.Value.Value == package.Identifier);

				var tooltipText = contentPackageNode?.Value.Nodes.FirstOrDefault(x => x.Key == nameof(ModContent.ModSource.TooltipText))?.Value.Value;
				var tooltipIcon = containerWidget.Get<ImageWidget>("PACKAGE_INFO");
				tooltipIcon.IsVisible = () => !string.IsNullOrWhiteSpace(tooltipText);
				tooltipIcon.GetTooltipText = () => tooltipText;

				listPanel.AddChild(containerWidget);
			}

			primaryButton.Bounds.Y += listContainer.Bounds.Height - panel.Bounds.Height;
			secondaryButton.Bounds.Y += listContainer.Bounds.Height - panel.Bounds.Height;
			panel.Bounds.Y -= (listContainer.Bounds.Height - panel.Bounds.Height) / 2;
			panel.Bounds.Height = listContainer.Bounds.Height;
		}

		void ShowList(string title, string message, Dictionary<string, IEnumerable<string>> groups)
		{
			visible = Mode.List;
			titleLabel.Text = title;
			listLabel.Text = message;

			listPanel.RemoveChildren();

			foreach (var kv in groups)
			{
				if (kv.Value.Any())
				{
					var groupTitle = kv.Key;
					var headerWidget = listHeaderTemplate.Clone();
					var headerTitleWidget = headerWidget.Get<LabelWidget>("LABEL");
					headerTitleWidget.GetText = () => groupTitle;
					listPanel.AddChild(headerWidget);
				}

				foreach (var i in kv.Value)
				{
					var item = i;
					var labelWidget = (LabelWidget)labelListTemplate.Clone();
					labelWidget.GetText = () => item;
					listPanel.AddChild(labelWidget);
				}
			}

			primaryButton.Bounds.Y += listContainer.Bounds.Height - panel.Bounds.Height;
			secondaryButton.Bounds.Y += listContainer.Bounds.Height - panel.Bounds.Height;
			panel.Bounds.Y -= (listContainer.Bounds.Height - panel.Bounds.Height) / 2;
			panel.Bounds.Height = listContainer.Bounds.Height;
		}

		void ShowContinueCancel(Action continueAction)
		{
			primaryButton.OnClick = continueAction;
			primaryButton.Text = TranslationProvider.GetString(Continue);
			primaryButton.Visible = true;

			secondaryButton.OnClick = Ui.CloseWindow;
			secondaryButton.Text = TranslationProvider.GetString(Cancel);
			secondaryButton.Visible = true;
			secondaryButton.Disabled = false;
			Game.RunAfterTick(Ui.ResetTooltips);
		}

		void ShowBackRetry(Action retryAction)
		{
			primaryButton.OnClick = retryAction;
			primaryButton.Text = TranslationProvider.GetString(Retry);
			primaryButton.Visible = true;

			secondaryButton.OnClick = Ui.CloseWindow;
			secondaryButton.Text = TranslationProvider.GetString(Back);
			secondaryButton.Visible = true;
			secondaryButton.Disabled = false;
			Game.RunAfterTick(Ui.ResetTooltips);
		}

		void ShowDisabledCancel()
		{
			primaryButton.Visible = false;
			secondaryButton.Disabled = true;
			Game.RunAfterTick(Ui.ResetTooltips);
		}
	}
}
