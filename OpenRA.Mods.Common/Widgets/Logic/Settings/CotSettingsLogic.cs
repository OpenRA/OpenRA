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
using System.Text;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class CotSettingsLogic : ChromeLogic
	{
		[FluentReference]
		const string ModeLocalhost = "options-cot-mode.localhost";

		[FluentReference]
		const string ModeUnicast = "options-cot-mode.unicast";

		[FluentReference]
		const string ModeMulticast = "options-cot-mode.multicast";

		readonly CotSettings cotSettings;

		CotEndpointMode mode;

		[ObjectCreator.UseCtor]
		public CotSettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label)
		{
			cotSettings = modData.GetSettings<CotSettings>();
			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			// Enable checkbox
			SettingsUtils.BindCheckboxPref(panel, "COT_ENABLED", cotSettings, "Enabled");

			// Mode dropdown
			mode = ParseMode(cotSettings.EndpointMode);

			var modeDropdown = panel.Get<DropDownButtonWidget>("COT_MODE_DROPDOWN");
			modeDropdown.GetText = () => ModeLabel(mode);
			// Host field
			var hostField = panel.Get<TextFieldWidget>("COT_HOST_FIELD");
			hostField.Text = cotSettings.Host;
			hostField.IsDisabled = () => mode == CotEndpointMode.Localhost;

			modeDropdown.OnMouseDown = _ => ShowModeDropdown(modeDropdown, scrollPanel, hostField);

			// Port field
			var portField = panel.Get<TextFieldWidget>("COT_PORT_FIELD");
			portField.Text = cotSettings.Port.ToString(NumberFormatInfo.CurrentInfo);

			// TTL field (visible only in Multicast mode)
			var ttlContainer = panel.Get("COT_TTL_CONTAINER");
			ttlContainer.IsVisible = () => mode == CotEndpointMode.Multicast;

			var ttlField = panel.Get<TextFieldWidget>("COT_TTL_FIELD");
			ttlField.Text = cotSettings.MulticastTtl.ToString(NumberFormatInfo.CurrentInfo);

			// Bind Interface field
			var bindIfField = panel.Get<TextFieldWidget>("COT_BIND_INTERFACE_FIELD");
			bindIfField.Text = cotSettings.BindInterfaceName ?? string.Empty;

			// Test button
			var testButton = panel.Get<ButtonWidget>("COT_TEST_BUTTON");
			testButton.OnClick = () =>
			{
				// Apply current on-screen values before sending
				cotSettings.EndpointMode = mode.ToString();
				cotSettings.Host = mode == CotEndpointMode.Localhost ? "127.0.0.1" : hostField.Text;

				if (int.TryParse(portField.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var p) && p is >= 1 and <= 65535)
					cotSettings.Port = p;

				if (int.TryParse(ttlField.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var t) && t > 0)
					cotSettings.MulticastTtl = t;

				cotSettings.BindInterfaceName = string.IsNullOrWhiteSpace(bindIfField.Text) ? "" : bindIfField.Text.Trim();

				CotOutputService.ConfigureFromSettings(cotSettings);
				CotOutputService.Enqueue(Encoding.UTF8.GetBytes(BuildTestCotXml()));
			};

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () =>
			{
				cotSettings.EndpointMode = mode.ToString();
				cotSettings.Host = mode == CotEndpointMode.Localhost ? "127.0.0.1" : hostField.Text;

				if (int.TryParse(portField.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var port) && port is >= 1 and <= 65535)
					cotSettings.Port = port;

				if (int.TryParse(ttlField.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var ttl) && ttl > 0)
					cotSettings.MulticastTtl = ttl;

				cotSettings.BindInterfaceName = string.IsNullOrWhiteSpace(bindIfField.Text) ? "" : bindIfField.Text.Trim();

				// Apply changes live
				if (cotSettings.Enabled)
					CotOutputService.ConfigureFromSettings(cotSettings);

				return false;
			};
		}

		Action ResetPanel(Widget panel)
		{
			var defaults = new CotSettings();
			return () =>
			{
				cotSettings.Enabled = defaults.Enabled;
				cotSettings.EndpointMode = defaults.EndpointMode;
				cotSettings.Host = defaults.Host;
				cotSettings.Port = defaults.Port;
				cotSettings.MulticastTtl = defaults.MulticastTtl;
				cotSettings.BindInterfaceName = defaults.BindInterfaceName;

				mode = ParseMode(defaults.EndpointMode);
				panel.Get<TextFieldWidget>("COT_HOST_FIELD").Text = defaults.Host;
				panel.Get<TextFieldWidget>("COT_PORT_FIELD").Text = defaults.Port.ToString(NumberFormatInfo.CurrentInfo);
				panel.Get<TextFieldWidget>("COT_TTL_FIELD").Text = defaults.MulticastTtl.ToString(NumberFormatInfo.CurrentInfo);
				panel.Get<TextFieldWidget>("COT_BIND_INTERFACE_FIELD").Text = defaults.BindInterfaceName;
			};
		}

		static CotEndpointMode ParseMode(string s)
		{
			if (Enum.TryParse<CotEndpointMode>(s, true, out var m))
				return m;
			return CotEndpointMode.Localhost;
		}

		static string ModeLabel(CotEndpointMode m)
		{
			return m switch
			{
				CotEndpointMode.Localhost => FluentProvider.GetMessage(ModeLocalhost),
				CotEndpointMode.Multicast => FluentProvider.GetMessage(ModeMulticast),
				_ => FluentProvider.GetMessage(ModeUnicast),
			};
		}

		void ShowModeDropdown(DropDownButtonWidget dropdown, ScrollPanelWidget scrollPanel, TextFieldWidget hostField)
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
					() => mode == options[o],
					() =>
					{
						mode = options[o];
						hostField.Text = DefaultHostForMode(mode);
						SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);
					});

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}

		static string DefaultHostForMode(CotEndpointMode m)
		{
			return m switch
			{
				CotEndpointMode.Localhost => "127.0.0.1",
				CotEndpointMode.Multicast => "239.2.3.1",
				_ => "",
			};
		}

		static string BuildTestCotXml()
		{
			var now = DateTime.UtcNow;
			var stale = now.AddSeconds(120);
			var uid = "OpenRA-Test-" + now.Ticks.ToString(CultureInfo.InvariantCulture);
			var time = now.ToString("yyyy-MM-dd'T'HH:mm:ss.ff'Z'", CultureInfo.InvariantCulture);
			var staleStr = stale.ToString("yyyy-MM-dd'T'HH:mm:ss.ff'Z'", CultureInfo.InvariantCulture);
			var callsign = XmlEscape("OpenRA Test");

			return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
				+ "<event version=\"2.0\""
				+ " uid=\"" + uid + "\""
				+ " type=\"a-f-G\""
				+ " how=\"m-g\""
				+ " time=\"" + time + "\""
				+ " start=\"" + time + "\""
				+ " stale=\"" + staleStr + "\">"
				+ "<point lat=\"34.2257\" lon=\"-77.9447\" hae=\"0.0\" ce=\"25.0\" le=\"25.0\"/>"
				+ "<detail>"
				+ "<contact callsign=\"" + callsign + "\"/>"
				+ "</detail>"
				+ "</event>";
		}

		static string XmlEscape(string s)
		{
			return s.Replace("&", "&amp;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;")
				.Replace("\"", "&quot;")
				.Replace("'", "&apos;");
		}
	}
}
