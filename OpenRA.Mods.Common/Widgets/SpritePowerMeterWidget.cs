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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SpritePowerMeterWidget : Widget
	{
		[Desc("The name of the Container Widget to tie the Y axis to.")]
		[FieldLoader.Require]
		public readonly string MeterAlongside = "";

		[Desc("The name of the Container with the items to get the height from.")]
		[FieldLoader.Require]
		public readonly string ParentContainer = "";

		[Desc("Height of each meter bar")]
		[FieldLoader.Require]
		public readonly int MeterHeight = 3;

		[Desc("How many units of power each bar represents.")]
		[FieldLoader.Require]
		public readonly int PowerUnitsPerBar = 25;

		[Desc("How many Ticks to wait before animating the bar.")]
		[FieldLoader.Require]
		public readonly int TickWait = 4;

		[Desc("Blank Image for the meter bar")]
		[FieldLoader.Require]
		public readonly string NoPowerImage = "";

		[Desc("When you have access power to use.")]
		[FieldLoader.Require]
		public readonly string AvailablePowerImage = "";

		[Desc("Used power image")]
		[FieldLoader.Require]
		public readonly string UsedPowerImage = "";

		[Desc("Too much power used meter image.")]
		[FieldLoader.Require]
		public readonly string OverUsedPowerImage = "";

		[Desc("How many Ticks to wait before animating the bar.")]
		[FieldLoader.Require]
		public readonly string FlashPowerImage = "";

		[Desc("The collection of images to get the meter images from.")]
		[FieldLoader.Require]
		public readonly string ImageCollection = "";

		[ObjectCreator.UseCtor]
		public SpritePowerMeterWidget() { }
	}
}
