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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AddFactionSuffixLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AddFactionSuffixLogic(Widget widget, World world)
		{
			string faction;
			if (!ChromeMetrics.TryGet("FactionSuffix-" + world.LocalPlayer.Faction.InternalName, out faction))
				faction = world.LocalPlayer.Faction.InternalName;
			var suffix = "-" + faction;

			var buttonWidget = widget as ButtonWidget;
			if (buttonWidget != null)
				buttonWidget.Background += suffix;
			else
			{
				var imageWidget = widget as ImageWidget;
				if (imageWidget != null)
					imageWidget.ImageCollection += suffix;
				else
					throw new InvalidOperationException("AddFactionSuffixLogic only supports ButtonWidget and ImageWidget");
			}
		}
	}
}
