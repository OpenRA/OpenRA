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
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class AssetBrowserLogic
	{
		Widget panel;

		ShpImageWidget spriteWidget;
		TextFieldWidget filenameInput;
		SliderWidget frameSlider;
		ButtonWidget playButton, pauseButton;
		ScrollPanelWidget assetList;
		ScrollItemWidget template;

		IFolder assetSource = null;
		List<string> availableShps = new List<string>();

		PaletteFromFile currentPalette;

		static readonly string[] AllowedExtensions = { ".shp", ".r8", ".tem", ".des", ".sno", ".int" };

		[ObjectCreator.UseCtor]
		public AssetBrowserLogic(Widget widget, Action onExit, World world)
		{
			panel = widget;

			var sourceDropdown = panel.Get<DropDownButtonWidget>("SOURCE_SELECTOR");
			sourceDropdown.OnMouseDown = _ => ShowSourceDropdown(sourceDropdown);
			sourceDropdown.GetText = () =>
			{
				var name = assetSource != null ? assetSource.Name.Replace(Platform.SupportDir, "^") : "All Packages";
		
				if (name.Length > 15)
					name = "..." + name.Substring(name.Length - 15);

				return name;
			};

			assetSource = FileSystem.MountedFolders.First();

			spriteWidget = panel.Get<ShpImageWidget>("SPRITE");

			currentPalette = world.WorldActor.TraitsImplementing<PaletteFromFile>().First(p => p.Name == spriteWidget.Palette);

			var paletteDropDown = panel.Get<DropDownButtonWidget>("PALETTE_SELECTOR");
			paletteDropDown.OnMouseDown = _ => ShowPaletteDropdown(paletteDropDown, world);
			paletteDropDown.GetText = () => currentPalette.Name;

			var colorPreview = panel.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = Game.Settings.Player.Color;

			var color = panel.Get<DropDownButtonWidget>("COLOR");
			color.IsDisabled = () => currentPalette.Name != colorPreview.Palette;
			color.OnMouseDown = _ => ShowColorDropDown(color, colorPreview, world);
			var block = panel.Get<ColorBlockWidget>("COLORBLOCK");
			block.GetColor = () => Game.Settings.Player.Color.RGB;

			filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
			filenameInput.OnEnterKey = () => LoadAsset(filenameInput.Text);

			frameSlider = panel.Get<SliderWidget>("FRAME_SLIDER");
			frameSlider.MaximumValue = (float)spriteWidget.FrameCount;
			frameSlider.Ticks = spriteWidget.FrameCount + 1;
			frameSlider.IsVisible = () => spriteWidget.FrameCount > 0;
			frameSlider.OnChange += x => { spriteWidget.Frame = (int)Math.Round(x); };
			frameSlider.GetValue = () => spriteWidget.Frame;

			panel.Get<LabelWidget>("FRAME_COUNT").GetText = () => "{0} / {1}".F(spriteWidget.Frame + 1, spriteWidget.FrameCount + 1);

			playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = () =>
			{
				spriteWidget.LoopAnimation = true;
				playButton.Visible = false;
				pauseButton.Visible = true;
			};
			pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = () =>
			{
				spriteWidget.LoopAnimation = false;
				playButton.Visible = true;
				pauseButton.Visible = false;
			};

			panel.Get<ButtonWidget>("BUTTON_STOP").OnClick = () =>
			{
				spriteWidget.LoopAnimation = false;
				frameSlider.Value = 0;
				spriteWidget.Frame = 0;
				playButton.Visible = true;
				pauseButton.Visible = false;
			};

			panel.Get<ButtonWidget>("BUTTON_NEXT").OnClick = () => { spriteWidget.RenderNextFrame(); };
			panel.Get<ButtonWidget>("BUTTON_PREV").OnClick = () => { spriteWidget.RenderPreviousFrame(); };

			panel.Get<ButtonWidget>("LOAD_BUTTON").OnClick = () =>
			{
				LoadAsset(filenameInput.Text);
			};

			assetList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
			template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");
			PopulateAssetList();

			panel.Get<ButtonWidget>("CLOSE_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		void AddAsset(ScrollPanelWidget list, string filepath, ScrollItemWidget template)
		{
			var filename = Path.GetFileName(filepath);
			var item = ScrollItemWidget.Setup(template,
				() => spriteWidget.Image == filename,
				() => { filenameInput.Text = filename; LoadAsset(filename); });
			item.Get<LabelWidget>("TITLE").GetText = () => filepath;

			list.AddChild(item);
		}

		bool LoadAsset(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return false;

			if (!FileSystem.Exists(filename))
				return false;

			spriteWidget.Frame = 0;
			spriteWidget.Image = filename;
			frameSlider.MaximumValue = (float)spriteWidget.FrameCount;
			frameSlider.Ticks = spriteWidget.FrameCount + 1;
			return true;
		}

		bool ShowSourceDropdown(DropDownButtonWidget dropdown)
		{
			Func<IFolder, ScrollItemWidget, ScrollItemWidget> setupItem = (source, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => assetSource == source,
				                                  () => { assetSource = source;	PopulateAssetList(); });
				item.Get<LabelWidget>("LABEL").GetText = () => source != null ? source.Name.Replace(Platform.SupportDir, "^") : "All Packages";
				return item;
			};

			// TODO: Re-enable "All Packages" once list generation is done in a background thread
			// var sources = new[] { (IFolder)null }.Concat(FileSystem.MountedFolders);

			var sources = FileSystem.MountedFolders;
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, sources, setupItem);
			return true;
		}

		void PopulateAssetList()
		{
			assetList.RemoveChildren();
			availableShps.Clear();

			// TODO: This is too slow to run in the main thread
			// var files = AssetSource != null ? AssetSource.AllFileNames() :
			// FileSystem.MountedFolders.SelectMany(f => f.AllFileNames());

			if (assetSource == null)
				return;

			var files = assetSource.AllFileNames();
			foreach (var file in files)
			{
				if (AllowedExtensions.Any(ext => file.EndsWith(ext, true, CultureInfo.InvariantCulture)))
				{
					AddAsset(assetList, file, template);
					availableShps.Add(file);
				}
			}
		}
		
		bool ShowPaletteDropdown(DropDownButtonWidget dropdown, World world)
		{
			Func<PaletteFromFile, ScrollItemWidget, ScrollItemWidget> setupItem = (palette, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => currentPalette.Name == palette.Name,
					() => { currentPalette = palette; spriteWidget.Palette = currentPalette.Name; });
				item.Get<LabelWidget>("LABEL").GetText = () => palette.Name;
				return item;
			};

			var palettes = world.WorldActor.TraitsImplementing<PaletteFromFile>();
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, palettes, setupItem);
			return true;
		}

		void ShowColorDropDown(DropDownButtonWidget color, ColorPreviewManagerWidget preview, World world)
		{
			Action onExit = () =>
			{
				Game.Settings.Player.Color = preview.Color;
				Game.Settings.Save();
			};

			color.RemovePanel();

			Action<HSLColor> onChange = c => preview.Color = c;

			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onChange", onChange },
				{ "initialColor", Game.Settings.Player.Color }
			});

			color.AttachPanel(colorChooser, onExit);
		}
	}
}
