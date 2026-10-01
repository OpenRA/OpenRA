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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ContentDownloadLogic : ChromeLogic
	{
		[FluentReference("title")]
		const string Downloading = "label-downloading";

		[FluentReference]
		const string FetchingMirrorList = "label-fetching-mirror-list";

		[FluentReference]
		const string UnknownHost = "label-unknown-host";

		[FluentReference]
		const string DownloadFailed = "label-download-failed";

		[FluentReference("host", "received", "suffix")]
		const string DownloadingFrom = "label-downloading-from";

		[FluentReference("host", "received", "total", "suffix", "progress")]
		const string DownloadingFromProgress = "label-downloading-from-progress";

		[FluentReference]
		const string Verifying = "label-verifying";

		[FluentReference]
		const string ValidationFailed = "label-validation-failed";

		[FluentReference]
		const string Saving = "label-saving";

		[FluentReference]
		const string SavingFailed = "label-saving-failed";

		[FluentReference]
		const string MirrorSelectionFailed = "label-mirror-selection-failed";

		static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

		readonly ContentSourcesModContent.Download download;
		readonly Action onSuccess;

		readonly Widget panel;
		readonly ProgressBarWidget progressBar;

		Func<string> getStatusText = () => "";
		string downloadHost;

		[ObjectCreator.UseCtor]
		public ContentDownloadLogic(Widget widget, ContentSourcesModContent.Download download, Action onSuccess)
		{
			this.download = download;
			this.onSuccess = onSuccess;

			panel = widget.Get("PACKAGE_DOWNLOAD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");

			var statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");
			var statusFont = Game.Renderer.Fonts[statusLabel.Font];
			var status = new CachedTransform<string, string>(s => WidgetUtils.TruncateText(s, statusLabel.Bounds.Width, statusFont));
			statusLabel.GetText = () => status.Update(getStatusText());

			var text = FluentProvider.GetMessage(Downloading, "title", FluentProvider.GetMessage(download.Title));
			panel.Get<LabelWidget>("TITLE").GetText = () => text;

			ShowDownloadDialog();
		}

		void ShowDownloadDialog()
		{
			getStatusText = () => FluentProvider.GetMessage(FetchingMirrorList);
			progressBar.Indeterminate = true;

			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.IsVisible = () => false;

			var cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");

			void OnDownloadProgress(long total, long read, int progressPercentage)
			{
				var dataReceived = 0.0f;
				var dataTotal = 0.0f;
				var mag = 0;
				var dataSuffix = "";
				var host = downloadHost ?? FluentProvider.GetMessage(UnknownHost);

				if (total < 0)
				{
					mag = (int)Math.Log(read, 1024);
					dataReceived = read / (float)(1L << (mag * 10));
					dataSuffix = SizeSuffixes[mag];

					getStatusText = () => FluentProvider.GetMessage(DownloadingFrom,
						"host", host,
						"received", $"{dataReceived:0.00}",
						"suffix", dataSuffix);
					progressBar.Indeterminate = true;
				}
				else
				{
					mag = (int)Math.Log(total, 1024);
					dataTotal = total / (float)(1L << (mag * 10));
					dataReceived = read / (float)(1L << (mag * 10));
					dataSuffix = SizeSuffixes[mag];

					getStatusText = () => FluentProvider.GetMessage(DownloadingFromProgress,
						"host", host,
						"received", $"{dataReceived:0.00}",
						"total", $"{dataTotal:0.00}",
						"suffix", dataSuffix,
						"progress", progressPercentage);
					progressBar.Indeterminate = false;
				}

				progressBar.Percentage = progressPercentage;
			}

			void OnError(string s) => Game.RunAfterTick(() =>
			{
				var host = downloadHost ?? FluentProvider.GetMessage(UnknownHost);
				Log.Write("debug", $"Download from {host} failed: " + s);

				progressBar.Indeterminate = false;
				progressBar.Percentage = 100;
				getStatusText = () => $"{host}: Error: {s}";
				retryButton.IsVisible = () => true;
				cancelButton.OnClick = Ui.CloseWindow;
			});

			void DownloadUrl(string url)
			{
				Log.Write("debug", "Downloading " + url);

				var tokenSource = new CancellationTokenSource();
				var token = tokenSource.Token;
				downloadHost = new Uri(url).Host;

				cancelButton.OnClick = () =>
				{
					tokenSource.Cancel();
					Game.RunAfterTick(Ui.CloseWindow);
				};

				retryButton.OnClick = ShowDownloadDialog;

				Task.Run(async () =>
				{
					var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

					try
					{
						var client = HttpClientFactory.Create();

						var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

						if (response.StatusCode != HttpStatusCode.OK)
						{
							OnError(FluentProvider.GetMessage(DownloadFailed));
							return;
						}

						await using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192, true))
						{
							await response.ReadAsStreamWithProgress(fileStream, OnDownloadProgress, token);
						}

						// Validate integrity
						if (!string.IsNullOrEmpty(download.SHA1))
						{
							getStatusText = () => FluentProvider.GetMessage(Verifying);
							progressBar.Indeterminate = true;

							var archiveValid = false;
							try
							{
								await using (var stream = File.OpenRead(file))
								{
									var archiveSHA1 = CryptoUtil.SHA1Hash(stream);
									Log.Write("debug", "Downloaded SHA1: " + archiveSHA1);
									Log.Write("debug", "Expected SHA1: " + download.SHA1);

									archiveValid = archiveSHA1 == download.SHA1;
								}
							}
							catch (Exception e)
							{
								Log.Write("debug", "SHA1 calculation failed: " + e);
							}

							if (!archiveValid)
							{
								OnError(FluentProvider.GetMessage(ValidationFailed));
								return;
							}
						}

						getStatusText = () => FluentProvider.GetMessage(Saving);
						progressBar.Indeterminate = true;

						try
						{
							await using (var stream = File.OpenRead(file))
							{
								var targetPath = Platform.ResolvePath(download.Path);
								Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

								await using (var f = File.Create(targetPath))
									await stream.CopyToAsync(f);
							}

							Game.RunAfterTick(() =>
							{
								Ui.CloseWindow();
								onSuccess();
							});
						}
						catch (Exception e)
						{
							Log.Write("debug", "saving failed: " + e);
							OnError(FluentProvider.GetMessage(SavingFailed));
						}
					}
					catch (Exception e)
					{
						OnError(e.ToString());
					}
					finally
					{
						File.Delete(file);
					}
				}, token);
			}

			if (download.MirrorList != null)
			{
				Log.Write("debug", "Fetching mirrors from " + download.MirrorList);

				Task.Run(async () =>
				{
					try
					{
						var client = HttpClientFactory.Create();
						var httpResponseMessage = await client.GetAsync(download.MirrorList);
						var result = await httpResponseMessage.Content.ReadAsStringAsync();

						var mirrorList = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
						DownloadUrl(mirrorList.Random(new MersenneTwister()));
					}
					catch (Exception e)
					{
						Log.Write("debug", "Mirror selection failed with error:");
						Log.Write("debug", e.ToString());
						OnError(FluentProvider.GetMessage(MirrorSelectionFailed));
					}
				});
			}
			else
				DownloadUrl(download.URL);
		}
	}
}
