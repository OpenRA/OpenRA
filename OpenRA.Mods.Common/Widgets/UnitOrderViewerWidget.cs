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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class UnitOrderViewerWidget : Widget
	{
		public readonly int2 IconSize = new int2(25, 25);
		public readonly int2 IconMargin = int2.Zero;
		public readonly int2 IconSpace = new int2(5, 4);

		List<OrderProviderCollection> orders = new List<OrderProviderCollection>();

		public override bool HandleMouseInput(MouseInput mi)
		{
			// TODO: Handle a click on the icons
			return false;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			foreach (var order in orders)
			{
				if (order.Info.Hotkey.Key == e.Key && order.Info.Hotkey.Modifiers == e.Modifiers)
				{
					order.ToogleOrder();
					return true;
				}
			}

			return false;
		}

		public void OnSelectionChange(Selection selection)
		{
			// Clean up since we don't want to have an IOrderTargeter active when we select them again.
			foreach (var order in orders)
				order.CancelOrder();

			orders.Clear();
			foreach (var a in selection.Actors)
			{
				foreach (var op in a.TraitsImplementing<IIssueOrder>())
				{
					var info = op.OrderInfo;
					if (info == null)
						continue;

					foreach (var io in info.IssuableOrders)
					{
						var provider = orders.FirstOrDefault(p => p.Order == io.Key);
						if (provider == null)
						{
							provider = new OrderProviderCollection(io.Key, io.Value);
							orders.Add(provider);
						}

						if (!provider.Issuers.Contains(a))
							provider.Issuers.Add(a);
					}
				}
			}

			orders.Sort((o1, o2) => o1.Info.UIOrder - o2.Info.UIOrder);

			// TODO: Setup new icons to render.
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			WidgetUtils.DrawPanel("panel-black", rb);
			var font = Game.Renderer.Fonts["TinyBold"];
			var x = 0;
			var y = 0;

			foreach (var order in orders)
			{
				if (order.Info.Hidden)
					continue;

				var rect = new Rectangle(rb.X + x * (IconSize.X + IconMargin.X), rb.Y + y * (IconSize.Y + IconMargin.Y), IconSize.X, IconSize.Y);
				var hover = Ui.MouseOverWidget == this && rect.Contains(Viewport.LastMousePos);
				var baseName = "button-order";
				ButtonWidget.DrawBackground(baseName, rect, false, false, hover, false);

				// TODO: Figure out how and where we want to get the sprite needed for abilities
				// WidgetUtils.DrawSHPCentered(icon.Sprite, icon.Pos + iconOffset, icon.Palette);
				x++;
				if (x == IconSpace.X)
				{
					y++;
					if (y == IconSpace.Y)
						return;

					x = 0;
				}

				var textSize = font.Measure(order.Info.Name);
				var position = new int2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2);
				font.DrawTextWithContrast(order.Info.Name, position, Color.White, Color.Black, 1);
				if (order.Info.Hotkey == Hotkey.Invalid)
					continue;

				var text = order.Info.Hotkey.Key.ToString();
				textSize = font.Measure(text);
				position = new int2(rect.X + (rect.Width - 3 * textSize.X / 2), rect.Y);
				font.DrawTextWithContrast(text, position, Color.White, Color.Black, 1);
			}
		}

		class OrderProviderCollection
		{
			public string Order { get; private set; }
			public OrderInfo Info { get; private set; }
			public List<Actor> Issuers { get; private set; }
			public bool Issued { get; private set; }

			public OrderProviderCollection(string order, OrderInfo info)
			{
				Order = order;
				Info = info;
				Issuers = new List<Actor>();
				Issued = false;
			}

			public void ToogleOrder()
			{
				if (Issued && Issuers.Any(p => p.World.UIOrderManager.OrderExists(p, Order)))
					CancelOrder();
				else
					IssueOrder();
			}

			public void IssueOrder()
			{
				Issued = true;
				foreach (var issuer in Issuers)
					issuer.World.UIOrderManager.IssueUIOrder(issuer, Order);
			}

			public void CancelOrder()
			{
				Issued = false;
				foreach (var issuer in Issuers)
					issuer.World.UIOrderManager.CancelUIOrder(issuer, Order);
			}
		}
	}
}
