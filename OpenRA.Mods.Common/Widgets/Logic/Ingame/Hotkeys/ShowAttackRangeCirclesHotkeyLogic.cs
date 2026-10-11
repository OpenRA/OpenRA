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
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("ShowAttackRangeCirclesKey")]
	public class ShowAttackRangeCirclesHotkeyLogic : ChromeLogic
	{
		readonly World world;
		readonly HotkeyReference hotkeyRef;

		[ObjectCreator.UseCtor]
		public ShowAttackRangeCirclesHotkeyLogic(Widget widget, World world, ModData modData, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;

			hotkeyRef = new HotkeyReference();
			if (logicArgs.TryGetValue("ShowAttackRangeCirclesKey", out var yaml))
				hotkeyRef = modData.Hotkeys[yaml.Value];

			var keyhandler = (LogicKeyListenerWidget)widget;
			keyhandler.AddHandler(HandleKeyPress);
		}

		bool HandleKeyPress(KeyInput e)
		{
			var options = world.WorldActor.TraitOrDefault<AttackRangeCirclesOptions>();
			if (options == null)
				return false;

			if (!options.FeatureEnabled && !world.IsReplay)
				return false;

			var hotkeyValue = hotkeyRef.GetValue();
			if (hotkeyValue == Hotkey.Invalid)
				return false;

			if (e.Key != hotkeyValue.Key)
				return false;

			if (e.Event == KeyInputEvent.Down && e.Modifiers == hotkeyValue.Modifiers)
			{
				options.HotkeyHeld = true;
				return true;
			}

			if (e.Event == KeyInputEvent.Up && options.HotkeyHeld)
			{
				options.HotkeyHeld = false;
				return true;
			}

			return false;
		}
	}
}
