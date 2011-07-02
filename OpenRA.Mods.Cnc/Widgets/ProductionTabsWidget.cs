#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Widgets;
using System;

namespace OpenRA.Mods.Cnc.Widgets
{
	class ProductionTabsWidget : Widget
	{
		string queueType;
		public string QueueType
		{
			get
			{
				return queueType;
			}
			set
			{
				queueType = value;
				ListOffset = 0;
				ResetButtons();
				Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget)
					.CurrentQueue = tabs.Keys.FirstOrDefault();
			}
		}

		public string PaletteWidget = null;
		public float ScrollVelocity = 4f;
		public int TabWidth = 30;
		public int ArrowWidth = 20;

		public ProductionQueue[] AllQueues;
		public Dictionary<string, int> QueueCounts = new Dictionary<string, int>();
		Dictionary<ProductionQueue, Rectangle> tabs = new Dictionary<ProductionQueue, Rectangle>();

		int ContentWidth = 0;
		float ListOffset = 0;
		bool leftPressed = false;
		bool rightPressed = false;
		Rectangle leftButtonRect;
		Rectangle rightButtonRect;

		readonly World world;

		[ObjectCreator.UseCtor]
		public ProductionTabsWidget( [ObjectCreator.Param] World world )
		{
			this.world = world;
		}
		
		public override void DrawInner()
		{
			var rb = RenderBounds;

			leftButtonRect = new Rectangle(rb.X, rb.Y, ArrowWidth, rb.Height);
			rightButtonRect = new Rectangle(rb.Right - ArrowWidth, rb.Y, ArrowWidth, rb.Height);

			var leftDisabled = ListOffset >= 0;
			var rightDisabled = ListOffset <= Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - ContentWidth;

			WidgetUtils.DrawPanel("panel-black", rb);
			ButtonWidget.DrawBackground("button", leftButtonRect, leftDisabled,
			                            leftPressed, leftButtonRect.Contains(Viewport.LastMousePos));
			ButtonWidget.DrawBackground("button", rightButtonRect, rightDisabled,
			                            rightPressed, rightButtonRect.Contains(Viewport.LastMousePos));

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", leftPressed || leftDisabled ? "up_pressed" : "up_arrow"),
				new float2(leftButtonRect.Left + 2, leftButtonRect.Top + 2));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", rightPressed || rightDisabled ? "down_pressed" : "down_arrow"),
				new float2(rightButtonRect.Left + 2, rightButtonRect.Top + 2));

			Game.Renderer.EnableScissor(leftButtonRect.Right, rb.Y + 1, rightButtonRect.Left - leftButtonRect.Right - 1, rb.Height);

			var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);
			// TODO: Draw children buttons
			var i = 1;
			foreach (var tab in tabs)
			{
				ButtonWidget.DrawBackground("button", tab.Value, false, tab.Key == palette.CurrentQueue, tab.Value.Contains(Viewport.LastMousePos));

				SpriteFont font = Game.Renderer.Fonts["TinyBold"];
				var text = i.ToString();
				int2 textSize = font.Measure(text);
				int2 position = new int2(tab.Value.X + (tab.Value.Width - textSize.X)/2, tab.Value.Y + (tab.Value.Height - textSize.Y)/2);
				font.DrawTextWithContrast(text, position, Color.White, Color.Black, 1);
				i++;
			}

			Game.Renderer.DisableScissor();
		}

		void Scroll(int direction)
		{
			ListOffset += direction*ScrollVelocity;
			ListOffset = Math.Min(0,Math.Max(Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - ContentWidth, ListOffset));
		}

		public void ResetButtons()
		{
			tabs.Clear();
			ContentWidth = 0;
			var rb = RenderBounds;
			var origin = new int2(leftButtonRect.Right - 1 + (int)ListOffset, leftButtonRect.Y);

			foreach (var queue in AllQueues.Where(q => q.Info.Type == QueueType))
			{
				var rect = new Rectangle(origin.X + ContentWidth, origin.Y, TabWidth, rb.Height);
				tabs.Add(queue, rect);
				ContentWidth += TabWidth - 1;
			}
		}

		public override void Tick()
		{
			if (leftPressed) Scroll(1);
			if (rightPressed) Scroll(-1);

			AllQueues = world.ActorsWithTrait<ProductionQueue>()
				.Where(p => p.Actor.Owner == world.LocalPlayer)
				.Select(p => p.Trait).ToArray();

			QueueCounts = AllQueues.Select(q => q.Info.Type).Distinct()
				.ToDictionary(t => t, t => AllQueues.Count(q => q.Info.Type == t));

			ResetButtons();
			base.Tick();
		}

		public override bool LoseFocus(MouseInput mi)
		{
			leftPressed = rightPressed = false;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.WheelDown)
			{
				Scroll(-1);
				return true;
			}

			if (mi.Button == MouseButton.WheelUp)
			{
				Scroll(1);
				return true;
			}

			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			if (!Focused)
				return false;

			if (Focused && mi.Event == MouseInputEvent.Up)
				return LoseFocus(mi);

			leftPressed = leftButtonRect.Contains(mi.Location.X, mi.Location.Y);
			rightPressed = rightButtonRect.Contains(mi.Location.X, mi.Location.Y);

			var queue = tabs.Where(a => a.Value.Contains(mi.Location))
				.Select(a => a.Key).FirstOrDefault();

			if (queue != null)
			{
				var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);
				palette.CurrentQueue = queue;
			}

			return (leftPressed || rightPressed || queue != null);
		}
	}
}
