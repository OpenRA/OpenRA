#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ColorPickerLogic
	{
		ColorRamp ramp;

		[ObjectCreator.UseCtor]
		public ColorPickerLogic(Widget widget, ColorRamp initialRamp, Action<ColorRamp> onChange,
			Action<ColorRamp> onSelect, WorldRenderer worldRenderer)
		{
			var panel = widget.GetWidget("COLOR_CHOOSER");
			ramp = initialRamp;
			var hueSlider = panel.GetWidget<SliderWidget>("HUE_SLIDER");
			var satSlider = panel.GetWidget<SliderWidget>("SAT_SLIDER");
			var lumSlider = panel.GetWidget<SliderWidget>("LUM_SLIDER");

			Action sliderChanged = () =>
			{
				ramp = new ColorRamp((byte)(255*hueSlider.Value),
									 (byte)(255*satSlider.Value),
									 (byte)(255*lumSlider.Value),
									 10);
				onChange(ramp);
			};

			hueSlider.OnChange += _ => sliderChanged();
			satSlider.OnChange += _ => sliderChanged();
			lumSlider.OnChange += _ => sliderChanged();

			Action updateSliders = () =>
			{
				hueSlider.Value = ramp.H / 255f;
				satSlider.Value = ramp.S / 255f;
				lumSlider.Value = ramp.L / 255f;
			};

			panel.GetWidget<ButtonWidget>("SAVE_BUTTON").OnClick = () => onSelect(ramp);

			var randomButton = panel.GetWidget<ButtonWidget>("RANDOM_BUTTON");
			if (randomButton != null)
				randomButton.OnClick = () =>
				{
					var hue = (byte)Game.CosmeticRandom.Next(255);
					var sat = (byte)Game.CosmeticRandom.Next(255);
					var lum = (byte)Game.CosmeticRandom.Next(51,255);

					ramp = new ColorRamp(hue, sat, lum, 10);
					updateSliders();
					sliderChanged();
				};

			// Set the initial state
			updateSliders();
			onChange(ramp);
		}
	}
}

