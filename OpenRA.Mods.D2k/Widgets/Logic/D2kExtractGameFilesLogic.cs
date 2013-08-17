#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Utility;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class D2kExtractGameFilesLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget extractingContainer, copyFilesContainer;

		[ObjectCreator.UseCtor]
		public D2kExtractGameFilesLogic(Widget widget, Action continueLoading)
		{
			panel = widget.Get("EXTRACT_GAMEFILES_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = Extract;

			extractingContainer = panel.Get("EXTRACTING");
			copyFilesContainer = panel.Get("COPY_FILES");

			Extract();
			this.continueLoading = continueLoading;
		}

		void Extract()
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			copyFilesContainer.IsVisible = () => false;
			extractingContainer.IsVisible = () => true;

			var pathToDataR8 = Path.Combine(Platform.SupportDir, "Content/d2k/DATA.R8");
			var pathToPalette = "mods/d2k/bits/d2k.pal";
			var pathToSHPs = Path.Combine(Platform.SupportDir, "Content/d2k/SHPs");
			var pathToTilesets = Path.Combine(Platform.SupportDir, "Content/d2k/Tilesets");

			var extractGameFiles = new string[][]
			{
				new string[] { "--r8", pathToDataR8, pathToPalette, "102", "105", Path.Combine(pathToSHPs, "crates") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "107", "109", Path.Combine(pathToSHPs, "spicebloom") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "114", "129", Path.Combine(pathToSHPs, "rockcrater1") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "130", "145", Path.Combine(pathToSHPs, "rockcrater2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "146", "161", Path.Combine(pathToSHPs, "sandcrater1") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "162", "177", Path.Combine(pathToSHPs, "sandcrater2") },

				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "BASE"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), pathToPalette, "748", "749", Path.Combine(pathToSHPs, "spice0") },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBAT.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "BAT"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBGBS.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "BGBS"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXICE.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "ICE"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXTREE.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "TREE"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXWAST.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "WAST"), "--tileset" },
				////new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXXMAS.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "XMAS"), "--tileset" },
			};

			var shpToCreate = new string[][]
			{
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockcrater1.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockcrater2.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sandcrater1.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sandcrater2.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "spice0.png"), "32" },
			};

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: " + s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			var t = new Thread(_ =>
			{
				try
				{
        			for (int i = 0; i < extractGameFiles.Length; i++)
					{
						progressBar.Percentage = i * 100 / extractGameFiles.Count();
						statusLabel.GetText = () => "Extracting...";
						Utility.Command.ConvertR8ToPng(extractGameFiles[i]);
					}

					for (int i = 0; i < shpToCreate.Length; i++)
					{
						progressBar.Percentage = i * 100 / shpToCreate.Count();
						statusLabel.GetText = () => "Converting...";
						Utility.Command.ConvertPngToShp(shpToCreate[i]);
						File.Delete(shpToCreate[i][1]);
					}

					statusLabel.GetText = () => "Building tilesets...";
					int c = 0;
					string[] TilesetArray = new string[] { "BASE", "BAT", "BGBS", "ICE", "TREE", "WAST" };
					foreach (string set in TilesetArray)
					{
						progressBar.Percentage = c * 100 / TilesetArray.Count();
						File.Delete(Path.Combine(pathToTilesets, "{0}.tsx".F(set)));
						File.Copy("mods/d2k/tilesets/{0}.tsx".F(set), Path.Combine(pathToTilesets, "{0}.tsx".F(set)));

						// TODO: this is ugly: a GUI will open and close immediately after some delay
						Process p = new Process();
						ProcessStartInfo TilesetBuilderProcessStartInfo = new ProcessStartInfo("OpenRA.TilesetBuilder.exe", Path.Combine(pathToTilesets, "{0}.png".F(set)) + " 32 --export Content/d2k/Tilesets");
						p.StartInfo = TilesetBuilderProcessStartInfo;
						p.Start();
						p.WaitForExit();
						File.Delete(Path.Combine(pathToTilesets, "{0}.tsx".F(set)));
						File.Delete(Path.Combine(pathToTilesets, "{0}.png".F(set)));
						File.Delete(Path.Combine(pathToTilesets, "{0}.yaml".F(set.ToLower())));
						File.Delete(Path.Combine(pathToTilesets, "{0}.pal".F(set.ToLower())));
						c++;
					}

					Game.RunAfterTick(() =>
					{
						progressBar.Percentage = 100;
						statusLabel.GetText = () => "Extraction and conversion complete.";
						backButton.IsDisabled = () => false;
						continueLoading();
					});
				}
				catch
				{
					onError("Installation failed");
				}
			}) { IsBackground = true };
			t.Start();
		}
	}
}
