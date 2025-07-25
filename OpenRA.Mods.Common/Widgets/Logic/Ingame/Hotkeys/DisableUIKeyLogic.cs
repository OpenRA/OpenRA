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

using System.Collections.Generic;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("DisableUIKey")]
	public class DisableUIKeyLogic : SingleHotkeyBaseLogic
	{
		[ObjectCreator.UseCtor]
		public DisableUIKeyLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "DisableUIKey", "GLOBAL_KEYHANDLER", logicArgs)
		{
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			// Allow to show the cursor without making UI visible.
			if (!Ui.WidgetsVisible && Game.HideCursor)
			{
				Game.HideCursor = false;
				return true;
			}

			Ui.WidgetsVisible ^= true;
			Game.HideCursor = false;

			return true;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				Ui.WidgetsVisible = true;
				Game.HideCursor = false;
			}
		}
	}
}
