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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DownloadPackageLogic : ChromeLogic
	{
		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		readonly ModContent.ModDownload download;
		readonly Action onSuccess;

		readonly Widget panel;
		readonly ProgressBarWidget progressBar;

		Func<string> getStatusText = () => "";
		string downloadHost;

		[ObjectCreator.UseCtor]
		public DownloadPackageLogic(Widget widget, ModContent.ModDownload download, Action onSuccess)
		{
			this.download = download;
			this.onSuccess = onSuccess;

			Log.AddChannel("install", "install.log");

			panel = widget.Get("PACKAGE_DOWNLOAD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");

			var statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");
			var statusFont = Game.Renderer.Fonts[statusLabel.Font];
			var status = new CachedTransform<string, string>(s => WidgetUtils.TruncateText(s, statusLabel.Bounds.Width, statusFont));
			statusLabel.GetText = () => status.Update(getStatusText());

			var text = "Downloading {0}".F(download.Title);
			panel.Get<LabelWidget>("TITLE").Text = text;

			ShowDownloadDialog();
		}

		void ShowDownloadDialog()
		{
			getStatusText = () => "Fetching list of mirrors...";
			progressBar.Indeterminate = true;

			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.IsVisible = () => false;

			var cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");

			var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

			Action deleteTempFile = () =>
			{
				Log.Write("install", "Deleting temporary file " + file);
				File.Delete(file);
			};

			Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
			{
				var dataReceived = 0.0f;
				var dataTotal = 0.0f;
				var mag = 0;
				var dataSuffix = "";

				if (i.TotalBytesToReceive < 0)
				{
					dataTotal = float.NaN;
					dataReceived = i.BytesReceived;
					dataSuffix = SizeSuffixes[0];
				}
				else
				{
					mag = (int)Math.Log(i.TotalBytesToReceive, 1024);
					dataTotal = i.TotalBytesToReceive / (float)(1L << (mag * 10));
					dataReceived = i.BytesReceived / (float)(1L << (mag * 10));
					dataSuffix = SizeSuffixes[mag];
				}

				progressBar.Indeterminate = false;
				progressBar.Percentage = i.ProgressPercentage;

				getStatusText = () => "Downloading from {4} {1:0.00}/{2:0.00} {3} ({0}%)".F(i.ProgressPercentage,
					dataReceived, dataTotal, dataSuffix,
					downloadHost ?? "unknown host");
			};

			Action<string> onExtractProgress = s => Game.RunAfterTick(() => getStatusText = () => s);

			Action<string> onError = s => Game.RunAfterTick(() =>
			{
				Log.Write("install", "Download failed: " + s);

				progressBar.Indeterminate = false;
				progressBar.Percentage = 100;
				getStatusText = () => "Error: " + s;
				retryButton.IsVisible = () => true;
			});

			Action<AsyncCompletedEventArgs> onDownloadComplete = i =>
			{
				if (i.Cancelled)
				{
					deleteTempFile();
					Game.RunAfterTick(Ui.CloseWindow);
					return;
				}

				if (i.Error != null)
				{
					deleteTempFile();
					onError(Download.FormatErrorMessage(i.Error));
					return;
				}

				// Automatically extract
				getStatusText = () => "Extracting...";
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

							onExtractProgress("Extracting " + entry.Name);
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

					Game.RunAfterTick(() => { Ui.CloseWindow(); onSuccess(); });
				}
				catch (Exception)
				{
					Log.Write("install", "Extraction failed");

					foreach (var f in extracted)
					{
						Log.Write("install", "Deleting " + f);
						File.Delete(f);
					}

					onError("Invalid archive");
				}
				finally
				{
					deleteTempFile();
				}
			};

			Action<string> downloadUrl = url =>
			{
				Log.Write("install", "Downloading " + url);

				downloadHost = new Uri(url).Host;
				var dl = new Download(url, file, onDownloadProgress, onDownloadComplete);
				cancelButton.OnClick = dl.CancelAsync;
				retryButton.OnClick = ShowDownloadDialog;
			};

			if (download.MirrorList != null)
			{
				Log.Write("install", "Fetching mirrors from " + download.MirrorList);

				Action<DownloadDataCompletedEventArgs> onFetchMirrorsComplete = i =>
				{
					progressBar.Indeterminate = true;

					if (i.Cancelled)
					{
						Game.RunAfterTick(Ui.CloseWindow);
						return;
					}

					if (i.Error != null)
					{
						onError(Download.FormatErrorMessage(i.Error));
						return;
					}

					try
					{
						var data = Encoding.UTF8.GetString(i.Result);
						var mirrorList = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
						downloadUrl(mirrorList.Random(new MersenneTwister()));
					}
					catch (Exception e)
					{
						Log.Write("install", "Mirror selection failed with error:");
						Log.Write("install", e.ToString());
						onError("Online mirror is not available. Please install from an original disc.");
					}
				};

				var updateMirrors = new Download(download.MirrorList, onDownloadProgress, onFetchMirrorsComplete);
				cancelButton.OnClick = updateMirrors.CancelAsync;
				retryButton.OnClick = ShowDownloadDialog;
			}
			else
				downloadUrl(download.URL);
		}
	}
}
