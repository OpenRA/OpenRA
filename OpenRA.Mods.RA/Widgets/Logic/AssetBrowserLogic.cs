#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class AssetBrowserLogic
	{
		Widget panel;

		TextFieldWidget filenameInput;
		SliderWidget frameSlider;
		ScrollPanelWidget assetList;
		ScrollItemWidget template;

		IFolder assetSource = null;
		List<string> availableShps = new List<string>();
		bool animateFrames = false;

		string currentPalette;
		string currentFilename;
		Sprite[] currentSprites;
		int currentFrame;

		readonly World world;

		static readonly string[] AllowedExtensions = { ".shp", ".r8", "tmp", ".tem", ".des", ".sno", ".int" };

		[ObjectCreator.UseCtor]
		public AssetBrowserLogic(Widget widget, Action onExit, World world)
		{
			this.world = world;

			panel = widget;
			assetSource = GlobalFileSystem.MountedFolders.First();

			var ticker = panel.GetOrNull<LogicTickerWidget>("ANIMATION_TICKER");
			if (ticker != null)
			{
				ticker.OnTick = () =>
				{
					if (animateFrames)
						SelectNextFrame();
				};
			}

			var sourceDropdown = panel.GetOrNull<DropDownButtonWidget>("SOURCE_SELECTOR");
			if (sourceDropdown != null)
			{
				sourceDropdown.OnMouseDown = _ => ShowSourceDropdown(sourceDropdown);
				sourceDropdown.GetText = () =>
				{
					var name = assetSource != null ? assetSource.Name.Replace(Platform.SupportDir, "^") : "All Packages";
					if (name.Length > 15)
						name = "..." + name.Substring(name.Length - 15);

					return name;
				};
			}

			var spriteWidget = panel.GetOrNull<SpriteWidget>("SPRITE");
			if (spriteWidget != null)
			{
				spriteWidget.GetSprite = () => currentSprites != null ? currentSprites[currentFrame] : null;
				currentPalette = spriteWidget.Palette;
				spriteWidget.GetPalette = () => currentPalette;
			}

			var paletteDropDown = panel.GetOrNull<DropDownButtonWidget>("PALETTE_SELECTOR");
			if (paletteDropDown != null)
			{
				paletteDropDown.OnMouseDown = _ => ShowPaletteDropdown(paletteDropDown, world);
				paletteDropDown.GetText = () => currentPalette;
			}

			var colorPreview = panel.GetOrNull<ColorPreviewManagerWidget>("COLOR_MANAGER");
			if (colorPreview != null)
				colorPreview.Color = Game.Settings.Player.Color;

			var colorDropdown = panel.GetOrNull<DropDownButtonWidget>("COLOR");
			if (colorDropdown != null)
			{
				colorDropdown.IsDisabled = () => currentPalette != colorPreview.Palette;
				colorDropdown.OnMouseDown = _ => ShowColorDropDown(colorDropdown, colorPreview, world);
				panel.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => Game.Settings.Player.Color.RGB;
			}

			filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
			filenameInput.OnEnterKey = () => LoadAsset(filenameInput.Text);

			var frameContainer = panel.GetOrNull("FRAME_SELECTOR");
			if (frameContainer != null)
				frameContainer.IsVisible = () => currentSprites != null && currentSprites.Length > 1;

			frameSlider = panel.Get<SliderWidget>("FRAME_SLIDER");
			frameSlider.OnChange += x => { currentFrame = (int)Math.Round(x); };
			frameSlider.GetValue = () => currentFrame;

			var frameText = panel.GetOrNull<LabelWidget>("FRAME_COUNT");
			if (frameText != null)
				frameText.GetText = () => "{0} / {1}".F(currentFrame + 1, currentSprites.Length);

			var playButton = panel.GetOrNull<ButtonWidget>("BUTTON_PLAY");
			if (playButton != null)
			{
				playButton.OnClick = () => animateFrames = true;
				playButton.IsVisible = () => !animateFrames;
			}

			var pauseButton = panel.GetOrNull<ButtonWidget>("BUTTON_PAUSE");
			if (pauseButton != null)
			{
				pauseButton.OnClick = () => animateFrames = false;
				pauseButton.IsVisible = () => animateFrames;
			}

			var stopButton = panel.GetOrNull<ButtonWidget>("BUTTON_STOP");
			if (stopButton != null)
			{
				stopButton.OnClick = () =>
				{
					frameSlider.Value = 0;
					currentFrame = 0;
					animateFrames = false;
				};
			}

			var nextButton = panel.GetOrNull<ButtonWidget>("BUTTON_NEXT");
			if (nextButton != null)
				nextButton.OnClick = SelectNextFrame;

			var prevButton = panel.GetOrNull<ButtonWidget>("BUTTON_PREV");
			if (prevButton != null)
				prevButton.OnClick = SelectPreviousFrame;

			var loadButton = panel.GetOrNull<ButtonWidget>("LOAD_BUTTON");
			if (loadButton != null)
				loadButton.OnClick = () => LoadAsset(filenameInput.Text);

			assetList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
			template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");
			PopulateAssetList();

			var closeButton = panel.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			if (closeButton != null)
				closeButton.OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		void SelectNextFrame()
		{
			currentFrame++;
			if (currentFrame >= currentSprites.Length)
				currentFrame = 0;
		}

		void SelectPreviousFrame()
		{
			currentFrame--;
			if (currentFrame < 0)
				currentFrame = currentSprites.Length - 1;
		}

		void AddAsset(ScrollPanelWidget list, string filepath, ScrollItemWidget template)
		{
			var filename = Path.GetFileName(filepath);
			var item = ScrollItemWidget.Setup(template,
				() => currentFilename == filename,
				() => { filenameInput.Text = filename; LoadAsset(filename); });
			item.Get<LabelWidget>("TITLE").GetText = () => filepath;

			list.AddChild(item);
		}

		bool LoadAsset(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return false;

			if (!GlobalFileSystem.Exists(filename))
				return false;

			currentFilename = filename;
			currentSprites = world.Map.Rules.TileSets[world.Map.Tileset].Data.SpriteLoader.LoadAllSprites(filename);
			currentFrame = 0;
			frameSlider.MaximumValue = (float)currentSprites.Length - 1;
			frameSlider.Ticks = currentSprites.Length;

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
			// var sources = new[] { (IFolder)null }.Concat(GlobalFileSystem.MountedFolders);

			var sources = GlobalFileSystem.MountedFolders;
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, sources, setupItem);
			return true;
		}

		void PopulateAssetList()
		{
			assetList.RemoveChildren();
			availableShps.Clear();

			// TODO: This is too slow to run in the main thread
			// var files = AssetSource != null ? AssetSource.AllFileNames() :
			// GlobalFileSystem.MountedFolders.SelectMany(f => f.AllFileNames());

			if (assetSource == null)
				return;

			var files = assetSource.AllFileNames().OrderBy(s => s);
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
				var name = palette.Name;
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => currentPalette == name,
					() => currentPalette = name);
				item.Get<LabelWidget>("LABEL").GetText = () => name;

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
