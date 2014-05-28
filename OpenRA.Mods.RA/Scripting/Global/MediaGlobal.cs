#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.Scripting
{
	[ScriptGlobal("Media")]
	public class MediaGlobal : ScriptGlobal
	{
		public MediaGlobal(ScriptContext context) : base(context) { }

		[Desc("Display a text message to the player.")]
		public void DisplayMessage(string text, string prefix = "Mission") // TODO: expose HSLColor to Lua and add as parameter
		{
			if (string.IsNullOrEmpty(text))
				return;

			Game.AddChatLine(Color.White, prefix, text);
		}
	}
}
