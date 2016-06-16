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
		enum Mode { Progress, Message, List }

		readonly ModContent content;

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
		readonly LabelWidget listTemplate;
		readonly LabelWidget listLabel;

		Mode visible = Mode.Progress;

		[ObjectCreator.UseCtor]
		public InstallFromDiscLogic(Widget widget, ModContent content, Action afterInstall)
		{
			this.content = content;

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
			listTemplate = listPanel.Get<LabelWidget>("LIST_TEMPLATE");
			listPanel.RemoveChildren();

			listLabel = listContainer.Get<LabelWidget>("LIST_MESSAGE");

			DetectContentDisks();
		}

		void DetectContentDisks()
		{
			var message = "Detecting drives";
			ShowProgressbar("Checking Discs", () => message);
			ShowBackRetry(DetectContentDisks);

			new Task(() =>
			{
				var volumes = DriveInfo.GetDrives()
					.Where(v => v.DriveType == DriveType.CDRom && v.IsReady)
					.Select(v => v.RootDirectory.FullName);

				foreach (var kv in content.Discs)
				{
					message = "Searching for " + kv.Value.Title;

					foreach (var volume in volumes)
					{
						if (PathIsDiscMount(volume, kv.Value))
						{
							var packages = content.Packages.Values
								.Where(p => p.Discs.Contains(kv.Key) && !p.IsInstalled())
								.Select(p => p.Title);

							// Ignore disc if content is already installed
							if (packages.Any())
							{
								Game.RunAfterTick(() =>
								{
									ShowList(kv.Value.Title, "The following content packages will be installed:", packages);
									ShowContinueCancel(() => InstallFromDisc(volume, kv.Value));
								});

								return;
							}
						}
					}
				}

				var discTitles = content.Packages.Values
					.Where(p => !p.IsInstalled())
					.SelectMany(p => p.Discs)
					.Select(d => content.Discs[d].Title)
					.Distinct();

				Game.RunAfterTick(() =>
				{
					ShowList("Disc Content Not Found", "Please insert or mount one of the following discs and try again", discTitles);
					ShowBackRetry(DetectContentDisks);
				});
			}).Start();
		}

		void InstallFromDisc(string path, ModContent.ModDisc disc)
		{
			var message = "";
			ShowProgressbar("Installing Content", () => message);
			ShowDisabledCancel();

			new Task(() =>
			{
				var extracted = new List<string>();

				try
				{
					foreach (var i in disc.Install)
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
									message = "Copying " + Path.GetFileName(sourcePath);
									extracted.Add(targetPath);
									Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
									File.Copy(sourcePath, targetPath);
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

							default:
								Game.Debug("debug", "Unknown installation command {0} - ignoring", i.Key);
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
						ShowBackRetry(() => InstallFromDisc(path, disc));
					});
				}
			}).Start();
		}

		enum ExtractionType { Raw, Blast }

		static void ExtractFromPackage(ExtractionType type, string path, MiniYaml actionYaml, List<string> extractedFiles, Action<string> updateMessage)
		{
			var sourcePath = Path.Combine(path, actionYaml.Value);
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
					using (var target = File.OpenWrite(targetPath))
					{
						Log.Write("install", "Extracting {0} -> {1}".F(sourcePath, targetPath));
						var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));
						if (type == ExtractionType.Blast)
						{
							Action<long, long> onProgress = (read, _) =>
								updateMessage("Extracting " + displayFilename + " ({0}%)".F(100 * read / length));
							Blast.Decompress(source, target, onProgress);
						}
						else
						{
							updateMessage("Extracting " + displayFilename);
							// This is a bit dumb memory-wise, but we load the whole thing when running the game anyway
							target.Write(source.ReadBytes(length));
						}
					}
				}
			}
		}

		static void ExtractFromMSCab(string path, MiniYaml actionYaml, List<string> extractedFiles, Action<string> updateMessage)
		{
			var sourcePath = Path.Combine(path, actionYaml.Value);
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
						// This is a bit dumb memory-wise, but we load the whole thing when running the game anyway
						Log.Write("install", "Extracting {0} -> {1}".F(sourcePath, targetPath));

						var displayFilename = Path.GetFileName(Path.GetFileName(targetPath));
						Action<int> onProgress = percent => updateMessage("Extracting {0} ({1}%)".F(displayFilename, percent));
						target.Write(reader.ExtractFile(node.Value.Value, onProgress));
					}
				}
			}
		}

		bool PathIsDiscMount(string path, ModContent.ModDisc disc)
		{
			try
			{
				foreach (var kv in disc.IDFiles)
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
