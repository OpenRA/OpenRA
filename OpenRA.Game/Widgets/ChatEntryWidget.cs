#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;
using System.Windows.Forms;

namespace OpenRA.Widgets
{
	// a dirty hack of a widget, which likes to steal the focus when \r is pressed, and then
	// refuse to give it up until \r is pressed again.

	class ChatEntryWidget : Widget
	{
		string content = "";
		bool composing = true;

		public override void DrawInner(World world)
		{
			//if (!composing)
			//	return;

			Game.chrome.renderer.BoldFont.DrawText("Chat:", RenderOrigin + new float2(3, 7), Color.White);
			Game.chrome.renderer.RegularFont.DrawText(content, RenderOrigin + new float2(40, 7), Color.White);

			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}

		public override bool LoseFocus(MouseInput mi)
		{
			return composing ? false : base.LoseFocus(mi);
		}

		public override bool HandleKeyPress(KeyPressEventArgs e, Modifiers modifiers)
		{
			if (e.KeyChar == '\r')
			{
				if (composing)
				{
					composing = false;
					if (content != "")
						Game.IssueOrder(Order.Chat(content));
					content = "";

					LoseFocus();
					return true;
				}
				else
				{
					TakeFocus(new MouseInput());
					return true;
				}
			}

			if (composing)
			{
				if (e.KeyChar == '\b' || e.KeyChar == 0x7f)
				{
					if (content.Length > 0)
						content = content.Remove(content.Length - 1);
				}
				else if (!char.IsControl(e.KeyChar))
				{
					content += e.KeyChar;
					return true;
				}

				return false;
			}

			return base.HandleKeyPress(e, modifiers);
		}
	}
}
