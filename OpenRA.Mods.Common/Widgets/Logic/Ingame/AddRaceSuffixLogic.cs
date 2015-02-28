#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AddRaceSuffixLogic
	{
		[ObjectCreator.UseCtor]
		public AddRaceSuffixLogic(Widget widget, World world)
		{
			string race;
			if (!ChromeMetrics.TryGet("RaceSuffix-" + world.LocalPlayer.Country.Race, out race))
				race = world.LocalPlayer.Country.Race;
			var suffix = "-" + race;

			if (widget is ButtonWidget)
				((ButtonWidget)widget).Background += suffix;
			else if (widget is ImageWidget)
				((ImageWidget)widget).ImageCollection += suffix;
			else
				throw new InvalidOperationException("AddRaceSuffixLogic only supports ButtonWidget and ImageWidget");
		}
	}
}
