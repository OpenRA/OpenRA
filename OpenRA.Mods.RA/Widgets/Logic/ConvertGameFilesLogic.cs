#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ConvertGameFilesLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget extractingContainer;

		string[][] ExtractGameFiles, ExportToPng, ImportFromPng;

		[ObjectCreator.UseCtor]
		public ConvertGameFilesLogic(Widget widget, string[][] ExtractGameFiles, string[][] ExportToPng, string[][] ImportFromPng)
		{
			panel = widget.Get("CONVERT_ASSETS_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = Extract;

			extractingContainer = panel.Get("EXTRACTING");

			this.ExtractGameFiles = ExtractGameFiles;
			this.ExportToPng = ExportToPng;
			this.ImportFromPng = ImportFromPng;

			Extract();
		}

		void Extract()
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			extractingContainer.IsVisible = () => true;

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: "+s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			var t = new Thread( _ =>
			{
				try
				{
        			for (int i = 0; i < ExtractGameFiles.Length; i++)
					{
						progressBar.Percentage = i*100/ExtractGameFiles.Count();
						statusLabel.GetText = () => "Extracting...";
						Utility.Command.ExtractFiles(ExtractGameFiles[i]);
					}

					for (int i = 0; i < ExportToPng.Length; i++)
					{
						progressBar.Percentage = i*100/ExportToPng.Count();
						statusLabel.GetText = () => "Exporting SHP to PNG...";
						Utility.Command.ConvertShpToPng(ExportToPng[i]);
					}

					for (int i = 0; i < ImportFromPng.Length; i++)
					{
						progressBar.Percentage = i*100/ImportFromPng.Count();
						statusLabel.GetText = () => "Converting PNG to SHP...";
						Utility.Command.ConvertPngToShp(ImportFromPng[i]);
					}

					Game.RunAfterTick(() =>
					{
						progressBar.Percentage = 100;
						statusLabel.GetText = () => "Done. Check {0}".F(Platform.SupportDir);
						backButton.IsDisabled = () => false;
					});
				}
				catch (FileNotFoundException f)
				{
					onError(f.FileName+" not found.");
				}
				catch (Exception e)
				{
					onError(e.Message);
				}

			}) { IsBackground = true };
			t.Start();
		}
	}
}
