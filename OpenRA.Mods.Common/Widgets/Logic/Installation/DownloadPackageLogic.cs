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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DownloadPackageLogic : ChromeLogic
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
		const string VerifyingArchive = "label-verifying-archive";

		[FluentReference]
		const string ArchiveValidationFailed = "label-archive-validation-failed";

		[FluentReference]
		const string Extracting = "label-extracting-archive";

		[FluentReference("entry")]
		const string ExtractingEntry = "label-extracting-archive-entry";

		[FluentReference]
		const string ArchiveExtractionFailed = "label-archive-extraction-failed";

		[FluentReference]
		const string MirrorSelectionFailed = "label-mirror-selection-failed";

		static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

		readonly ModData modData;
		readonly ModContent.ModDownload download;
		readonly Action onSuccess;

		readonly Widget panel;
		readonly ProgressBarWidget progressBar;

		Func<string> getStatusText = () => "";
		string downloadHost;

		[ObjectCreator.UseCtor]
		public DownloadPackageLogic(Widget widget, ModData modData, ModContent.ModDownload download, Action onSuccess)
		{
			this.modData = modData;
			this.download = download;
			this.onSuccess = onSuccess;

			Log.AddChannel("install", "install.log");

			panel = widget.Get("PACKAGE_DOWNLOAD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");

			var statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");
			var statusFont = Game.Renderer.Fonts[statusLabel.Font];
			var status = new CachedTransform<string, string>(s => WidgetUtils.TruncateText(s, statusLabel.Bounds.Width, statusFont));
			statusLabel.GetText = () => status.Update(getStatusText());

			var text = FluentProvider.GetMessage(Downloading, "title", download.Title);
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

			void OnExtractProgress(string s) => Game.RunAfterTick(() => getStatusText = () => s);

			void OnError(string s) => Game.RunAfterTick(() =>
			{
				var host = downloadHost ?? FluentProvider.GetMessage(UnknownHost);
				Log.Write("install", $"Download from {host} failed: " + s);

				progressBar.Indeterminate = false;
				progressBar.Percentage = 100;
				getStatusText = () => $"{host}: Error: {s}";
				retryButton.IsVisible = () => true;
				cancelButton.OnClick = Ui.CloseWindow;
			});

			void DownloadUrl(string url)
			{
				Log.Write("install", "Downloading " + url);

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
							getStatusText = () => FluentProvider.GetMessage(VerifyingArchive);
							progressBar.Indeterminate = true;

							var archiveValid = false;
							try
							{
								await using (var stream = File.OpenRead(file))
								{
									var archiveSHA1 = CryptoUtil.SHA1Hash(stream);
									Log.Write("install", "Downloaded SHA1: " + archiveSHA1);
									Log.Write("install", "Expected SHA1: " + download.SHA1);

									archiveValid = archiveSHA1 == download.SHA1;
								}
							}
							catch (Exception e)
							{
								Log.Write("install", "SHA1 calculation failed: " + e.ToString());
							}

							if (!archiveValid)
							{
								OnError(FluentProvider.GetMessage(ArchiveValidationFailed));
								return;
							}
						}

						// Automatically extract
						getStatusText = () => FluentProvider.GetMessage(Extracting);
						progressBar.Indeterminate = true;

						var extracted = new List<string>();
						try
						{
							await using (var stream = File.OpenRead(file))
							{
								var packageLoader = modData.ObjectCreator.CreateObject<IPackageLoader>($"{download.Type}Loader");

								if (packageLoader.TryParsePackage(stream, file, modData.ModFiles, out var package))
								{
									foreach (var kv in download.Extract)
									{
										if (!package.Contains(kv.Value))
										{
											Log.Write("install", $"Downloaded package does not contain {kv.Value} - skipping.");
											continue;
										}

										OnExtractProgress(FluentProvider.GetMessage(ExtractingEntry, "entry", kv.Value));
										Log.Write("install", "Extracting " + kv.Value);
										var targetPath = Platform.ResolvePath(kv.Key);
										Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
										extracted.Add(targetPath);

										await using (var zz = package.GetStream(kv.Value))
										await using (var f = File.Create(targetPath))
											await zz.CopyToAsync(f);
									}

									package.Dispose();
								}
							}

							Game.RunAfterTick(() =>
							{
								Ui.CloseWindow();
								onSuccess();
							});
						}
						catch (Exception e)
						{
							Log.Write("install", "Archive extraction failed: " + e.ToString());

							foreach (var f in extracted)
							{
								Log.Write("install", "Deleting " + f);
								File.Delete(f);
							}

							OnError(FluentProvider.GetMessage(ArchiveExtractionFailed));
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
				Log.Write("install", "Fetching mirrors from " + download.MirrorList);

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
						Log.Write("install", "Mirror selection failed with error:");
						Log.Write("install", e.ToString());
						OnError(FluentProvider.GetMessage(MirrorSelectionFailed));
					}
				});
			}
			else
				DownloadUrl(download.URL);
		}
	}
}
