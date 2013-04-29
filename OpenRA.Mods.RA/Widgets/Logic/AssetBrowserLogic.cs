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

		static ShpImageWidget spriteImage;
		static TextFieldWidget filenameInput;
		static SliderWidget frameSlider;
		static ButtonWidget playButton, pauseButton;
		static ScrollPanelWidget assetList;
		static ScrollItemWidget template;

		public enum SourceType { Folders, Packages }
		public static SourceType AssetSource = SourceType.Folders;

		public static List<string> AvailableShps = new List<string>();

		[ObjectCreator.UseCtor]
		public AssetBrowserLogic(Widget widget, Action onExit, World world)
		{
			panel = widget;

			var sourceDropdown = panel.Get<DropDownButtonWidget>("SOURCE_SELECTOR");
			sourceDropdown.OnMouseDown = _ => ShowSourceDropdown(sourceDropdown);
			sourceDropdown.GetText = () => AssetSource == SourceType.Folders ? "Folders"
				: AssetSource == SourceType.Packages ? "Packages" : "None";
			sourceDropdown.Disabled = !Rules.PackageContents.Keys.Any();

			spriteImage = panel.Get<ShpImageWidget>("SPRITE");

			filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
			filenameInput.Text = spriteImage.Image+".shp";
			filenameInput.OnEnterKey = () => LoadAsset(Path.GetFileNameWithoutExtension(filenameInput.Text));

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
				LoadAsset(Path.GetFileNameWithoutExtension(filenameInput.Text));
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

		static void AddAsset(ScrollPanelWidget list, string filepath, ScrollItemWidget template)
		{
			var sprite = Path.GetFileNameWithoutExtension(filepath);
			var filename = Path.GetFileName(filepath);

			var item = ScrollItemWidget.Setup(template,
			                                  () => spriteImage != null && spriteImage.Image == sprite,
			                                  () => LoadAsset(sprite));
			item.Get<LabelWidget>("TITLE").GetText = () => filename;

			list.AddChild(item);
		}

		static bool LoadAsset(string sprite)
		{
			if (sprite == null)
				return false;

			filenameInput.Text = sprite+".shp";
			spriteImage.Frame = 0;
			spriteImage.Image = sprite;
			frameSlider.MaximumValue = (float)spriteImage.FrameCount;
			frameSlider.Ticks = spriteImage.FrameCount+1;
			return true;
		}

		public static bool ShowSourceDropdown(DropDownButtonWidget dropdown)
		{
			var options = new Dictionary<string, SourceType>()
			{
				{ "Folders", SourceType.Folders },
				{ "Packages", SourceType.Packages },
			};
			
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => AssetSource == options[o],
				                                  () => { AssetSource = options[o];	PopulateAssetList(); });
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}

		public static void PopulateAssetList()
		{
			assetList.RemoveChildren();
			AvailableShps.Clear();

			if (AssetSource == SourceType.Folders)
			{
				foreach (var folder in FileSystem.FolderPaths)
				{
					if (Directory.Exists(folder))
					{
						var shps = Directory.GetFiles(folder, "*.shp");
						foreach (var shp in shps)
						{
							AddAsset(assetList, shp, template);
							AvailableShps.Add(Path.GetFileName(shp));
						}
					}
				}
			}

			if (AssetSource == SourceType.Packages)
				foreach (var hiddenFile in Rules.PackageContents.Keys)
				{
					AddAsset(assetList, hiddenFile, template);
					AvailableShps.Add(hiddenFile);
				}
		}
	}
}
