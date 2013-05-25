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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class AssetBrowserLogic
	{
		Widget panel;

		ShpImageWidget spriteImage;
		TextFieldWidget filenameInput;
		SliderWidget frameSlider;
		ButtonWidget playButton, pauseButton;
		ScrollPanelWidget assetList;
		ScrollItemWidget template;

		IFolder AssetSource = null;
		List<string> AvailableShps = new List<string>();

		[ObjectCreator.UseCtor]
		public AssetBrowserLogic(Widget widget, Action onExit, World world)
		{
			panel = widget;

			var sourceDropdown = panel.Get<DropDownButtonWidget>("SOURCE_SELECTOR");
			sourceDropdown.OnMouseDown = _ => ShowSourceDropdown(sourceDropdown);
			sourceDropdown.GetText = () =>
			{
				var name = AssetSource != null ? AssetSource.Name : "All Packages";
				if (name.Length > 15)
					name = "..."+name.Substring(name.Length - 15);

				return name;
			};

			AssetSource = FileSystem.MountedFolders.First();

			spriteImage = panel.Get<ShpImageWidget>("SPRITE");

			filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
			filenameInput.Text = spriteImage.Image+".shp";
			filenameInput.OnEnterKey = () => LoadAsset(filenameInput.Text);

			frameSlider = panel.Get<SliderWidget>("FRAME_SLIDER");
			frameSlider.MaximumValue = (float)spriteImage.FrameCount;
			frameSlider.Ticks = spriteImage.FrameCount+1;
			frameSlider.OnChange += x => { spriteImage.Frame = (int)Math.Round(x); };
			frameSlider.GetValue = () => spriteImage.Frame;

			panel.Get<LabelWidget>("FRAME_COUNT").GetText = () => "{0}/{1}".F(spriteImage.Frame, spriteImage.FrameCount);

			playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = () =>
			{
				spriteImage.LoopAnimation = true;
				playButton.Visible = false;
				pauseButton.Visible = true;
			};
			pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = () =>
			{
				spriteImage.LoopAnimation = false;
				playButton.Visible = true;
				pauseButton.Visible = false;
			};

			panel.Get<ButtonWidget>("BUTTON_STOP").OnClick = () =>
			{
				spriteImage.LoopAnimation = false;
				frameSlider.Value = 0;
				spriteImage.Frame = 0;
				playButton.Visible = true;
				pauseButton.Visible = false;
			};

			panel.Get<ButtonWidget>("BUTTON_NEXT").OnClick = () => { spriteImage.RenderNextFrame(); };
			panel.Get<ButtonWidget>("BUTTON_PREV").OnClick = () => { spriteImage.RenderPreviousFrame(); };

			panel.Get<ButtonWidget>("LOAD_BUTTON").OnClick = () =>
			{
				LoadAsset(filenameInput.Text);
			};

			assetList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
			template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");
			PopulateAssetList();

			var palette = (WidgetUtils.ActiveModId() == "d2k") ? "d2k.pal" : "egopal.pal";

			panel.Get<ButtonWidget>("EXPORT_BUTTON").OnClick = () =>
			{
				var ExtractGameFiles = new string[][]
				{
					new string[] {"--extract", WidgetUtils.ActiveModId(), palette, "--userdir"},
					new string[] {"--extract", WidgetUtils.ActiveModId(), "{0}.shp".F(spriteImage.Image), "--userdir"},
				};
				
				var ExportToPng = new string[][]
				{
					new string[] {"--png", Platform.SupportDir+"{0}.shp".F(spriteImage.Image), Platform.SupportDir+palette},
				};

				var ImportFromPng = new string[][] { };

				var args = new WidgetArgs()
				{
					{ "ExtractGameFiles", ExtractGameFiles },
					{ "ExportToPng", ExportToPng },
					{ "ImportFromPng", ImportFromPng}
				};
				
				Ui.OpenWindow("CONVERT_ASSETS_PANEL", args);
			};

			panel.Get<ButtonWidget>("EXTRACT_BUTTON").OnClick = () =>
			{
				var ExtractGameFilesList = new List<string[]>();
				var ExportToPngList = new List<string[]>();

				ExtractGameFilesList.Add(new string[] { "--extract", WidgetUtils.ActiveModId(), palette, "--userdir"} );

				foreach (var shp in AvailableShps)
				{
					ExtractGameFilesList.Add(new string[] { "--extract", WidgetUtils.ActiveModId(), shp, "--userdir" } );
					ExportToPngList.Add(new string[] { "--png", Platform.SupportDir+shp, Platform.SupportDir+palette } );
					Console.WriteLine(Platform.SupportDir+shp);
				}

				var ExtractGameFiles = ExtractGameFilesList.ToArray();
				var ExportToPng = ExportToPngList.ToArray();
				var ImportFromPng = new string[][] { };

				var args = new WidgetArgs()
				{
					{ "ExtractGameFiles", ExtractGameFiles },
					{ "ExportToPng", ExportToPng },
					{ "ImportFromPng", ImportFromPng}
				};
				
				Ui.OpenWindow("CONVERT_ASSETS_PANEL", args);
			};


			panel.Get<ButtonWidget>("IMPORT_BUTTON").OnClick = () =>
			{
				var imageSizeInput = panel.Get<TextFieldWidget>("IMAGE_SIZE_INPUT");
				var imageFilename = panel.Get<TextFieldWidget>("IMAGE_FILENAME_INPUT");
				
				var ExtractGameFiles = new string[][] { };
				var ExportToPng = new string[][] { };
				var ImportFromPng = new string[][]
				{
					new string[] {"--shp", Platform.SupportDir+imageFilename.Text, imageSizeInput.Text},
				};

				var args = new WidgetArgs()
				{
					{ "ExtractGameFiles", ExtractGameFiles },
					{ "ExportToPng", ExportToPng },
					{ "ImportFromPng", ImportFromPng}
				};
				
				Ui.OpenWindow("CONVERT_ASSETS_PANEL", args);
			};

			panel.Get<ButtonWidget>("CLOSE_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		void AddAsset(ScrollPanelWidget list, string filepath, ScrollItemWidget template)
		{
			var sprite = Path.GetFileNameWithoutExtension(filepath);
			var item = ScrollItemWidget.Setup(template,
			                                  () => spriteImage.Image == sprite,
			                                  () => LoadAsset(sprite));
			item.Get<LabelWidget>("TITLE").GetText = () => filepath;

			list.AddChild(item);
		}

		bool LoadAsset(string sprite)
		{
			if (sprite == null)
				return false;

			spriteImage.Frame = 0;
			spriteImage.Image = sprite;
			frameSlider.MaximumValue = (float)spriteImage.FrameCount;
			frameSlider.Ticks = spriteImage.FrameCount+1;
			return true;
		}

		bool ShowSourceDropdown(DropDownButtonWidget dropdown)
		{
			Func<IFolder, ScrollItemWidget, ScrollItemWidget> setupItem = (source, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => AssetSource == source,
				                                  () => { AssetSource = source;	PopulateAssetList(); });
				item.Get<LabelWidget>("LABEL").GetText = () => source != null ? source.Name : "All Packages";
				return item;
			};

			// TODO: Re-enable "All Packages" once list generation is done in a background thread
			//var sources = new[] { (IFolder)null }.Concat(FileSystem.MountedFolders);

			var sources = FileSystem.MountedFolders;
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 250, sources, setupItem);
			return true;
		}

		void PopulateAssetList()
		{
			assetList.RemoveChildren();
			AvailableShps.Clear();

			// TODO: This is too slow to run in the main thread
			//var files = AssetSource != null ? AssetSource.AllFileNames() :
			//	FileSystem.MountedFolders.SelectMany(f => f.AllFileNames());

			if (AssetSource == null)
				return;

			var files = AssetSource.AllFileNames();
			foreach (var file in files)
			{
				if (file.EndsWith(".shp"))
				{
					AddAsset(assetList, file, template);
					AvailableShps.Add(file);
				}
			}
		}
	}
}
