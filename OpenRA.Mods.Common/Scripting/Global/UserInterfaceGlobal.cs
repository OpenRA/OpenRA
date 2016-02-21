#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
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
		public void SetMissionText(string text, HSLColor? color = null)
		{
			var luaLabel = Ui.Root.Get("INGAME_ROOT").Get<LabelWidget>("MISSION_TEXT");
			luaLabel.GetText = () => text;

			Color c = color.HasValue ? HSLColor.RGBFromHSL(color.Value.H / 255f, color.Value.S / 255f, color.Value.L / 255f) : Color.White;
			luaLabel.GetColor = () => c;
		}
	}
}
