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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class AddRaceSuffixLogic
	{
		[ObjectCreator.UseCtor]
		public AddRaceSuffixLogic(Widget widget, World world)
		{
			var suffix = "-" + world.LocalPlayer.Country.Race;
			if (widget is ButtonWidget)
				((ButtonWidget)widget).Background += suffix;
			else if (widget is ImageWidget)
				((ImageWidget)widget).ImageCollection += suffix;
			else
				throw new InvalidOperationException("AddRaceSuffixLogic only supports ButtonWidget and ImageWidget");
		}
	}
}
