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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncColorPickerLogic
	{
		ColorRamp ramp;
		[ObjectCreator.UseCtor]
		public CncColorPickerLogic([ObjectCreator.Param] Widget widget,
		                           [ObjectCreator.Param] ColorRamp initialRamp,
		                           [ObjectCreator.Param] Action<ColorRamp> onChange,
		                           [ObjectCreator.Param] Action<ColorRamp> onSelect,
		                           [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			var panel = widget.GetWidget("COLOR_CHOOSER");
			ramp = initialRamp;
			var hueSlider = panel.GetWidget<SliderWidget>("HUE_SLIDER");
			var satSlider = panel.GetWidget<SliderWidget>("SAT_SLIDER");
			var lumSlider = panel.GetWidget<SliderWidget>("LUM_SLIDER");
			
			Action sliderChanged = () => 
			{
				ramp = new ColorRamp((byte)(255*hueSlider.GetOffset()),
				                     (byte)(255*satSlider.GetOffset()),
				                     (byte)(255*lumSlider.GetOffset()),
				                     10);
				onChange(ramp);
			};
				         
			hueSlider.OnChange += _ => sliderChanged();
			satSlider.OnChange += _ => sliderChanged();
			lumSlider.OnChange += _ => sliderChanged();
			
			Action updateSliders = () =>
			{
				hueSlider.SetOffset(ramp.H / 255f);
				satSlider.SetOffset(ramp.S / 255f);
				lumSlider.SetOffset(ramp.L / 255f);
			};
			
			panel.GetWidget<ButtonWidget>("SAVE_BUTTON").OnClick = () => onSelect(ramp);
			panel.GetWidget<ButtonWidget>("RANDOM_BUTTON").OnClick = () => 
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

