#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AssetBrowserLogic : ChromeLogic
	{
		readonly string[] allowedExtensions;
		readonly IEnumerable<IReadOnlyPackage> acceptablePackages;

		readonly World world;
		readonly ModData modData;

		Widget panel;

		TextFieldWidget filenameInput;
		SliderWidget frameSlider;
		ScrollPanelWidget assetList;
		ScrollItemWidget template;

		IReadOnlyPackage assetSource = null;
		bool animateFrames = false;

		string currentPalette;
		string currentFilename;
		IReadOnlyPackage currentPackage;
		Sprite[] currentSprites;
		VqaPlayerWidget player = null;
		bool isVideoLoaded = false;
		bool isLoadError = false;
		int currentFrame;

		[ObjectCreator.UseCtor]
		public AssetBrowserLogic(Widget widget, Action onExit, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;
			this.modData = modData;
			panel = widget;

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
					var name = assetSource != null ? Platform.UnresolvePath(assetSource.Name) : "All Packages";
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
				spriteWidget.IsVisible = () => !isVideoLoaded && !isLoadError;
			}

			var playerWidget = panel.GetOrNull<VqaPlayerWidget>("PLAYER");
			if (playerWidget != null)
				playerWidget.IsVisible = () => isVideoLoaded && !isLoadError;

			var errorLabelWidget = panel.GetOrNull("ERROR");
			if (errorLabelWidget != null)
				errorLabelWidget.IsVisible = () => isLoadError;

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
				colorDropdown.IsDisabled = () => currentPalette != colorPreview.PaletteName;
				colorDropdown.OnMouseDown = _ => ColorPickerLogic.ShowColorDropDown(colorDropdown, colorPreview, world);
				panel.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => Game.Settings.Player.Color;
			}

			filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
			filenameInput.OnTextEdited = () => ApplyFilter();
			filenameInput.OnEscKey = filenameInput.YieldKeyboardFocus;

			var frameContainer = panel.GetOrNull("FRAME_SELECTOR");
			if (frameContainer != null)
				frameContainer.IsVisible = () => (currentSprites != null && currentSprites.Length > 1) ||
					(isVideoLoaded && player != null && player.Video != null && player.Video.Frames > 1);

			frameSlider = panel.Get<SliderWidget>("FRAME_SLIDER");
			if (frameSlider != null)
			{
				frameSlider.OnChange += x =>
				{
					if (!isVideoLoaded)
						currentFrame = (int)Math.Round(x);
				};

				frameSlider.GetValue = () => isVideoLoaded ? player.Video.CurrentFrame : currentFrame;
				frameSlider.IsDisabled = () => isVideoLoaded;
			}

			var frameText = panel.GetOrNull<LabelWidget>("FRAME_COUNT");
			if (frameText != null)
			{
				frameText.GetText = () =>
					isVideoLoaded ?
					"{0} / {1}".F(player.Video.CurrentFrame + 1, player.Video.Frames) :
					"{0} / {1}".F(currentFrame, currentSprites.Length - 1);
			}

			var playButton = panel.GetOrNull<ButtonWidget>("BUTTON_PLAY");
			if (playButton != null)
			{
				playButton.OnClick = () =>
				{
					if (isVideoLoaded)
						player.Play();
					else
						animateFrames = true;
				};

				playButton.IsVisible = () => isVideoLoaded ? player.Paused : !animateFrames;
			}

			var pauseButton = panel.GetOrNull<ButtonWidget>("BUTTON_PAUSE");
			if (pauseButton != null)
			{
				pauseButton.OnClick = () =>
				{
					if (isVideoLoaded)
						player.Pause();
					else
						animateFrames = false;
				};

				pauseButton.IsVisible = () => isVideoLoaded ? !player.Paused : animateFrames;
			}

			var stopButton = panel.GetOrNull<ButtonWidget>("BUTTON_STOP");
			if (stopButton != null)
			{
				stopButton.OnClick = () =>
				{
					if (isVideoLoaded)
						player.Stop();
					else
					{
						frameSlider.Value = 0;
						currentFrame = 0;
						animateFrames = false;
					}
				};
			}

			var nextButton = panel.GetOrNull<ButtonWidget>("BUTTON_NEXT");
			if (nextButton != null)
			{
				nextButton.OnClick = () =>
				{
					if (!isVideoLoaded)
						nextButton.OnClick = SelectNextFrame;
				};

				nextButton.IsVisible = () => !isVideoLoaded;
			}

			var prevButton = panel.GetOrNull<ButtonWidget>("BUTTON_PREV");
			if (prevButton != null)
			{
				prevButton.OnClick = () =>
				{
					if (!isVideoLoaded)
						SelectPreviousFrame();
				};

				prevButton.IsVisible = () => !isVideoLoaded;
			}

			if (logicArgs.ContainsKey("SupportedFormats"))
				allowedExtensions = FieldLoader.GetValue<string[]>("SupportedFormats", logicArgs["SupportedFormats"].Value);
			else
				allowedExtensions = new string[0];

			acceptablePackages = modData.ModFiles.MountedPackages.Where(p =>
				p.Contents.Any(c => allowedExtensions.Contains(Path.GetExtension(c).ToLowerInvariant())));

			assetList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
			template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");
			PopulateAssetList();

			var closeButton = panel.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			if (closeButton != null)
				closeButton.OnClick = () =>
				{
					if (isVideoLoaded)
						player.Stop();
					Ui.CloseWindow();
					onExit();
				};
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

		Dictionary<string, bool> assetVisByName = new Dictionary<string, bool>();

		bool FilterAsset(string filename)
		{
			var filter = filenameInput.Text;

			if (string.IsNullOrWhiteSpace(filter))
				return true;

			if (filename.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				return true;

			return false;
		}

		void ApplyFilter()
		{
			assetVisByName.Clear();
			assetList.Layout.AdjustChildren();
			assetList.ScrollToTop();

			// Select the first visible
			var firstVisible = assetVisByName.FirstOrDefault(kvp => kvp.Value);
			IReadOnlyPackage package;
			string filename;

			if (firstVisible.Key != null && modData.DefaultFileSystem.TryGetPackageContaining(firstVisible.Key, out package, out filename))
				LoadAsset(package, filename);
		}

		void AddAsset(ScrollPanelWidget list, string filepath, IReadOnlyPackage package, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template,
				() => currentFilename == filepath && currentPackage == package,
				() => { LoadAsset(package, filepath); });

			item.Get<LabelWidget>("TITLE").GetText = () => filepath;
			item.IsVisible = () =>
			{
				bool visible;
				if (assetVisByName.TryGetValue(filepath, out visible))
					return visible;

				visible = FilterAsset(filepath);
				assetVisByName.Add(filepath, visible);
				return visible;
			};

			list.AddChild(item);
		}

		bool LoadAsset(IReadOnlyPackage package, string filename)
		{
			if (isVideoLoaded)
			{
				player.Stop();
				player = null;
				isVideoLoaded = false;
			}

			if (string.IsNullOrEmpty(filename))
				return false;

			if (!package.Contains(filename))
				return false;

			isLoadError = false;

			try
			{
				currentPackage = package;
				currentFilename = filename;
				var prefix = "";
				var fs = modData.DefaultFileSystem as OpenRA.FileSystem.FileSystem;

				if (fs != null)
				{
					prefix = fs.GetPrefix(package);
					if (prefix != null)
						prefix += "|";
				}

				if (Path.GetExtension(filename.ToLowerInvariant()) == ".vqa")
				{
					player = panel.Get<VqaPlayerWidget>("PLAYER");
					player.Load(prefix + filename);
					player.DrawOverlay = false;
					isVideoLoaded = true;
					frameSlider.MaximumValue = (float)player.Video.Frames - 1;
					frameSlider.Ticks = 0;
					return true;
				}

				currentSprites = world.Map.Rules.Sequences.SpriteCache[prefix + filename];
				currentFrame = 0;
				frameSlider.MaximumValue = (float)currentSprites.Length - 1;
				frameSlider.Ticks = currentSprites.Length;
			}
			catch (Exception ex)
			{
				isLoadError = true;
				Log.AddChannel("assetbrowser", "assetbrowser.log");
				Log.Write("assetbrowser", "Error reading {0}:{3} {1}{3}{2}", filename, ex.Message, ex.StackTrace, Environment.NewLine);

				return false;
			}

			return true;
		}

		bool ShowSourceDropdown(DropDownButtonWidget dropdown)
		{
			Func<IReadOnlyPackage, ScrollItemWidget, ScrollItemWidget> setupItem = (source, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => assetSource == source,
					() => { assetSource = source; PopulateAssetList(); });
				item.Get<LabelWidget>("LABEL").GetText = () => source != null ? Platform.UnresolvePath(source.Name) : "All Packages";
				return item;
			};

			var sources = new[] { (IReadOnlyPackage)null }.Concat(acceptablePackages);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, sources, setupItem);
			return true;
		}

		void PopulateAssetList()
		{
			assetList.RemoveChildren();

			var files = new SortedList<string, List<IReadOnlyPackage>>();

			if (assetSource != null)
				foreach (var content in assetSource.Contents)
					files.Add(content, new List<IReadOnlyPackage> { assetSource });
			else
			{
				foreach (var mountedPackage in modData.ModFiles.MountedPackages)
				{
					foreach (var content in mountedPackage.Contents)
					{
						if (!files.ContainsKey(content))
							files.Add(content, new List<IReadOnlyPackage> { mountedPackage });
						else
							files[content].Add(mountedPackage);
					}
				}
			}

			foreach (var file in files.OrderBy(s => s.Key))
			{
				if (!allowedExtensions.Any(ext => file.Key.EndsWith(ext, true, CultureInfo.InvariantCulture)))
					continue;

				foreach (var package in file.Value)
					AddAsset(assetList, file.Key, package, template);
			}
		}

		bool ShowPaletteDropdown(DropDownButtonWidget dropdown, World world)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (name, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => currentPalette == name,
					() => currentPalette = name);
				item.Get<LabelWidget>("LABEL").GetText = () => name;

				return item;
			};

			var palettes = world.WorldActor.TraitsImplementing<IProvidesAssetBrowserPalettes>()
				.SelectMany(p => p.PaletteNames);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, palettes, setupItem);
			return true;
		}
	}
}
