#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public abstract class SingleHotkeyBaseLogic : ChromeLogic
	{
		protected SingleHotkeyBaseLogic(Widget widget, string argName, string parentName, Dictionary<string, MiniYaml> logicArgs)
		{
			var ks = Game.Settings.Keys;
			MiniYaml yaml;

			var namedKey = new HotkeyReference();
			if (logicArgs.TryGetValue(argName, out yaml))
				namedKey = new HotkeyReference(yaml.Value, ks);

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
