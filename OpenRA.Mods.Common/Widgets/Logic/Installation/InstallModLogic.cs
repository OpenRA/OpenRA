#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallModLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public InstallModLogic(Widget widget, string modId)
		{
			var panel = widget.Get("INSTALL_MOD_PANEL");

			var mods = Manifest.AllMods[modId].RequiresMods.Where(m => !Game.IsModInstalled(m)).Select(m => "{0} ({1})".F(m.Key, m.Value));
			var text = string.Join(", ", mods);
			panel.Get<LabelWidget>("MOD_LIST").Text = text;

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = Ui.CloseWindow;
		}
	}
}
