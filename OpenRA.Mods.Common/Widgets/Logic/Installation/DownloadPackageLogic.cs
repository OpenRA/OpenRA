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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DownloadPackageLogic : ChromeLogic
	{
		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

		[TranslationReference("title")]
		static readonly string Downloading = "downloading";

		[TranslationReference]
		static readonly string FetchingMirrorList = "fetching-mirror-list";

		[TranslationReference]
		static readonly string UnknownHost = "unknown-host";

		[TranslationReference("host", "received", "suffix")]
		static readonly string DownloadingFrom = "downloading-from";

		[TranslationReference("host", "received", "total", "suffix", "progress")]
		static readonly string DownloadingFromProgress = "downloading-from-progress";

		[TranslationReference]
		static readonly string VerifyingArchive = "verifying-archive";

		[TranslationReference]
		static readonly string ArchiveValidationFailed = "archive-validation-failed";

		[TranslationReference]
		static readonly string Extracting = "extracting";

		[TranslationReference("entry")]
		static readonly string ExtractingEntry = "extracting-entry";

		[TranslationReference]
		static readonly string ArchiveExtractionFailed = "archive-extraction-failed";

		[TranslationReference]
		static readonly string MirrorSelectionFailed = "mirror-selection-failed";

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

			var text = modData.Translation.GetString(Downloading, Translation.Arguments("title", download.Title));
			panel.Get<LabelWidget>("TITLE").Text = text;

			ShowDownloadDialog();
		}

		void ShowDownloadDialog()
		{
			getStatusText = () => modData.Translation.GetString(FetchingMirrorList);
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
				var host = downloadHost ?? modData.Translation.GetString(UnknownHost);

				if (total < 0)
				{
					mag = (int)Math.Log(read, 1024);
					dataReceived = read / (float)(1L << (mag * 10));
					dataSuffix = SizeSuffixes[mag];

					getStatusText = () => modData.Translation.GetString(DownloadingFrom,
						Translation.Arguments("host", host, "received", $"{dataReceived:0.00}", "suffix", dataSuffix));
					progressBar.Indeterminate = true;
				}
				else
				{
					mag = (int)Math.Log(total, 1024);
					dataTotal = total / (float)(1L << (mag * 10));
					dataReceived = read / (float)(1L << (mag * 10));
					dataSuffix = SizeSuffixes[mag];

					getStatusText = () => modData.Translation.GetString(DownloadingFromProgress,
						Translation.Arguments("host", host, "received", $"{dataReceived:0.00}", "total", $"{dataTotal:0.00}",
							"suffix", dataSuffix, "progress", progressPercentage));
					progressBar.Indeterminate = false;
				}

				progressBar.Percentage = progressPercentage;
			}

			Action<string> onExtractProgress = s => Game.RunAfterTick(() => getStatusText = () => s);

			Action<string> onError = s => Game.RunAfterTick(() =>
			{
				var host = downloadHost ?? modData.Translation.GetString(UnknownHost);
				Log.Write("install", $"Download from {host} failed: " + s);

				progressBar.Indeterminate = false;
				progressBar.Percentage = 100;
				getStatusText = () => $"{host}: Error: {s}";
				retryButton.IsVisible = () => true;
				cancelButton.OnClick = Ui.CloseWindow;
			});

			Action<string> downloadUrl = url =>
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

						using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192, true))
						{
							await response.ReadAsStreamWithProgress(fileStream, OnDownloadProgress, token);
						}

						// Validate integrity
						if (!string.IsNullOrEmpty(download.SHA1))
						{
							getStatusText = () => modData.Translation.GetString(VerifyingArchive);
							progressBar.Indeterminate = true;

							var archiveValid = false;
							try
							{
								using (var stream = File.OpenRead(file))
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
								onError(modData.Translation.GetString(ArchiveValidationFailed));
								return;
							}
						}

						// Automatically extract
						getStatusText = () => modData.Translation.GetString(Extracting);
						progressBar.Indeterminate = true;

						var extracted = new List<string>();
						try
						{
							using (var stream = File.OpenRead(file))
							using (var z = new ZipFile(stream))
							{
								foreach (var kv in download.Extract)
								{
									var entry = z.GetEntry(kv.Value);
									if (entry == null || !entry.IsFile)
										continue;

									onExtractProgress(modData.Translation.GetString(ExtractingEntry, Translation.Arguments("entry", entry.Name)));
									Log.Write("install", "Extracting " + entry.Name);
									var targetPath = Platform.ResolvePath(kv.Key);
									Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
									extracted.Add(targetPath);

									using (var zz = z.GetInputStream(entry))
									using (var f = File.Create(targetPath))
										zz.CopyTo(f);
								}

								z.Close();
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

							onError(modData.Translation.GetString(ArchiveExtractionFailed));
						}
					}
					catch (Exception e)
					{
						onError(e.ToString());
					}
					finally
					{
						File.Delete(file);
					}
				}, token);
			};

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

						var mirrorList = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
						downloadUrl(mirrorList.Random(new MersenneTwister()));
					}
					catch (Exception e)
					{
						Log.Write("install", "Mirror selection failed with error:");
						Log.Write("install", e.ToString());
						onError(modData.Translation.GetString(MirrorSelectionFailed));
					}
				});
			}
			else
				downloadUrl(download.URL);
		}
	}
}
