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
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ColorPickerLogic
	{
		[ObjectCreator.UseCtor]
		public ColorPickerLogic(Widget widget, HSLColor initialColor, Action<HSLColor> onChange, WorldRenderer worldRenderer)
		{
			var ticker = widget.GetOrNull<LogicTickerWidget>("ANIMATE_PREVIEW");
			if (ticker != null)
			{
				var preview = widget.Get<SpriteSequenceWidget>("PREVIEW");
				var anim = preview.GetAnimation();
				anim.PlayRepeating(anim.CurrentSequence.Name);
				ticker.OnTick = anim.Tick;
			}

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
			mixer.Set(initialColor);
			hueSlider.Value = initialColor.H / 255f;
			onChange(mixer.Color);
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

