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
using System.Collections.Generic;
using System.Globalization;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame.Hotkeys
{
	[ChromeLogicArgsHotkeys("QuickSaveKey")]
	public class QuickSaveHotkeyLogic : SingleHotkeyBaseLogic
	{
		const string QuickSavePattern = "quicksave-";
		const string SaveFileExtension = ".orasav";
		readonly World world;

		[ObjectCreator.UseCtor]
		public QuickSaveHotkeyLogic(Widget widget, World world, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "QuickSaveKey", "GLOBAL_KEYHANDLER", logicArgs)
		{
			this.world = world;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			var dateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.InvariantCulture);
			var fileName = $"{QuickSavePattern}{dateTime}{SaveFileExtension}";
			world.RequestGameSave(fileName, false);
			return true;
		}
	}
}
