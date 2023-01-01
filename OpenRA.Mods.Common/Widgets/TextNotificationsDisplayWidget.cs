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

using System;
using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TextNotificationsDisplayWidget : Widget
	{
		public readonly int DisplayDurationMs = 0;
		public readonly int ItemSpacing = 4;
		public readonly int BottomSpacing = 0;
		public readonly int LogLength = 8;
		public readonly bool HideOverflow = true;

		public string ChatTemplate = "CHAT_LINE_TEMPLATE";
		public string SystemTemplate = "SYSTEM_LINE_TEMPLATE";
		public string MissionTemplate = "CHAT_LINE_TEMPLATE";
		public string FeedbackTemplate = "TRANSIENT_LINE_TEMPLATE";
		public string TransientsTemplate = "TRANSIENT_LINE_TEMPLATE";
		readonly Dictionary<TextNotificationPool, Widget> templates = new Dictionary<TextNotificationPool, Widget>();

		readonly List<long> expirations = new List<long>();

		Rectangle overflowDrawBounds = Rectangle.Empty;
		public override Rectangle EventBounds => Rectangle.Empty;

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			templates.Add(TextNotificationPool.Chat, Ui.LoadWidget(ChatTemplate, null, new WidgetArgs()));
			templates.Add(TextNotificationPool.System, Ui.LoadWidget(SystemTemplate, null, new WidgetArgs()));
			templates.Add(TextNotificationPool.Mission, Ui.LoadWidget(MissionTemplate, null, new WidgetArgs()));
			templates.Add(TextNotificationPool.Feedback, Ui.LoadWidget(FeedbackTemplate, null, new WidgetArgs()));
			templates.Add(TextNotificationPool.Transients, Ui.LoadWidget(TransientsTemplate, null, new WidgetArgs()));

			// HACK: Assume that all templates use the same font
			var lineHeight = Game.Renderer.Fonts[templates[TextNotificationPool.Chat].Get<LabelWidget>("TEXT").Font].Measure("").Y;
			var wholeLines = (int)Math.Floor((double)((Bounds.Height - BottomSpacing) / lineHeight));
			var visibleChildrenHeight = wholeLines * lineHeight;

			overflowDrawBounds = new Rectangle(RenderOrigin.X, RenderOrigin.Y, Bounds.Width, Bounds.Height);
			overflowDrawBounds.Y += Bounds.Height - visibleChildrenHeight;
			overflowDrawBounds.Height = visibleChildrenHeight;
		}

		public override void DrawOuter()
		{
			if (!IsVisible() || Children.Count == 0)
				return;

			var mostRecentMessageOverflows = Bounds.Height < Children[Children.Count - 1].Bounds.Height;

			if (mostRecentMessageOverflows && HideOverflow)
				Game.Renderer.EnableScissor(overflowDrawBounds);

			for (var i = Children.Count - 1; i >= 0; i--)
			{
				if (Bounds.Contains(Children[i].Bounds) || !HideOverflow || mostRecentMessageOverflows)
					Children[i].DrawOuter();

				if (mostRecentMessageOverflows)
					break;
			}

			if (mostRecentMessageOverflows && HideOverflow)
				Game.Renderer.DisableScissor();
		}

		public void AddNotification(TextNotification notification)
		{
			var notificationWidget = templates[notification.Pool].Clone();
			WidgetUtils.SetupTextNotification(notificationWidget, notification, Bounds.Width, false);

			if (Children.Count == 0)
				notificationWidget.Bounds.Y = Bounds.Bottom - notificationWidget.Bounds.Height - BottomSpacing;
			else
			{
				foreach (var line in Children)
					line.Bounds.Y -= notificationWidget.Bounds.Height + ItemSpacing;

				var lastLine = Children[Children.Count - 1];
				notificationWidget.Bounds.Y = lastLine.Bounds.Bottom + ItemSpacing;
			}

			AddChild(notificationWidget);
			expirations.Add(Game.RunTime + DisplayDurationMs);

			while (Children.Count > LogLength)
				RemoveNotification();
		}

		public void RemoveMostRecentNotification()
		{
			if (Children.Count == 0)
				return;

			var mostRecentChild = Children[Children.Count - 1];

			RemoveChild(mostRecentChild);
			expirations.RemoveAt(expirations.Count - 1);

			for (var i = Children.Count - 1; i >= 0; i--)
				Children[i].Bounds.Y += mostRecentChild.Bounds.Height + ItemSpacing;
		}

		void RemoveNotification()
		{
			if (Children.Count == 0)
				return;

			RemoveChild(Children[0]);
			expirations.RemoveAt(0);
		}

		public override void Tick()
		{
			if (DisplayDurationMs == 0)
				return;

			// This takes advantage of the fact that recentLines is ordered by expiration, from sooner to later
			while (Children.Count > 0 && Game.RunTime >= expirations[0])
				RemoveNotification();
		}
	}
}
