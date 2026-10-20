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

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AdminAuthPromptLogic : ChromeLogic
	{
		/// <summary>
		/// Opens the admin authentication panel, wiring orderManager so the
		/// submitted password is dispatched as an order.
		/// </summary>
		public static void Open(OrderManager orderManager)
		{
			Ui.OpenWindow("ADMIN_AUTH_PANEL", new WidgetArgs { { "orderManager", orderManager } });
		}

		[ObjectCreator.UseCtor]
		public AdminAuthPromptLogic(Widget widget, OrderManager orderManager)
		{
			var passwordField = widget.Get<PasswordFieldWidget>("PASSWORD_INPUT");
			var resetCheckbox = widget.Get<CheckboxWidget>("RESET_OTHERS_CHECKBOX");

			var resetOthers = false;
			resetCheckbox.IsChecked = () => resetOthers;
			resetCheckbox.OnClick = () => resetOthers = !resetOthers;

			void Submit()
			{
				var order = Order.Command($"admin {passwordField.Text} {(resetOthers ? "1" : "0")}");
				orderManager?.IssueOrder(order);
				Ui.CloseWindow();
			}

			passwordField.OnEnterKey = _ => { Submit(); return true; };
			widget.Get<ButtonWidget>("OK_BUTTON").OnClick = Submit;
			widget.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = Ui.CloseWindow;

			passwordField.TakeKeyboardFocus();
		}
	}
}
