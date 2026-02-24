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
using System.Globalization;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public sealed class CotOutputSettingsPromptLogic : ChromeLogic
	{
		[FluentReference]
		const string ModeLocalhost = "options-cot-mode.localhost";

		[FluentReference]
		const string ModeUnicast = "options-cot-mode.unicast";

		[FluentReference]
		const string ModeMulticast = "options-cot-mode.multicast";

		static CotSettings LoadSettingsConfig()
		{
			try
			{
				return Game.Settings.GetOrCreate<CotSettings>(null);
			}
			catch
			{
				return null;
			}
		}

		public static bool ShouldShowPrompt()
		{
			var s = LoadSettingsConfig();

			// Show prompt if settings haven't been configured (default port and host)
			return s == null || (s.Host == "127.0.0.1" && s.Port == 4242 && s.EndpointMode == "Localhost");
		}

		[ObjectCreator.UseCtor]
		public CotOutputSettingsPromptLogic(Widget widget, Action onComplete)
		{
			// Load from settings.yaml
			var s = LoadSettingsConfig() ?? new CotSettings();

			var mode = Enum.TryParse<CotEndpointMode>(s.EndpointMode, true, out var parsedMode)
				? parsedMode : CotEndpointMode.Localhost;
			var remember = true;

			var modeDropdown = widget.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			var hostField = widget.Get<TextFieldWidget>("HOST_FIELD");
			hostField.Text = string.IsNullOrWhiteSpace(s.Host)
				? (mode == CotEndpointMode.Localhost ? "127.0.0.1" : string.Empty)
				: s.Host;

			var portField = widget.Get<TextFieldWidget>("PORT_FIELD");
			portField.Text = (s.Port <= 0 ? 4242 : s.Port).ToString(NumberFormatInfo.CurrentInfo);

			var ttlContainer = widget.GetOrNull("MULTICAST_TTL_CONTAINER");
			var ttlField = widget.Get<TextFieldWidget>("TTL_FIELD");
			ttlField.Text = (s.MulticastTtl <= 0 ? 1 : s.MulticastTtl).ToString(NumberFormatInfo.CurrentInfo);

			var bindIfField = widget.Get<TextFieldWidget>("BIND_INTERFACE_FIELD");
			bindIfField.Text = s.BindInterfaceName ?? string.Empty;

			var rememberCheckbox = widget.Get<CheckboxWidget>("REMEMBER_CHECKBOX");
			rememberCheckbox.IsChecked = () => remember;
			rememberCheckbox.OnClick = () => remember ^= true;

			static string ModeLabel(CotEndpointMode m)
			{
				return m switch
				{
					CotEndpointMode.Localhost => FluentProvider.GetMessage(ModeLocalhost),
					CotEndpointMode.Multicast => FluentProvider.GetMessage(ModeMulticast),
					_ => FluentProvider.GetMessage(ModeUnicast),
				};
			}

			if (ttlContainer != null)
				ttlContainer.IsVisible = () => mode == CotEndpointMode.Multicast;

			modeDropdown.GetText = () => ModeLabel(mode);
			modeDropdown.OnMouseDown = _ => ShowModeDropdown(modeDropdown, () => mode, m => mode = m);

			// Buttons
			var continueButton = widget.Get<ButtonWidget>("CONTINUE_BUTTON");
			continueButton.IsDisabled = () => mode != CotEndpointMode.Localhost && string.IsNullOrWhiteSpace(hostField.Text);
			continueButton.OnClick = () =>
			{
				var host = hostField.Text;
				var hostBlank = string.IsNullOrWhiteSpace(host);
				if (mode == CotEndpointMode.Localhost)
					host = "127.0.0.1";
				else if (hostBlank)
					return; // Minimal validation: require host for Unicast/Multicast; keep prompt open

				if (!int.TryParse(portField.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var port) || port <= 0)
					port = 4242;

				int? ttl = null;
				if (mode == CotEndpointMode.Multicast)
				{
					if (int.TryParse(ttlField.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var parsedTtl) && parsedTtl > 0)
						ttl = parsedTtl;
					else
						ttl = 1;
				}

				// Write back to CotSettings
				s.EndpointMode = mode.ToString();
				s.Host = host;
				s.Port = port;
				s.MulticastTtl = ttl ?? 1;
				s.BindInterfaceName = string.IsNullOrWhiteSpace(bindIfField.Text) ? "" : bindIfField.Text.Trim();
				s.Save();

				// Start the service from settings
				CotOutputService.ConfigureFromSettings(s);

				Ui.CloseWindow();
				onComplete();
			};

			widget.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onComplete();
			};
		}

		static void ShowModeDropdown(DropDownButtonWidget dropdown, Func<CotEndpointMode> get, Action<CotEndpointMode> set)
		{
			var options = new Dictionary<string, CotEndpointMode>()
			{
				{ FluentProvider.GetMessage(ModeLocalhost), CotEndpointMode.Localhost },
				{ FluentProvider.GetMessage(ModeUnicast), CotEndpointMode.Unicast },
				{ FluentProvider.GetMessage(ModeMulticast), CotEndpointMode.Multicast },
			};

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => get() == options[o],
					() => set(options[o]));

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}
	}
}
