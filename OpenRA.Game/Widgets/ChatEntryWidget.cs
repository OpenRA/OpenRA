#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	// a dirty hack of a widget, which likes to steal the focus when \r is pressed, and then
	// refuse to give it up until \r is pressed again.

	// this emulates the previous chat support, with one improvement: shift+enter can toggle the
	// team/all mode *while* composing, not just on beginning to compose.

	class ChatEntryWidget : Widget
	{
		string content = "";
		bool composing = false;
		bool teamChat = false;

		public override void DrawInner( WorldRenderer wr )
		{
			if (composing)
			{
				var text = teamChat ? "Chat (Team): " : "Chat (All): ";
				var w = Game.Renderer.BoldFont.Measure(text).X;

				Game.Renderer.BoldFont.DrawText(text, RenderOrigin + new float2(3, 7), Color.White);
				Game.Renderer.RegularFont.DrawText(content, RenderOrigin + new float2(3 + w, 7), Color.White);
			}
		}
		
		public override Rectangle EventBounds { get { return Rectangle.Empty; } }
		public override bool LoseFocus(MouseInput mi)
		{
			return composing ? false : base.LoseFocus(mi);
		}

		public override bool HandleInputInner(MouseInput mi) { return false; }

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up) return false;
			
			if (e.KeyChar == '\r')
			{
				if (composing)
				{
					if (e.Modifiers.HasModifier(Modifiers.Shift))
					{
						teamChat ^= true;
						return true;
					}

					composing = false;
					if (content != "")
						Game.IssueOrder(teamChat
							? Order.TeamChat(content)
							: Order.Chat(content));
					content = "";

					LoseFocus();
					return true;
				}
				else
				{
					TakeFocus(new MouseInput());
					composing = true;
					teamChat ^= e.Modifiers.HasModifier(Modifiers.Shift);
					return true;
				}
			}

			if (composing)
			{
				if (e.KeyChar == '\b' || e.KeyChar == 0x7f)
				{
					if (content.Length > 0)
						content = content.Remove(content.Length - 1);
					return true;
				}
				else if (!char.IsControl(e.KeyChar))
				{
					content += e.KeyChar;
					return true;
				}

				return false;
			}

			return base.HandleKeyPressInner(e);
		}
	}
}
