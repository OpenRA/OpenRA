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
	public class LoadLocalPlayerProfileLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public LoadLocalPlayerProfileLogic(Widget widget, World world)
		{
			Func<bool> minimalProfile = () => Ui.CurrentWindow() != null;

			Game.LoadWidget(world, "LOCAL_PROFILE_PANEL", widget, new WidgetArgs()
			{
				{ "minimalProfile", minimalProfile }
			});
		}
	}
}
