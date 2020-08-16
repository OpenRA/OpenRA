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

		[ObjectCreator.UseCtor]
		public ColorPickerLogic(Widget widget, ModData modData, World world, Color initialColor, string initialFaction, Action<Color> onChange,
			Dictionary<string, MiniYaml> logicArgs)
		{
			if (initialFaction == null || !ChromeMetrics.TryGet("ColorPickerActorType-" + initialFaction, out string actorType))
				actorType = ChromeMetrics.Get<string>("ColorPickerActorType");

			var preview = widget.GetOrNull<ActorPreviewWidget>("PREVIEW");
			var actor = world.Map.Rules.Actors[actorType];

			var td = new TypeDictionary();
			td.Add(new OwnerInit(world.WorldActor.Owner));
			td.Add(new FactionInit(world.WorldActor.Owner.PlayerReference.Faction));
			foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
				foreach (var o in api.ActorPreviewInits(actor, ActorPreviewType.ColorPicker))
					td.Add(o);

			preview?.SetPreview(actor, td);

			var hueSlider = widget.Get<SliderWidget>("HUE");
			var mixer = widget.Get<ColorMixerWidget>("MIXER");
			var randomButton = widget.GetOrNull<ButtonWidget>("RANDOM_BUTTON");

			hueSlider.OnChange += _ => mixer.Set(hueSlider.Value);
			mixer.OnChange += () => onChange(mixer.Color);

			if (randomButton != null)
			{
				randomButton.OnClick = () =>
				{
					// Avoid colors with low sat or lum
					var hue = (byte)Game.CosmeticRandom.Next(255);
					var sat = (byte)Game.CosmeticRandom.Next(70, 255);
					var lum = (byte)Game.CosmeticRandom.Next(70, 255);
					var color = Color.FromAhsl(hue, sat, lum);

					mixer.Set(color);
					hueSlider.Value = HueFromColor(color);
				};
			}

			// Set the initial state
			var validator = modData.Manifest.Get<ColorValidator>();
			mixer.SetPaletteRange(validator.HsvSaturationRange[0], validator.HsvSaturationRange[1], validator.HsvValueRange[0], validator.HsvValueRange[1]);
			mixer.Set(initialColor);
			hueSlider.Value = HueFromColor(initialColor);

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

			var paletteCols = 8;
			var palettePresetRows = 2;
			var paletteCustomRows = 1;

			if (logicArgs.TryGetValue("PaletteColumns", out var yaml))
				if (!int.TryParse(yaml.Value, out paletteCols))
					throw new YamlException("Invalid value for PaletteColumns: {0}".F(yaml.Value));
			if (logicArgs.TryGetValue("PalettePresetRows", out yaml))
				if (!int.TryParse(yaml.Value, out palettePresetRows))
					throw new YamlException("Invalid value for PalettePresetRows: {0}".F(yaml.Value));
			if (logicArgs.TryGetValue("PaletteCustomRows", out yaml))
				if (!int.TryParse(yaml.Value, out paletteCustomRows))
					throw new YamlException("Invalid value for PaletteCustomRows: {0}".F(yaml.Value));

			for (var j = 0; j < palettePresetRows; j++)
			{
				for (var i = 0; i < paletteCols; i++)
				{
					var colorIndex = j * paletteCols + i;
					if (colorIndex >= validator.TeamColorPresets.Length)
						break;

					var color = validator.TeamColorPresets[colorIndex];

					var newSwatch = (ColorBlockWidget)presetColorTemplate.Clone();
					newSwatch.GetColor = () => color;
					newSwatch.IsVisible = () => true;
					newSwatch.Bounds.X = i * newSwatch.Bounds.Width;
					newSwatch.Bounds.Y = j * newSwatch.Bounds.Height;
					newSwatch.OnMouseUp = m =>
					{
						mixer.Set(color);
						hueSlider.Value = HueFromColor(color);
						onChange(color);
					};

					presetArea.AddChild(newSwatch);
				}
			}

			for (var j = 0; j < paletteCustomRows; j++)
			{
				for (var i = 0; i < paletteCols; i++)
				{
					var colorIndex = j * paletteCols + i;

					var newSwatch = (ColorBlockWidget)customColorTemplate.Clone();
					newSwatch.GetColor = () => Game.Settings.Player.CustomColors[colorIndex];
					newSwatch.IsVisible = () => Game.Settings.Player.CustomColors.Length > colorIndex;
					newSwatch.Bounds.X = i * newSwatch.Bounds.Width;
					newSwatch.Bounds.Y = j * newSwatch.Bounds.Height;
					newSwatch.OnMouseUp = m =>
					{
						var color = Game.Settings.Player.CustomColors[colorIndex];
						mixer.Set(color);
						hueSlider.Value = HueFromColor(color);
						onChange(color);
					};

					customArea.AddChild(newSwatch);
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
						.ToArray();
					Game.Settings.Save();

					// Flash the palette tab to show players that something has happened
					if (!paletteTabOpenedLast)
						paletteTabHighlighted = 4;
				};
			}
		}

		static float HueFromColor(Color c)
		{
			c.ToAhsv(out _, out var h, out _, out _);
			return h;
		}

		public static void ShowColorDropDown(DropDownButtonWidget color, ColorPreviewManagerWidget preview, World world)
		{
			Action onExit = () =>
			{
				Game.Settings.Player.Color = preview.Color;
				Game.Settings.Save();
			};

			color.RemovePanel();

			Action<Color> onChange = c => preview.Color = c;

			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onChange", onChange },
				{ "initialColor", Game.Settings.Player.Color },
				{ "initialFaction", null }
			});

			color.AttachPanel(colorChooser, onExit);
		}

		public override void Tick()
		{
			if (paletteTabHighlighted > 0)
				paletteTabHighlighted--;
		}
	}
}
