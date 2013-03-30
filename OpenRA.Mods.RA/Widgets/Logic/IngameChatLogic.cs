#region Copyright & License Information
/*
 * Copyright 2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;
using OpenRA.Network;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameChatLogic
	{
		internal World World;
		internal readonly ContainerWidget ChatOverlay;
		internal readonly ChatDisplayWidget ChatOverlayDisplay;


		internal readonly ContainerWidget ChatChrome;
		internal readonly ScrollPanelWidget ChatScrollPanel;
		internal readonly ContainerWidget ChatTemplate;
		internal readonly TextFieldWidget ChatText;

		private bool teamChat = false;
		internal bool TeamChat
		{
			get { return World.Observer ? false : teamChat; }
			set { teamChat = value; }
		}

		[ObjectCreator.UseCtor]
		public IngameChatLogic(Widget widget, OrderManager orderManager, World world)
		{
			World = world;
			var chatPanel = (ContainerWidget) widget;


			ChatOverlay = chatPanel.Get<ContainerWidget>("CHAT_OVERLAY");
			ChatOverlayDisplay = ChatOverlay.Get<ChatDisplayWidget>("CHAT_DISPLAY");
			ChatOverlay.Visible = false;

			ChatChrome = chatPanel.Get<ContainerWidget>("CHAT_CHROME");
			ChatChrome.Visible = true;

			var chatMode = ChatChrome.Get<ButtonWidget>("CHAT_MODE");
			chatMode.GetText = () => TeamChat ? "Team" : "All";
			chatMode.OnClick = () => TeamChat = !TeamChat;

			ChatText = ChatChrome.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			ChatText.OnTabKey = () => { TeamChat = !TeamChat; return true; };
			ChatText.OnEnterKey = () =>
			{
				ChatText.Text = ChatText.Text.Trim();
				if (ChatText.Text != "")
					orderManager.IssueOrder(Order.Chat(TeamChat, ChatText.Text));
				CloseChat();
				return true;
			};
			ChatText.OnEscKey = () => {CloseChat(); return true; };

			var chatClose = ChatChrome.Get<ButtonWidget>("CHAT_CLOSE");
			chatClose.OnClick += () => CloseChat();

			chatPanel.OnKeyPress = (e) =>
			{
				if (e.Event == KeyInputEvent.Up) return false;
				if (!IsOpen && (e.KeyName == "enter" || e.KeyName == "return") )
				{

					var shift = e.Modifiers.HasModifier(Modifiers.Shift);
					var toggle = Game.Settings.Game.TeamChatToggle ;
					TeamChat = (!toggle && shift) || ( toggle &&  (TeamChat ^ shift) );
					OpenChat();
					return true;
				}

				return false;
			};

			ChatScrollPanel = ChatChrome.Get<ScrollPanelWidget>("CHAT_SCROLLPANEL");
			ChatTemplate = ChatScrollPanel.Get<ContainerWidget>("CHAT_TEMPLATE");

			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;

			CloseChat();
			ChatOverlayDisplay.AddLine(Color.White, null, "Use RETURN key to open chat window...");
		}

		void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;
		}

		public void OpenChat()
		{
			ChatText.Text = "";
			ChatOverlay.Visible = false;
			ChatChrome.Visible = true;
			ChatText.TakeFocus(new MouseInput());
		}

		public void CloseChat()
		{
			ChatOverlay.Visible = true;
			ChatChrome.Visible = false;
			ChatText.LoseFocus();
		}

		public bool IsOpen { get { return ChatChrome.IsVisible(); } }

		public void AddChatLine(Color c, string from, string text)
		{

			ChatOverlayDisplay.AddLine(c, from, text);

			var template = ChatTemplate.Clone();
			var nameLabel = template.Get<LabelWidget>("NAME");
			var textLabel = template.Get<LabelWidget>("TEXT");

			var name = "";
			if (!string.IsNullOrEmpty(from))
				name = from + ":";
			var font = Game.Renderer.Fonts[nameLabel.Font];
			var nameSize = font.Measure(from);

			nameLabel.GetColor = () => c;
			nameLabel.GetText = () => name;
			nameLabel.Bounds.Width = nameSize.X;
			textLabel.Bounds.X += nameSize.X;
			textLabel.Bounds.Width -= nameSize.X;

			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			text = WidgetUtils.WrapText(text, textLabel.Bounds.Width, font);
			textLabel.GetText = () => text;
			var dh = font.Measure(text).Y - textLabel.Bounds.Height;
			if (dh > 0)
			{
				textLabel.Bounds.Height += dh;
				template.Bounds.Height += dh;
			}

			ChatScrollPanel.AddChild(template);
			ChatScrollPanel.ScrollToBottom();
			Sound.PlayNotification(null, "Sounds", "ChatLine", null);
		}
	}
}

