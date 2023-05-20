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

using OpenRA.Mods.Common.Scripting;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	sealed class ScriptErrorLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ScriptErrorLogic(Widget widget, World world)
		{
			var panel = widget.Get<ScrollPanelWidget>("SCRIPT_ERROR_MESSAGE_PANEL");
			var label = widget.Get<LabelWidget>("SCRIPT_ERROR_MESSAGE");
			var font = Game.Renderer.Fonts[label.Font];

			var luaScript = world.WorldActor.TraitOrDefault<LuaScript>();
			if (luaScript != null)
			{
				// Native exceptions have OS-dependend line endings, so strip these away as WrapText doesn't handle them
				var errorMessage = luaScript.Context.ErrorMessage.Replace("\r\n", "\n");

				var text = WidgetUtils.WrapText(errorMessage, label.Bounds.Width, font);
				label.Text = text;
				label.Bounds.Height = font.Measure(text).Y;
				panel.ScrollToTop();
				panel.Layout.AdjustChildren();
			}
		}
	}
}
