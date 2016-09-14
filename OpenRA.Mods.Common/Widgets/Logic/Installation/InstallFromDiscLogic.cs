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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallFromDiscLogic : ChromeLogic
	{
		// Hide percentage indicators for files smaller than 25 MB
		const int ShowPercentageThreshold = 26214400;

		enum Mode { Progress, Message, List }

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

		[ObjectCreator.UseCtor]
		public InstallFromDiscLogic(Widget widget, ModContent content, Dictionary<string, ModContent.ModSource> sources, Action afterInstall)
		{
			this.content = content;
			this.sources = sources;

			Log.AddChannel("install", "install.log");

			// this.afterInstall = afterInstall;
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
			var message = "Detecting drives";
			ShowProgressbar("Checking Discs", () => message);
			ShowBackRetry(DetectContentDisks);

			new Task(() =>
			{
				var volumes = DriveInfo.GetDrives()
					.Where(IsValidDrive)
					.Select(v => v.RootDirectory.FullName);

				foreach (var kv in sources)
				{
					message = "Searching for " + kv.Value.Title;

					var path = FindSourcePath(kv.Value, volumes);
					if (path != null)
					{
						var packages = content.Packages.Values
							.Where(p => p.Sources.Contains(kv.Key) && !p.IsInstalled())
							.Select(p => p.Title);

						// Ignore disc if content is already installed
						if (packages.Any())
						{
							Game.RunAfterTick(() =>
							{
								ShowList(kv.Value.Title, "The following content packages will be installed:", packages);
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
					{ "Game Discs", discs },
				};

				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					var installations = missingSources
						.Where(s => s.Type == ModContent.SourceType.Install)
						.Select(s => s.Title)
						.Distinct();

					options.Add("Digital Installs", installations);
				}

				Game.RunAfterTick(() =>
				{
					ShowList("Game Content Not Found", "Please insert or install one of the following content sources:", options);
					ShowBackRetry(DetectContentDisks);
				});
			}).Start();
		}

		void InstallFromDisc(string path, ModContent.ModSource modSource)
		{
			var message = "";
			ShowProgressbar("Installing Content", () => message);
			ShowDisabledCancel();

			new Task(() =>
			{
				var extracted = new List<string>();

				try
				{
					foreach (var i in modSource.Install)
					{
						switch (i.Key)
						{
							case "copy":
							{
								var sourceDir = Path.Combine(path, i.Value.Value);
								foreach (var node in i.Value.Nodes)
								{
									var sourcePath = Path.Combine(sourceDir, node.Value.Value);
									var targetPath = Platform.ResolvePath(node.Key);
									if (File.Exists(targetPath))
									{
										Log.Write("install", "Ignoring installed file " + targetPath);
										continue;
									}

									Log.Write("install", "Copying {0} -> {1}".F(sourcePath, targetPath));
									extracted.Add(targetPath);
									Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

									using (var source = File.OpenRead(sourcePath))
									using (var target = File.OpenWrite(targetPath))
									{
										var displayFilename = Path.GetFileName(targetPath);
										var length = source.Length;

										Action<long> onProgress = null;
										if (length < ShowPercentageThreshold)
											message = "Copying " + displayFilename;
										else
											onProgress = b => message = "Copying " + displayFilename + " ({0}%)".F(100 * b / length);

										CopyStream(source, target, length, onProgress);
									}
								}

								break;
							}

							case "extract-raw":
							{
								ExtractFromPackage(ExtractionType.Raw, path, i.Value, extracted, m => message = m);
								break;
							}

							case "extract-blast":
							{
								ExtractFromPackage(ExtractionType.Blast, path, i.Value, extracted, m => message = m);
								break;
							}

							case "extract-mscab":
							{
								ExtractFromMSCab(path, i.Value, extracted, m => message = m);
								break;
							}

							case "extract-iscab":
							{
								ExtractFromISCab(path, i.Value, extracted, m => message = m);
								break;
							}

							case "delete":
							{
								var sourcePath = Path.Combine(path, i.Value.Value);

								// Try as an absolute path
								if (!File.Exists(sourcePath))
									sourcePath = Platform.ResolvePath(i.Value.Value);

								Log.Write("debug", "Deleting {0}", sourcePath);
								File.Delete(sourcePath);
								break;
							}

							default:
								Log.Write("debug", "Unknown installation command {0} - ignoring", i.Key);
								break;
						}
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
						ShowMessage("Installation Failed", "Refer to install.log in the logs directory for details.");
						ShowBackRetry(() => InstallFromDisc(path, modSource));
					});
				}
			}).Start();
		}

		static void CopyStream(Stream input, Stream output, long length, Action<long> onProgress = null)
		{
			var buffer = new byte[4096];
			var copied = 0L;
			while (copied < length)
			{
				var read = (int)Math.Min(buffer.Length, length - copied);
				var write = input.Read(buffer, 0, read);
				output.Write(buffer, 0, write);
				copied += write;

				if (onProgress != null)
					onProgress(copied);
			}
		}

		enum ExtractionType { Raw, Blast }

		static void ExtractFromPackage(ExtractionType type, string path, MiniYaml actionYaml, List<string> extractedFiles, Action<string> updateMessage)
		{
			var sourcePath = Path.Combine(path, actionYaml.Value);

			// Try as an absolute path
			if (!File.Exists(sourcePath))
				sourcePath = Platform.ResolvePath(actionYaml.Value);

			using (var source = File.OpenRead(sourcePath))
			{
				foreach (var node in actionYaml.Nodes)
				{
					var targetPath = Platform.ResolvePath(node.Key);

					if (File.Exists(targetPath))
					{
						Log.Write("install", "Skipping installed file " + targetPath);
						continue;
					}

					var offsetNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Offset");
					if (offsetNode == null)
					{
						Log.Write("install", "Skipping entry with missing Offset definition " + targetPath);
						continue;
					}

					var lengthNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Length");
					if (lengthNode == null)
					{
						Log.Write("install", "Skipping entry with missing Length definition " + targetPath);
						continue;
					}

					var length = FieldLoader.GetValue<int>("Length", lengthNode.Value.Value);
					source.Position = FieldLoader.GetValue<int>("Offset", offsetNode.Value.Value);

					extractedFiles.Add(targetPath);
					Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
					var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));

					Action<long> onProgress = null;
					if (length < ShowPercentageThreshold)
						updateMessage("Extracting " + displayFilename);
					else
						onProgress = b => updateMessage("Extracting " + displayFilename + " ({0}%)".F(100 * b / length));

					using (var target = File.OpenWrite(targetPath))
					{
						Log.Write("install", "Extracting {0} -> {1}".F(sourcePath, targetPath));
						if (type == ExtractionType.Blast)
						{
							Action<long, long> onBlastProgress = (read, _) =>
							{
								if (onProgress != null)
									onProgress(read);
							};

							Blast.Decompress(source, target, onBlastProgress);
						}
						else
							CopyStream(source, target, length, onProgress);
					}
				}
			}
		}

		static void ExtractFromMSCab(string path, MiniYaml actionYaml, List<string> extractedFiles, Action<string> updateMessage)
		{
			var sourcePath = Path.Combine(path, actionYaml.Value);

			// Try as an absolute path
			if (!File.Exists(sourcePath))
				sourcePath = Platform.ResolvePath(actionYaml.Value);

			using (var source = File.OpenRead(sourcePath))
			{
				var reader = new MSCabCompression(source);
				foreach (var node in actionYaml.Nodes)
				{
					var targetPath = Platform.ResolvePath(node.Key);

					if (File.Exists(targetPath))
					{
						Log.Write("install", "Skipping installed file " + targetPath);
						continue;
					}

					extractedFiles.Add(targetPath);
					Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
					using (var target = File.OpenWrite(targetPath))
					{
						Log.Write("install", "Extracting {0} -> {1}".F(sourcePath, targetPath));
						var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));
						Action<int> onProgress = percent => updateMessage("Extracting {0} ({1}%)".F(displayFilename, percent));
						reader.ExtractFile(node.Value.Value, target, onProgress);
					}
				}
			}
		}

		static void ExtractFromISCab(string path, MiniYaml actionYaml, List<string> extractedFiles, Action<string> updateMessage)
		{
			var sourcePath = Path.Combine(path, actionYaml.Value);

			// Try as an absolute path
			if (!File.Exists(sourcePath))
				sourcePath = Platform.ResolvePath(actionYaml.Value);

			var volumeNode = actionYaml.Nodes.FirstOrDefault(n => n.Key == "Volumes");
			if (volumeNode == null)
				throw new InvalidDataException("extract-iscab entry doesn't define a Volumes node");

			var extractNode = actionYaml.Nodes.FirstOrDefault(n => n.Key == "Extract");
			if (extractNode == null)
				throw new InvalidDataException("extract-iscab entry doesn't define an Extract node");

			var volumes = new Dictionary<int, Stream>();
			try
			{
				foreach (var node in volumeNode.Value.Nodes)
				{
					var volume = FieldLoader.GetValue<int>("(key)", node.Key);
					var stream = File.OpenRead(Path.Combine(path, node.Value.Value));
					volumes.Add(volume, stream);
				}

				using (var source = File.OpenRead(sourcePath))
				{
					var reader = new InstallShieldCABCompression(source, volumes);
					foreach (var node in extractNode.Value.Nodes)
					{
						var targetPath = Platform.ResolvePath(node.Key);

						if (File.Exists(targetPath))
						{
							Log.Write("install", "Skipping installed file " + targetPath);
							continue;
						}

						extractedFiles.Add(targetPath);
						Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
						using (var target = File.OpenWrite(targetPath))
						{
							Log.Write("install", "Extracting {0} -> {1}".F(sourcePath, targetPath));
							var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));
							Action<int> onProgress = percent => updateMessage("Extracting {0} ({1}%)".F(displayFilename, percent));
							reader.ExtractFile(node.Value.Value, target, onProgress);
						}
					}
				}
			}
			finally
			{
				foreach (var kv in volumes)
					kv.Value.Dispose();
			}
		}

		string FindSourcePath(ModContent.ModSource source, IEnumerable<string> volumes)
		{
			if (source.Type == ModContent.SourceType.Install)
			{
				if (source.RegistryKey == null)
					return null;

				if (Platform.CurrentPlatform != PlatformType.Windows)
					return null;

				var path = Microsoft.Win32.Registry.GetValue(source.RegistryKey, source.RegistryValue, null) as string;
				if (path == null)
					return null;

				return IsValidSourcePath(path, source) ? path : null;
			}

			if (source.Type == ModContent.SourceType.Disc)
				foreach (var volume in volumes)
					if (IsValidSourcePath(volume, source))
						return volume;

			return null;
		}

		bool IsValidSourcePath(string path, ModContent.ModSource source)
		{
			try
			{
				foreach (var kv in source.IDFiles)
				{
					var filePath = Path.Combine(path, kv.Key);
					if (!File.Exists(filePath))
						return false;

					using (var fileStream = File.OpenRead(filePath))
					using (var csp = SHA1.Create())
					{
						var hash = new string(csp.ComputeHash(fileStream).SelectMany(a => a.ToString("x2")).ToArray());
						if (hash != kv.Value)
							return false;
					}
				}
			}
			catch (Exception)
			{
				return false;
			}

			return true;
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
			primaryButton.Text = "Continue";
			primaryButton.Visible = true;

			secondaryButton.OnClick = Ui.CloseWindow;
			secondaryButton.Text = "Cancel";
			secondaryButton.Visible = true;
			secondaryButton.Disabled = false;
			Game.RunAfterTick(Ui.ResetTooltips);
		}

		void ShowBackRetry(Action retryAction)
		{
			primaryButton.OnClick = retryAction;
			primaryButton.Text = "Retry";
			primaryButton.Visible = true;

			secondaryButton.OnClick = Ui.CloseWindow;
			secondaryButton.Text = "Back";
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
