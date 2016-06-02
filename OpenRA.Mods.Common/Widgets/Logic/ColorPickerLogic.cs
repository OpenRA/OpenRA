#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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