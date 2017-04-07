#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ColorPickerLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ColorPickerLogic(Widget widget, ModData modData, World world, HSLColor initialColor, Action<HSLColor> onChange, WorldRenderer worldRenderer)
		{
			string actorType;
			if (!ChromeMetrics.TryGet("ColorPickerActorType", out actorType))
				actorType = "mcv";

			var preview = widget.GetOrNull<ActorPreviewWidget>("PREVIEW");
			var actor = world.Map.Rules.Actors[actorType];

			var td = new TypeDictionary();
			td.Add(new HideBibPreviewInit());
			td.Add(new OwnerInit(world.WorldActor.Owner));
			td.Add(new FactionInit(world.WorldActor.Owner.PlayerReference.Faction));

			if (preview != null)
				preview.SetPreview(actor, td);

			var hueSlider = widget.Get<SliderWidget>("HUE");
			var mixer = widget.Get<ColorMixerWidget>("MIXER");
			var randomButton = widget.GetOrNull<ButtonWidget>("RANDOM_BUTTON");

			hueSlider.OnChange += _ => mixer.Set(hueSlider.Value);
			mixer.OnChange += () => onChange(mixer.Color);

			if (randomButton != null)
				randomButton.OnClick = () =>
				{
					// Avoid colors with low sat or lum
					var hue = (byte)Game.CosmeticRandom.Next(255);
					var sat = (byte)Game.CosmeticRandom.Next(70, 255);
					var lum = (byte)Game.CosmeticRandom.Next(70, 255);

					mixer.Set(new HSLColor(hue, sat, lum));
					hueSlider.Value = hue / 255f;
				};

			// Set the initial state
			var validator = modData.Manifest.Get<ColorValidator>();
			mixer.SetPaletteRange(validator.HsvSaturationRange[0], validator.HsvSaturationRange[1], validator.HsvValueRange[0], validator.HsvValueRange[1]);
			mixer.Set(initialColor);

			hueSlider.Value = initialColor.H / 255f;
			onChange(mixer.Color);
            
            
            // Setup the mod team preset colors
            int maxCustomColors = 10;

            var clearCustomPaletteButton = widget.GetOrNull<ButtonWidget>("CLEAR_CUSTOM_BUTTON");
            if (clearCustomPaletteButton != null)
                clearCustomPaletteButton.OnClick = () =>
                {
                    var blankCustomColors = Enumerable.Repeat("FFFFFF", maxCustomColors).ToArray();

                    var playerSettings = Game.Settings.Player;
                    playerSettings.CustomColors = blankCustomColors;

                    Game.Settings.Save();

                    for (int i = 0; i < maxCustomColors; i++)
                    {
                        var swatchName = "COLORCUSTOM" + i;
                        var swatch = widget.GetOrNull<ColorPaletteSwatchWidget>(swatchName);
                        swatch.GetColor = () => Color.White;
                    }
                };



            var defaultColorPresets = new Color[]
            {
                // RA1 colors
		        Color.FromArgb(196,176,96),     // C4B060
		        Color.FromArgb(68,148,228),     // 4494E4
		        Color.FromArgb(236,0,0),        // EC0000
		        Color.FromArgb(0,196,0),        // 00C400
		        Color.FromArgb(252,136,0),      // FC8800
		        Color.FromArgb(112,112,112),    // 707070
		        Color.FromArgb(68,144,124),     // 44907C
		        Color.FromArgb(112,44,36),      // 702C24
		    };

            // Parse each hex color
            var presetColors = new List<Color>();
            var modTeamColors = Game.ModData.Manifest.TeamColorPresets;
            if (modTeamColors != null && modTeamColors.Length > 0)
            {
                foreach (var modTeamColor in modTeamColors)
                {
                    Color newColor;
                    if (HSLColor.TryParseRGB(modTeamColor, out newColor))
                        presetColors.Add(newColor);
                }
            }
            else
            {
                presetColors = defaultColorPresets.ToList();
            }


            // Build the color swatch controls


            var addSwatchesAction = new Action<List<Color>, int, int, string, bool>((colorList, x, y, prefix, isEditable) =>
            {
                int maxX = 216;
                int width = 20;
                int pad = 2;
                string controlName = prefix;

                for (int i = 0; i < colorList.Count; i++)
                {
                    var nameString = controlName + i;
                    var newSwatch = new ColorPaletteSwatchWidget()
                    {
                        Id = nameString,
                        Width = width.ToString(),
                        Height = width.ToString(),
                        X = x.ToString(),
                        Y = y.ToString()
                    };
                    newSwatch.Initialize(new WidgetArgs());

                    int thisIndex = i;
                    var thisColor = colorList[i];
                    newSwatch.GetColor = () => thisColor;

                    newSwatch.OnClick = () =>
                    {
                        if (isEditable && Game.GetModifierKeys().HasModifier(Modifiers.Ctrl))
                        {
                            var playerSettings = Game.Settings.Player;

                            // Set all to white
                            var blankCustomColors = Enumerable.Repeat("FFFFFF", maxCustomColors).ToArray();
                            if (playerSettings.CustomColors == null)
                            {
                                playerSettings.CustomColors = blankCustomColors;
                            }
                            else
                            {
                                int savedColorIndex = 0;
                                foreach (var playerSettingsCustomColor in playerSettings.CustomColors)
                                {
                                    blankCustomColors[savedColorIndex] = playerSettingsCustomColor;
                                    savedColorIndex++;
                                }
                            }

                            var newColor = mixer.Color.ToHexString();
                            if (thisIndex < blankCustomColors.Length)
                            {
                                blankCustomColors[thisIndex] = newColor;
                                var newColorRGB = Color.FromArgb(mixer.Color.RGB.ToArgb());
                                newSwatch.GetColor = () => newColorRGB;
                            }

                            playerSettings.CustomColors = blankCustomColors;

                            Game.Settings.Save();
                        }
                        else
                        {
                            var newColor = newSwatch.GetColor();
                            mixer.Set(new HSLColor(newColor));
                        }

                    };
                    widget.AddChild(newSwatch);


                    x += width + pad;
                    if (x > maxX)
                    {
                        x = 5;
                        y += width + pad;
                    }

                }
            });

            int startX = 5;
            int startY = 158;

            addSwatchesAction(presetColors, startX, startY, "COLORPRESET", false);



            var defaultCustomColors = new Color[]
            {
                /*
		        Color.FromArgb(255,0,0),
                */
		    };
            defaultCustomColors = Enumerable.Repeat(Color.White, maxCustomColors).ToArray();

            // Parse each hex color
            var customColors = defaultCustomColors.ToList();
            var settingsCustomColors = Game.Settings.Player.CustomColors;
            if (settingsCustomColors != null && settingsCustomColors.Length > 0)
            {
                int i = 0;
                foreach (var customColor in settingsCustomColors)
                {
                    if (i >= maxCustomColors)
                        break;

                    Color newColor;
                    if (HSLColor.TryParseRGB(customColor, out newColor))
                        customColors[i] = newColor;
                    i++;
                }
            }


            startY = 202;
            addSwatchesAction(customColors, startX, startY, "COLORCUSTOM", true);

        }

        public static void ShowColorDropDown(DropDownButtonWidget color, ColorPreviewManagerWidget preview, World world)

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