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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("ToggleDepthPreviewKey",
		"IncreaseDepthPreviewContrastKey", "DecreaseDepthPreviewContrastKey",
		"IncreaseDepthPreviewOffsetKey", "DecreaseDepthPreviewOffsetKey")]
	public class DepthPreviewHotkeysLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public DepthPreviewHotkeysLogic(Widget widget, World world, ModData modData, Dictionary<string, MiniYaml> logicArgs)
		{
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();

			var toggleKey = new HotkeyReference();
			if (logicArgs.TryGetValue("ToggleDepthPreviewKey", out var yaml))
				toggleKey = modData.Hotkeys[yaml.Value];

			var increaseContrastKey = new HotkeyReference();
			if (logicArgs.TryGetValue("IncreaseDepthPreviewContrastKey", out yaml))
				increaseContrastKey = modData.Hotkeys[yaml.Value];

			var decreaseContrastKey = new HotkeyReference();
			if (logicArgs.TryGetValue("DecreaseDepthPreviewContrastKey", out yaml))
				decreaseContrastKey = modData.Hotkeys[yaml.Value];

			var increaseOffsetKey = new HotkeyReference();
			if (logicArgs.TryGetValue("IncreaseDepthPreviewOffsetKey", out yaml))
				increaseOffsetKey = modData.Hotkeys[yaml.Value];

			var decreaseOffsetKey = new HotkeyReference();
			if (logicArgs.TryGetValue("DecreaseDepthPreviewOffsetKey", out yaml))
				decreaseOffsetKey = modData.Hotkeys[yaml.Value];

			var keyhandler = widget.Get<LogicKeyListenerWidget>("DEPTHPREVIEW_KEYHANDLER");
			keyhandler.AddHandler(e =>
			{
				if (e.Event != KeyInputEvent.Down)
					return false;

				if (toggleKey.IsActivatedBy(e))
				{
					debugVis.DepthBuffer ^= true;
					return true;
				}

				if (!debugVis.DepthBuffer)
					return false;

				if (decreaseOffsetKey.IsActivatedBy(e))
				{
					debugVis.DepthBufferOffset -= 0.05f;
					return true;
				}

				if (increaseOffsetKey.IsActivatedBy(e))
				{
					debugVis.DepthBufferOffset += 0.05f;
					return true;
				}

				if (increaseContrastKey.IsActivatedBy(e))
				{
					debugVis.DepthBufferContrast += 0.1f;
					return true;
				}

				if (decreaseContrastKey.IsActivatedBy(e))
				{
					debugVis.DepthBufferContrast -= 0.1f;
					return true;
				}

				return false;
			});
		}
	}
}
