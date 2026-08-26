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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Tcd.Orders;
using OpenRA.Mods.Tcd.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Tcd.Widgets.Logic
{
	// Watches the two formation-drawing keys. SingleHotkeyBaseLogic only reports key
	// presses, and these need to know when the key is released as well, so this hooks
	// the raw key listener instead.
	[ChromeLogicArgsHotkeys("FormationPointsKey", "FormationDrawKey")]
	public sealed class FormationInputLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public FormationInputLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			var pointsKey = Bind(modData, logicArgs, "FormationPointsKey");
			var drawKey = Bind(modData, logicArgs, "FormationDrawKey");

			var listener = widget.Get<LogicKeyListenerWidget>("PLAYER_KEYHANDLER");
			listener.AddHandler(e =>
			{
				var capture = world.WorldActor.TraitOrDefault<FormationCapture>();
				if (capture == null)
					return false;

				if (pointsKey.IsActivatedBy(e))
					return HandlePoints(world, capture, e);

				if (drawKey.IsActivatedBy(e))
					return HandleDraw(world, capture, e);

				return false;
			});
		}

		static HotkeyReference Bind(ModData modData, Dictionary<string, MiniYaml> logicArgs, string argName)
		{
			return logicArgs.TryGetValue(argName, out var yaml) ? modData.Hotkeys[yaml.Value] : new HotkeyReference();
		}

		static bool HandlePoints(World world, FormationCapture capture, KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				// Only one drawing tool at a time.
				if (world.OrderGenerator is LineFormationOrderGenerator)
					world.CancelInputMode();

				capture.Begin(FormationCaptureMode.Points);
				return true;
			}

			var placed = capture.Commit();
			TextNotificationsManager.Debug(placed > 0
				? $"Shape formation: {placed} units."
				: "Shape cancelled: no points marked.");

			return true;
		}

		static bool HandleDraw(World world, FormationCapture capture, KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				// Swapping the generator is what buys us press-and-drag: only a generator
				// that is not a UnitOrderGenerator receives mouse-down and mouse-move.
				capture.Cancel();
				if (world.OrderGenerator is not LineFormationOrderGenerator)
					world.OrderGenerator = new LineFormationOrderGenerator(world, oneShot: false);
			}
			else if (world.OrderGenerator is LineFormationOrderGenerator)
				world.CancelInputMode();

			return true;
		}
	}
}
