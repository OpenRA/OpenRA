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
	public class MissionResultsLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public MissionResultsLogic(Widget widget, World world, Action onContinue, Action onExit)
		{
			widget.Get<ButtonWidget>("CONTINUE").OnClick = onContinue;
			widget.Get<ButtonWidget>("MENU").OnClick = onExit;
			var stats = widget.Get("STATS");
			Game.LoadWidget(world, "SKIRMISH_STATS", stats, new WidgetArgs()
			{
				{ "hideMenu", (Action<bool>)(hidden => stats.IsVisible = () => !hidden) },
				{ "closeMenu", onExit }
			});
		}
	}
}
