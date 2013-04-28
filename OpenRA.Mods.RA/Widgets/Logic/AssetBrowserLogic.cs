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
		static ButtonWidget playButton;
		static ButtonWidget pauseButton;
		static ScrollPanelWidget assetList;
		static ScrollItemWidget template;

		public enum SourceType { Folders, Packages }
		public static SourceType AssetSource = SourceType.Folders;

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
			filenameInput.Text = spriteImage.Image;
			filenameInput.OnEnterKey = () => LoadAsset(filenameInput.Text);

			frameSlider = panel.Get<SliderWidget>("FRAME_SLIDER");
			frameSlider.MaximumValue = (float)spriteImage.FrameCount;
			frameSlider.Ticks = spriteImage.FrameCount+1;
			frameSlider.OnChange += x => { spriteImage.Frame = (int)Math.Round(x); };
			frameSlider.GetValue = () => spriteImage.Frame;

			panel.Get<LabelWidget>("FRAME_COUNT").GetText = () => spriteImage.Frame.ToString();

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

			panel.Get<ButtonWidget>("EXPORT_BUTTON").OnClick = () =>
			{
				var palette = (WidgetUtils.ActiveModId() == "d2k") ? "d2k.pal" : "egopal.pal";

				var ExtractGameFiles = new string[][]
				{
					new string[] {"--extract", WidgetUtils.ActiveModId(), palette},
					new string[] {"--extract", WidgetUtils.ActiveModId(), "{0}.shp".F(spriteImage.Image)},
				};
				
				var ExportToPng = new string[][]
				{
					new string[] {"--png", "{0}.shp".F(spriteImage.Image), palette},
				};

				var args = new WidgetArgs()
				{
					{ "ExtractGameFiles", ExtractGameFiles },
					{ "ExportToPng", ExportToPng }
				};

				Ui.OpenWindow("EXTRACT_ASSETS_PANEL", args);
			};

			panel.Get<ButtonWidget>("CLOSE_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		static void AddAsset(ScrollPanelWidget list, string filepath, ScrollItemWidget template)
		{
			var sprite = Path.GetFileNameWithoutExtension(filepath);

			var item = ScrollItemWidget.Setup(template,
			                                  () => spriteImage != null && spriteImage.Image == sprite,
			                                  () => LoadAsset(sprite));
			item.Get<LabelWidget>("TITLE").GetText = () => sprite;

			list.AddChild(item);
		}

		static bool LoadAsset(string filename)
		{
			if (filename == null)
				return false;

			filenameInput.Text = filename;
			spriteImage.Frame = 0;
			spriteImage.Image = filename;
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

			if (AssetSource == SourceType.Folders)
			{
				foreach (var folder in FileSystem.FolderPaths)
				{
					if (Directory.Exists(folder))
					{
						var shps = Directory.GetFiles(folder, "*.shp");
						foreach (var shp in shps)
							AddAsset(assetList, shp, template);
					}
				}
			}

			if (AssetSource == SourceType.Packages)
				foreach (var hiddenFile in Rules.PackageContents.Keys)
					AddAsset(assetList, hiddenFile, template);
		}
	}
}
