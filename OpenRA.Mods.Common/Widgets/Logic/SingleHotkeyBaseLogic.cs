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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public abstract class SingleHotkeyBaseLogic : ChromeLogic
	{
		protected SingleHotkeyBaseLogic(Widget widget, ModData modData, string argName, string parentName, Dictionary<string, MiniYaml> logicArgs)
		{
			var namedKey = new HotkeyReference();
			if (logicArgs.TryGetValue(argName, out var yaml))
				namedKey = modData.Hotkeys[yaml.Value];

			var keyhandler = widget.Get<LogicKeyListenerWidget>(parentName);
			keyhandler.AddHandler(e =>
			{
				if (e.Event == KeyInputEvent.Down)
					if (namedKey.IsActivatedBy(e))
						return OnHotkeyActivated(e);

				return false;
			});
		}

		protected abstract bool OnHotkeyActivated(KeyInput e);
	}
}
