#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Orders;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Widgets
{
	public class OrdersPaletteWidget : BackgroundWidget
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public OrdersPaletteWidget(World world)
			: base()
		{
			this.world = world;
		}

		public void DrawOrderButtonTooltip(IOrderTargeter order, Rectangle rect)
		{
			rect = rect.InflateBy(3, 3, 3, 3);
			var pos = new int2(rect.Left, rect.Top);
			var border = WidgetUtils.GetBorderSizes("dialog4");

			var height = 50;
			var width = 200;
			var tl = pos - new int2(0, height);

			WidgetUtils.DrawPanelPartial("dialog4", rect.InflateBy(0, border[1], 0, 0),
				PanelSides.Bottom | PanelSides.Left | PanelSides.Right);

			WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(tl.X, tl.Y, rect.Width + border[3], height + border[1]),
				PanelSides.Top | PanelSides.Left);

			WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(tl.X + rect.Width - border[2], tl.Y, width, height),
				PanelSides.Top | PanelSides.Right | PanelSides.Bottom);

			WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(tl.X, tl.Y, width + rect.Width - border[2], height), PanelSides.Center);

			WidgetUtils.DrawPanelPartial("dialog4", rect.InflateBy(0, border[1] + border[1], border[3], -rect.Height + border[1]), PanelSides.Center);

			WidgetUtils.DrawPanelPartial("dialog4", rect.InflateBy(0, border[1], 0, -rect.Height + border[1] + border[1]), PanelSides.Center);

			// Show the order name:
			Game.Renderer.Fonts["Bold"]
				.DrawText(order.OrderID, tl + new int2(border[0], border[1]), Color.White);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough;
		}

		// Giant hack
		public void Update()
		{
			RemoveChildren();
			int x = 0;
			foreach (var o in GetOrders())
			{
				var s = o; // Closure fail
				var child = new SidebarButtonWidget(world)
				{
					Bounds = new Rectangle(x, 0, 34, 28),
					OnClick = () =>
					{
						Game.Debug("OrderButton: {0}", s.OrderID);

						if (s.IsImmediate)
							IssueOrder(a => new Order(s.OrderID, a, false));
						else
							world.OrderGenerator = new RestrictedUnitOrderGenerator(s.OrderID);
					},
					Image = "opal-button",
					DrawTooltip = (rect) => DrawOrderButtonTooltip(s, rect)
				};
				AddChild(child);
				x += 34 + 3;
			}
		}

		void IssueOrder(Func<Actor, Order> f)
		{
			var orders = world.Selection.Actors.Select(f).ToArray();
			foreach (var o in orders) world.IssueOrder(o);
			world.PlayVoiceForOrders(orders);
		}

		static readonly IEqualityComparer<IOrderTargeter> orderComparer = new OrderTargeterEqualityComparer();

		class OrderTargeterEqualityComparer : IEqualityComparer<IOrderTargeter>
		{
			public bool Equals(IOrderTargeter x, IOrderTargeter y)
			{
				return x.OrderID.Equals(y.OrderID);
			}

			public int GetHashCode(IOrderTargeter obj)
			{
				return obj.OrderID.GetHashCode();
			}
		}

		List<IOrderTargeter> GetOrders()
		{
			return world.Selection.Actors
				.Where(a => !a.Destroyed && a.Owner == a.World.LocalPlayer)
				.SelectMany(a => a.TraitsImplementing<IIssueOrder>())
				.SelectMany(io => io.Orders)
				.Distinct(orderComparer)
				.ToList();
		}
	}

	public class UpdateOrderPaletteInfo : TraitInfo<UpdateOrderPalette> { }

	public class UpdateOrderPalette : INotifySelection
	{
		public void SelectionChanged()
		{
			Ui.Root.Get<OrdersPaletteWidget>("ORDERS_PALETTE").Update();
		}
	}
}
