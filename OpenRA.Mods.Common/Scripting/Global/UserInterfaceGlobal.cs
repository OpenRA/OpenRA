#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Scripting.Global
{
	[ScriptGlobal("UserInterface")]
	public class UserInterfaceGlobal : ScriptGlobal
	{
		public UserInterfaceGlobal(ScriptContext context)
			: base(context) { }

		[Desc("Displays a text message at the top center of the screen.")]
		public void SetMissionText(string text, Color? color = null)
		{
			var luaLabel = Ui.Root.Get("INGAME_ROOT").Get<LabelWidget>("MISSION_TEXT");
			luaLabel.GetText = () => text;

			var c = color.HasValue ? color.Value : Color.White;
			luaLabel.GetColor = () => c;
		}

		public string Translate(string text)
		{
			return Context.World.Map.Translate(text);
		}
	}
}
