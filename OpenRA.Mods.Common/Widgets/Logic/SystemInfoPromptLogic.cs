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
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SystemInfoPromptLogic : ChromeLogic
	{
		// Increment the version number when adding new stats
		const int SystemInformationVersion = 5;

		static Dictionary<string, (string Label, string Value)> GetSystemInformation()
		{
			var lang = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
			return new Dictionary<string, (string, string)>()
			{
				{ "id", ("Anonymous ID", Game.Settings.Debug.UUID) },
				{ "platform", ("OS Type", Platform.CurrentPlatform.ToString()) },
				{ "arch", ("Architecture", Platform.CurrentArchitecture.ToString()) },
				{ "os", ("OS Version", Environment.OSVersion.ToString()) },
				{ "x64", ("OS is 64 bit", Environment.Is64BitOperatingSystem.ToString()) },
				{ "x64process", ("Process is 64 bit", Environment.Is64BitProcess.ToString()) },
				{ "runtime", (".NET Runtime", Platform.RuntimeVersion) },
				{ "gl", ("OpenGL Version", Game.Renderer.GLVersion) },
				{ "windowsize", ("Window Size", $"{Game.Renderer.NativeResolution.Width}x{Game.Renderer.NativeResolution.Height}") },
				{ "windowscale", ("Window Scale", Game.Renderer.NativeWindowScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "uiscale", ("UI Scale", Game.Settings.Graphics.UIScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "lang", ("System Language", lang) }
			};
		}

		public static bool ShouldShowPrompt()
		{
			return Game.Settings.Debug.SystemInformationVersionPrompt < SystemInformationVersion;
		}

		public static string CreateParameterString()
		{
			if (!Game.Settings.Debug.SendSystemInformation)
				return "";

			return $"&sysinfoversion={SystemInformationVersion}&"
				+ GetSystemInformation()
					.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value.Value))
					.JoinWith("&");
		}

		[ObjectCreator.UseCtor]
		public SystemInfoPromptLogic(Widget widget, Action onComplete)
		{
			var sysInfoCheckbox = widget.Get<CheckboxWidget>("SYSINFO_CHECKBOX");
			sysInfoCheckbox.IsChecked = () => Game.Settings.Debug.SendSystemInformation;
			sysInfoCheckbox.OnClick = () => Game.Settings.Debug.SendSystemInformation ^= true;

			var sysInfoData = widget.Get<ScrollPanelWidget>("SYSINFO_DATA");
			var template = sysInfoData.Get<LabelWidget>("DATA_TEMPLATE");
			sysInfoData.RemoveChildren();

			foreach (var info in GetSystemInformation().Values)
			{
				var label = template.Clone() as LabelWidget;
				var text = info.Label + ": " + info.Value;
				label.GetText = () => text;
				sysInfoData.AddChild(label);
			}

			widget.Get<ButtonWidget>("CONTINUE_BUTTON").OnClick = () =>
			{
				Game.Settings.Debug.SystemInformationVersionPrompt = SystemInformationVersion;
				Game.Settings.Save();
				Ui.CloseWindow();
				onComplete();
			};
		}
	}
}
