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
using System.Diagnostics;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Widgets;
using OpenRA.Utility;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ExtractGameFilesLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget extractingContainer;

		string[][] ExtractGameFiles, ExportToPng;

		[ObjectCreator.UseCtor]
		public ExtractGameFilesLogic(Widget widget, string[][] ExtractGameFiles, string[][] ExportToPng)
		{
			panel = widget.Get("EXTRACT_ASSETS_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = Extract;

			extractingContainer = panel.Get("EXTRACTING");

			this.ExtractGameFiles = ExtractGameFiles;
			foreach (var s in ExtractGameFiles)
				foreach (var ss in s)
					Console.WriteLine(ss);
			this.ExportToPng = ExportToPng;

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
						statusLabel.GetText = () => "Converting...";
						Utility.Command.ConvertShpToPng(ExportToPng[i]);
					}

					Game.RunAfterTick(() =>
					{
						progressBar.Percentage = 100;
						statusLabel.GetText = () => "Extraction and conversion complete.";
						backButton.IsDisabled = () => false;
					});
				}
				catch
				{
					onError("Extraction or conversion failed");
				}
			}) { IsBackground = true };
			t.Start();
		}
	}
}
