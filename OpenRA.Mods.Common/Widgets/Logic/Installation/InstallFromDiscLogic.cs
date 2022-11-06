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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenRA.Mods.Common.Installer;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallFromDiscLogic : ChromeLogic
	{
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
		readonly LabelWidget listTemplate;
		readonly LabelWidget listLabel;

		Mode visible = Mode.Progress;

		[TranslationReference]
		static readonly string DetectingDrives = "detecting-drives";

		[TranslationReference]
		static readonly string CheckingDiscs = "checking-discs";

		[TranslationReference("title")]
		static readonly string SearchingDiscFor = "searching-disc-for";

		[TranslationReference]
		static readonly string ContentPackageInstallation = "content-package-installation";

		[TranslationReference]
		static readonly string GameDiscs = "game-discs";

		[TranslationReference]
		static readonly string DigitalInstalls = "digital-installs";

		[TranslationReference]
		static readonly string GameContentNotFound = "game-content-not-found";

		[TranslationReference]
		static readonly string AlternativeContentSources = "alternative-content-sources";

		[TranslationReference]
		static readonly string InstallingContent = "installing-content";

		[TranslationReference("filename")]
		public static readonly string CopyingFilename = "copying-filename";

		[TranslationReference("filename", "progress")]
		public static readonly string CopyingFilenameProgress = "copying-filename-progress";

		[TranslationReference]
		static readonly string InstallationFailed = "installation-failed";

		[TranslationReference]
		static readonly string CheckInstallLog = "check-install-log";

		[TranslationReference("filename")]
		public static readonly string Extracing = "extracting-filename";

		[TranslationReference("filename", "progress")]
		public static readonly string ExtracingProgress = "extracting-filename-progress";

		[TranslationReference]
		static readonly string Continue = "continue";

		[TranslationReference]
		static readonly string Cancel = "cancel";

		[TranslationReference]
		static readonly string Retry = "retry";

		[TranslationReference]
		static readonly string Back = "back";

		[ObjectCreator.UseCtor]
		public InstallFromDiscLogic(Widget widget, ModData modData, ModContent content, Dictionary<string, ModContent.ModSource> sources)
		{
			this.modData = modData;
			this.content = content;
			this.sources = sources;

			Log.AddChannel("install", "install.log");

			panel = widget.Get("DISC_INSTALL_PANEL");

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
			listTemplate = listPanel.Get<LabelWidget>("LIST_TEMPLATE");
			listPanel.RemoveChildren();

			listLabel = listContainer.Get<LabelWidget>("LIST_MESSAGE");

			DetectContentDisks();
		}

		static bool IsValidDrive(DriveInfo d)
		{
			if (d.DriveType == DriveType.CDRom && d.IsReady)
				return true;

			// HACK: the "TFD" DVD is detected as a fixed udf-formatted drive on OSX
			if (d.DriveType == DriveType.Fixed && d.DriveFormat == "udf")
				return true;

			return false;
		}

		void DetectContentDisks()
		{
			var message = modData.Translation.GetString(DetectingDrives);
			ShowProgressbar(modData.Translation.GetString(CheckingDiscs), () => message);
			ShowBackRetry(DetectContentDisks);

			new Task(() =>
			{
				var volumes = DriveInfo.GetDrives()
					.Where(IsValidDrive)
					.Select(v => v.RootDirectory.FullName);

				if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					// Outside of Gnome, most mounting tools on Linux don't set DriveType.CDRom
					// so provide a fallback by allowing users to manually mount images on known paths
					volumes = volumes.Concat(new[]
					{
						"/media/openra",
						"/media/" + Environment.UserName + "/openra",
						"/mnt/openra"
					});
				}

				foreach (var kv in sources)
				{
					message = modData.Translation.GetString(SearchingDiscFor, Translation.Arguments("title", kv.Value.Title));

					var path = FindSourcePath(kv.Value, volumes);
					if (path != null)
					{
						Log.Write("install", $"Using installer `{kv.Key}: {kv.Value.Title}` of type `{kv.Value.Type}`:");

						var packages = content.Packages.Values
							.Where(p => p.Sources.Contains(kv.Key) && !p.IsInstalled())
							.Select(p => p.Title);

						// Ignore disc if content is already installed
						if (packages.Any())
						{
							Game.RunAfterTick(() =>
							{
								ShowList(kv.Value.Title, modData.Translation.GetString(ContentPackageInstallation), packages);
								ShowContinueCancel(() => InstallFromDisc(path, kv.Value));
							});

							return;
						}
					}
				}

				var missingSources = content.Packages.Values
					.Where(p => !p.IsInstalled())
					.SelectMany(p => p.Sources)
					.Select(d => sources[d]);

				var discs = missingSources
					.Where(s => s.Type == ModContent.SourceType.Disc)
					.Select(s => s.Title)
					.Distinct();

				var options = new Dictionary<string, IEnumerable<string>>()
				{
					{ modData.Translation.GetString(GameDiscs), discs },
				};

				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					var installations = missingSources
						.Where(s => s.Type == ModContent.SourceType.RegistryDirectory || s.Type == ModContent.SourceType.RegistryDirectoryFromFile)
						.Select(s => s.Title)
						.Distinct();

					options.Add(modData.Translation.GetString(DigitalInstalls), installations);
				}

				Game.RunAfterTick(() =>
				{
					ShowList(modData.Translation.GetString(GameContentNotFound), modData.Translation.GetString(AlternativeContentSources), options);
					ShowBackRetry(DetectContentDisks);
				});
			}).Start();
		}

		void InstallFromDisc(string path, ModContent.ModSource modSource)
		{
			var message = "";
			ShowProgressbar(modData.Translation.GetString(InstallingContent), () => message);
			ShowDisabledCancel();

			new Task(() =>
			{
				var extracted = new List<string>();

				try
				{
					foreach (var i in modSource.Install)
					{
						var sourceAction = modSource.ObjectCreator.CreateObject<ISourceAction>($"{i.Key}SourceAction");
						sourceAction.RunActionOnSource(i.Value, path, modData, extracted, m => message = m);
					}

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
						ShowMessage(modData.Translation.GetString(InstallationFailed), modData.Translation.GetString(CheckInstallLog));
						ShowBackRetry(() => InstallFromDisc(path, modSource));
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

		void ShowList(string title, string message, IEnumerable<string> items)
		{
			visible = Mode.List;
			titleLabel.Text = title;
			listLabel.Text = message;

			listPanel.RemoveChildren();
			foreach (var i in items)
			{
				var item = i;
				var labelWidget = (LabelWidget)listTemplate.Clone();
				labelWidget.GetText = () => item;
				listPanel.AddChild(labelWidget);
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
					var labelWidget = (LabelWidget)listTemplate.Clone();
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
			primaryButton.Text = modData.Translation.GetString(Continue);
			primaryButton.Visible = true;

			secondaryButton.OnClick = Ui.CloseWindow;
			secondaryButton.Text = modData.Translation.GetString(Cancel);
			secondaryButton.Visible = true;
			secondaryButton.Disabled = false;
			Game.RunAfterTick(Ui.ResetTooltips);
		}

		void ShowBackRetry(Action retryAction)
		{
			primaryButton.OnClick = retryAction;
			primaryButton.Text = modData.Translation.GetString(Retry);
			primaryButton.Visible = true;

			secondaryButton.OnClick = Ui.CloseWindow;
			secondaryButton.Text = modData.Translation.GetString(Back);
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
