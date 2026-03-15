#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ColorPickerLogic : ChromeLogic
	{
		static bool paletteTabOpenedLast;
		int paletteTabHighlighted = 0;

		// All color swatches in the palette for keyboard navigation
		readonly List<ColorBlockWidget> allSwatches = [];
		readonly int paletteCols;

		// Callback to update the preview when navigating swatches with arrow keys
		readonly Action<Color> onColorChange;

		[ObjectCreator.UseCtor]
		public ColorPickerLogic(Widget widget, ModData modData, World world, Color initialColor, Action<Color> onChange, Action<Widget> extraLogic,
			Action onConfirm, Dictionary<string, MiniYaml> logicArgs)
		{
			onColorChange = onChange;
			var mixer = widget.Get<ColorMixerWidget>("MIXER");
			var hueSlider = widget.Get<HueSliderWidget>("HUE_SLIDER");

			// Set the initial state
			// All users need to use the same TraitInfo instance, chosen as the default mod rules
			var colorManager = modData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();

			var (sMin, sMax) = colorManager.SaturationRange;
			var (vMin, vMax) = colorManager.ValueRange;
			mixer.SetColorLimits(sMin, sMax, vMin, vMax);
			mixer.OnChange += () => onChange(mixer.Color);
			mixer.Set(initialColor);

			// Configure confirmation callback for ENTER/SPACE in mixer
			mixer.OnConfirm = onConfirm;

			hueSlider.OnChange += h =>
			{
				mixer.SetColorLimits(sMin, sMax, vMin, vMax, h);
				var (_, _, s, v) = mixer.Color.ToAhsv();
				mixer.Set(Color.FromAhsv(h, s, v));
				onChange(mixer.Color);
			};

			hueSlider.UpdateValue(initialColor.ToAhsv().H);

			var randomButton = widget.GetOrNull<ButtonWidget>("RANDOM_BUTTON");
			if (randomButton != null)
			{
				var terrainColors = modData.DefaultTerrainInfo
					.SelectMany(t => t.Value.RestrictedPlayerColors)
					.Distinct()
					.ToList();
				var playerColors = Array.Empty<Color>();
				randomButton.OnClick = () =>
				{
					var randomColor = colorManager.RandomValidColor(world.LocalRandom, terrainColors, playerColors);
					mixer.Set(randomColor);
					hueSlider.UpdateValue(randomColor.ToAhsv().H);
				};
			}

			// HACK: the value returned from the color mixer will generally not
			// be equal to the given initialColor due to its internal RGB -> HSL -> RGB
			// conversion. This conversion can sometimes convert a valid initial value
			// into an invalid (too close to terrain / another player) color.
			// We use the original colour here instead of the mixer color to make sure
			// that we keep the player's previous colour value if they don't change anything
			onChange(initialColor);

			// Setup tab controls
			var mixerTab = widget.Get("MIXER_TAB");
			var paletteTab = widget.Get("PALETTE_TAB");
			var paletteTabPanel = widget.Get("PALETTE_TAB_PANEL");
			var mixerTabButton = widget.Get<ButtonWidget>("MIXER_TAB_BUTTON");
			var paletteTabButton = widget.Get<ButtonWidget>("PALETTE_TAB_BUTTON");
			var presetArea = paletteTabPanel.Get<ContainerWidget>("PRESET_AREA");
			var customArea = paletteTabPanel.Get<ContainerWidget>("CUSTOM_AREA");
			var presetColorTemplate = paletteTabPanel.Get<ColorBlockWidget>("COLORPRESET");
			var customColorTemplate = paletteTabPanel.Get<ColorBlockWidget>("COLORCUSTOM");

			mixerTab.IsVisible = () => !paletteTabOpenedLast;
			mixerTabButton.OnClick = () => paletteTabOpenedLast = false;
			mixerTabButton.IsHighlighted = mixerTab.IsVisible;

			paletteTab.IsVisible = () => paletteTabOpenedLast;
			paletteTabButton.OnClick = () => paletteTabOpenedLast = true;
			paletteTabButton.IsHighlighted = () => paletteTab.IsVisible() || paletteTabHighlighted > 0;

			var palettePresetRows = 2;
			var paletteCustomRows = 1;

			if (logicArgs.TryGetValue("PaletteColumns", out var yaml))
			{
				if (!int.TryParse(yaml.Value, out var cols))
					throw new YamlException($"Invalid value for PaletteColumns: {yaml.Value}");
				paletteCols = cols;
			}
			else
			{
				paletteCols = 8;
			}

			if (logicArgs.TryGetValue("PalettePresetRows", out yaml) && !int.TryParse(yaml.Value, out palettePresetRows))
				throw new YamlException($"Invalid value for PalettePresetRows: {yaml.Value}");
			if (logicArgs.TryGetValue("PaletteCustomRows", out yaml) && !int.TryParse(yaml.Value, out paletteCustomRows))
				throw new YamlException($"Invalid value for PaletteCustomRows: {yaml.Value}");

			var presetColors = colorManager.PresetColors;
			var tabIndex = 0;

			// Create preset color swatches
			for (var j = 0; j < palettePresetRows; j++)
			{
				for (var i = 0; i < paletteCols; i++)
				{
					var colorIndex = j * paletteCols + i;
					if (colorIndex >= presetColors.Length)
						break;

					var color = presetColors[colorIndex];

					var newSwatch = presetColorTemplate.Clone();
					newSwatch.GetColor = () => color;
					newSwatch.IsVisible = () => true;
					newSwatch.Bounds.X = i * newSwatch.Bounds.Width;
					newSwatch.Bounds.Y = j * newSwatch.Bounds.Height;
					newSwatch.TabIndex = tabIndex++;
					newSwatch.IsFocusable = true;

					// Mouse selection
					newSwatch.OnMouseUp = m =>
					{
						mixer.Set(color);
						hueSlider.UpdateValue(color.ToAhsv().H);
					};

					// Keyboard selection (ENTER/SPACE) - select color and close picker
					newSwatch.OnKeyboardSelect = () =>
					{
						mixer.Set(color);
						hueSlider.UpdateValue(color.ToAhsv().H);
						onConfirm?.Invoke();
					};

					// Arrow key navigation
					newSwatch.OnArrowKey = key => HandleSwatchArrowKey(newSwatch, key);

					// TAB focus gained - update preview color
					newSwatch.OnSwatchFocusGained = () => onColorChange?.Invoke(color);

					presetArea.AddChild(newSwatch);
					allSwatches.Add(newSwatch);
				}
			}

			// Create custom color swatches
			for (var j = 0; j < paletteCustomRows; j++)
			{
				for (var i = 0; i < paletteCols; i++)
				{
					var colorIndex = j * paletteCols + i;

					var newSwatch = customColorTemplate.Clone();
					var getColor = new CachedTransform<Color, Color>(c => colorManager.MakeValid(c, world.LocalRandom, [], []));

					newSwatch.GetColor = () => getColor.Update(Game.Settings.Player.CustomColors[colorIndex]);
					newSwatch.IsVisible = () => Game.Settings.Player.CustomColors.Length > colorIndex;
					newSwatch.Bounds.X = i * newSwatch.Bounds.Width;
					newSwatch.Bounds.Y = j * newSwatch.Bounds.Height;
					newSwatch.TabIndex = tabIndex++;
					newSwatch.IsFocusable = true;

					// Mouse selection
					newSwatch.OnMouseUp = m =>
					{
						var c = newSwatch.GetColor();
						mixer.Set(c);
						hueSlider.UpdateValue(c.ToAhsv().H);
					};

					// Keyboard selection (ENTER/SPACE) - select color and close picker
					newSwatch.OnKeyboardSelect = () =>
					{
						var c = newSwatch.GetColor();
						mixer.Set(c);
						hueSlider.UpdateValue(c.ToAhsv().H);
						onConfirm?.Invoke();
					};

					// Arrow key navigation
					newSwatch.OnArrowKey = key => HandleSwatchArrowKey(newSwatch, key);

					// TAB focus gained - update preview color
					newSwatch.OnSwatchFocusGained = () => onColorChange?.Invoke(newSwatch.GetColor());

					customArea.AddChild(newSwatch);
					allSwatches.Add(newSwatch);
				}
			}

			// Store color button
			var storeButton = widget.Get<ButtonWidget>("STORE_BUTTON");
			if (storeButton != null)
			{
				storeButton.OnClick = () =>
				{
					// Update the custom color list:
					//  - Remove any duplicates of the new color
					//  - Add the new color to the end
					//  - Save the last N colors
					Game.Settings.Player.CustomColors = Game.Settings.Player.CustomColors
						.Where(c => c != mixer.Color)
						.Append(mixer.Color)
						.Reverse().Take(paletteCustomRows * paletteCols).Reverse()
						.ToImmutableArray();
					Game.Settings.Save();

					// Flash the palette tab to show players that something has happened
					if (!paletteTabOpenedLast)
						paletteTabHighlighted = 4;
				};
			}

			// Attach logic to preview actor.
			extraLogic(widget);
		}

		// Handle arrow key navigation between color swatches
		bool HandleSwatchArrowKey(ColorBlockWidget currentSwatch, Keycode key)
		{
			var currentIndex = allSwatches.IndexOf(currentSwatch);
			if (currentIndex < 0)
				return false;

			// Filter to only visible swatches
			var visibleSwatches = allSwatches.Where(s => s.IsVisible()).ToList();
			var visibleIndex = visibleSwatches.IndexOf(currentSwatch);
			if (visibleIndex < 0)
				return false;

			var newIndex = visibleIndex;
			var cols = paletteCols;
			var totalVisible = visibleSwatches.Count;

			switch (key)
			{
				case Keycode.LEFT:
					newIndex = visibleIndex > 0 ? visibleIndex - 1 : totalVisible - 1;
					break;
				case Keycode.RIGHT:
					newIndex = visibleIndex < totalVisible - 1 ? visibleIndex + 1 : 0;
					break;
				case Keycode.UP:
					newIndex = visibleIndex >= cols ? visibleIndex - cols : visibleIndex;
					break;
				case Keycode.DOWN:
					newIndex = visibleIndex + cols < totalVisible ? visibleIndex + cols : visibleIndex;
					break;
				default:
					return false;
			}

			if (newIndex != visibleIndex && newIndex >= 0 && newIndex < totalVisible)
			{
				var newSwatch = visibleSwatches[newIndex];

				// Transfer TAB focus to the new swatch
				Ui.TabFocusWidget = newSwatch;
				currentSwatch.OnTabFocusLost();
				newSwatch.OnTabFocusGained();

				// Update the preview with the newly focused swatch's color
				onColorChange?.Invoke(newSwatch.GetColor());

				return true;
			}

			return false;
		}

		public override void Tick()
		{
			if (paletteTabHighlighted > 0)
				paletteTabHighlighted--;
		}
	}
}
