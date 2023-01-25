#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			if (!ChromeMetrics.TryGet("FactionSuffix-" + world.LocalPlayer.Faction.InternalName, out string faction))
				faction = world.LocalPlayer.Faction.InternalName;
			var suffix = "-" + faction;

			if (widget is ButtonWidget bw)
				bw.Background += suffix;
			else if (widget is ImageWidget iw)
				iw.ImageCollection += suffix;
			else if (widget is BackgroundWidget bgw)
				bgw.Background += suffix;
			else if (widget is TextFieldWidget tfw)
				tfw.Background += suffix;
			else if (widget is ScrollPanelWidget spw)
			{
				spw.Button += suffix;
				spw.Background += suffix;
				spw.ScrollBarBackground += suffix;
				spw.Decorations += suffix;
			}
			else if (widget is ProductionTabsWidget ptw)
			{
				ptw.ArrowButton += suffix;
				ptw.TabButton += suffix;

				// TODO: This isn't functional, ProductionTabsWidget's caches aren't updated with the new values.
				ptw.Decorations += suffix;
				ptw.Background += suffix;
			}
			else
				throw new InvalidOperationException("AddFactionSuffixLogic only supports ButtonWidget, ImageWidget, BackgroundWidget, TextFieldWidget, ScrollPanelWidget and ProductionTabsWidget");
		}
	}
}
