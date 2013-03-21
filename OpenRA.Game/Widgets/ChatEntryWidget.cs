#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Network;

namespace OpenRA.Widgets
{
	// a dirty hack of a widget, which likes to steal the focus when \r is pressed, and then
	// refuse to give it up until \r is pressed again.

	public class ChatEntryWidget : Widget
	{
		string content = "";
		bool composing = false;
		bool teamChat = false;
		public readonly bool UseContrast = false;

		readonly OrderManager orderManager;

		[ObjectCreator.UseCtor]
		internal ChatEntryWidget(   OrderManager orderManager )
		{
			this.orderManager = orderManager;
		}

		public override void Draw()
		{
			if (composing)
			{
				var text = teamChat ? "Chat (Team): " : "Chat (All): ";
				var w = Game.Renderer.Fonts["Bold"].Measure(text).X;

				Game.Renderer.Fonts["Bold"].DrawTextWithContrast(text, RenderOrigin + new float2(3, 7), Color.White, Color.Black, UseContrast ? 1 : 0);
				Game.Renderer.Fonts["Regular"].DrawTextWithContrast(content, RenderOrigin + new float2(3 + w, 7), Color.White, Color.Black, UseContrast ? 1 : 0);
			}
		}

		public override Rectangle EventBounds { get { return Rectangle.Empty; } }

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up) return false;

			if (e.KeyName == "return" || e.KeyName == "enter" )
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
						orderManager.IssueOrder(Order.Chat(teamChat, content));
					content = "";

					LoseFocus();
					return true;
				}
				else
				{
					TakeFocus(new MouseInput());
					composing = true;
					teamChat = (Game.Settings.Game.TeamChatToggle && teamChat)
						^ e.Modifiers.HasModifier(Modifiers.Shift);
					return true;
				}
			}

			if (composing)
			{
				if (e.KeyName == "escape")
				{
					composing = false;
					content = "";
					LoseFocus();
					return true;
				}
				else if (e.KeyName == "backspace")
				{
					if (content.Length > 0)
						content = content.Remove(content.Length - 1);
					return true;
				}
				else if (e.IsValidInput())
				{
					content += e.UnicodeChar.ToString();
					return true;
				}

				return false;
			}

			return false;
		}
	}
}
