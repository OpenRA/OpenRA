#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		IModel currentVoxel;
		VqaPlayerWidget player = null;
		bool isVideoLoaded = false;
		bool isLoadError = false;
		int currentFrame;
		WRot modelOrientation;

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
				var sourceName = new CachedTransform<IReadOnlyPackage, string>(GetSourceDisplayName);
				sourceDropdown.GetText = () => sourceName.Update(assetSource);
			}

			var spriteWidget = panel.GetOrNull<SpriteWidget>("SPRITE");
			if (spriteWidget != null)
			{
				spriteWidget.GetSprite = () => currentSprites != null ? currentSprites[currentFrame] : null;
				currentPalette = spriteWidget.Palette;
				spriteWidget.GetPalette = () => currentPalette;
				spriteWidget.IsVisible = () => !isVideoLoaded && !isLoadError && currentSprites != null;
			}

			var playerWidget = panel.GetOrNull<VqaPlayerWidget>("PLAYER");
			if (playerWidget != null)
				playerWidget.IsVisible = () => isVideoLoaded && !isLoadError;

			var modelWidget = panel.GetOrNull<ModelWidget>("VOXEL");
			if (modelWidget != null)
			{
				modelWidget.GetVoxel = () => currentVoxel;
				currentPalette = modelWidget.Palette;
				modelWidget.GetPalette = () => currentPalette;
				modelWidget.GetPlayerPalette = () => currentPalette;
				modelWidget.GetRotation = () => modelOrientation;
				modelWidget.IsVisible = () => !isVideoLoaded && !isLoadError && currentVoxel != null;
			}

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

			frameSlider = panel.GetOrNull<SliderWidget>("FRAME_SLIDER");
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
						if (frameSlider != null)
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

			var voxelContainer = panel.GetOrNull("VOXEL_SELECTOR");
			if (voxelContainer != null)
				voxelContainer.IsVisible = () => currentVoxel != null;

			var rollSlider = panel.GetOrNull<SliderWidget>("ROLL_SLIDER");
			if (rollSlider != null)
			{
				rollSlider.OnChange += x =>
				{
					var roll = (int)x;
					modelOrientation = modelOrientation.WithRoll(new WAngle(roll));
				};

				rollSlider.GetValue = () => modelOrientation.Roll.Angle;
			}

			var pitchSlider = panel.GetOrNull<SliderWidget>("PITCH_SLIDER");
			if (pitchSlider != null)
			{
				pitchSlider.OnChange += x =>
				{
					var pitch = (int)x;
					modelOrientation = modelOrientation.WithPitch(new WAngle(pitch));
				};

				pitchSlider.GetValue = () => modelOrientation.Pitch.Angle;
			}

			var yawSlider = panel.GetOrNull<SliderWidget>("YAW_SLIDER");
			if (yawSlider != null)
			{
				yawSlider.OnChange += x =>
				{
					var yaw = (int)x;
					modelOrientation = modelOrientation.WithYaw(new WAngle(yaw));
				};

				yawSlider.GetValue = () => modelOrientation.Yaw.Angle;
			}

			var assetBrowserModData = modData.Manifest.Get<AssetBrowser>();
			allowedExtensions = assetBrowserModData.SupportedExtensions;

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

			if (firstVisible.Key != null && modData.DefaultFileSystem.TryGetPackageContaining(firstVisible.Key, out var package, out var filename))
				LoadAsset(package, filename);
		}

		void AddAsset(ScrollPanelWidget list, string filepath, IReadOnlyPackage package, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template,
				() => currentFilename == filepath && currentPackage == package,
				() => { LoadAsset(package, filepath); });

			var label = item.Get<LabelWithTooltipWidget>("TITLE");
			WidgetUtils.TruncateLabelToTooltip(label, filepath);

			item.IsVisible = () =>
			{
				if (assetVisByName.TryGetValue(filepath, out var visible))
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

					if (frameSlider != null)
					{
						frameSlider.MaximumValue = (float)player.Video.Frames - 1;
						frameSlider.Ticks = 0;
					}

					return true;
				}

				if (Path.GetExtension(filename.ToLowerInvariant()) == ".vxl")
				{
					var voxelName = Path.GetFileNameWithoutExtension(filename);
					currentVoxel = world.ModelCache.GetModel(voxelName);
					currentSprites = null;
				}
				else
				{
					currentSprites = world.Map.Rules.Sequences.SpriteCache[prefix + filename];
					currentFrame = 0;

					if (frameSlider != null)
					{
						frameSlider.MaximumValue = (float)currentSprites.Length - 1;
						frameSlider.Ticks = currentSprites.Length;
					}

					currentVoxel = null;
				}
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
			var sourceName = new CachedTransform<IReadOnlyPackage, string>(GetSourceDisplayName);
			Func<IReadOnlyPackage, ScrollItemWidget, ScrollItemWidget> setupItem = (source, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => assetSource == source,
					() => { assetSource = source; PopulateAssetList(); });

				item.Get<LabelWidget>("LABEL").GetText = () => sourceName.Update(source);
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

		string GetSourceDisplayName(IReadOnlyPackage source)
		{
			if (source == null)
				return "All Packages";

			// Packages that are explicitly mounted in the filesystem use their explicit mount name
			var fs = (OpenRA.FileSystem.FileSystem)modData.DefaultFileSystem;
			var name = fs.GetPrefix(source);

			// Fall back to the path relative to the mod, engine, or support dir
			if (name == null)
			{
				name = source.Name;
				var compare = Platform.CurrentPlatform == PlatformType.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				if (name.StartsWith(modData.Manifest.Package.Name, compare))
					name = "$" + modData.Manifest.Id + "/" + name.Substring(modData.Manifest.Package.Name.Length + 1);
				else if (name.StartsWith(Platform.EngineDir, compare))
					name = "./" + name.Substring(Platform.EngineDir.Length);
				else if (name.StartsWith(Platform.SupportDir, compare))
					name = "^" + name.Substring(Platform.SupportDir.Length);
			}

			if (name.Length > 18)
				name = "..." + name.Substring(name.Length - 15);

			return name;
		}
	}
}
