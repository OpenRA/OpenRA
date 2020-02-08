#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1203:ConstantsMustAppearBeforeFields",
		Justification = "SystemInformation version should be defined next to the dictionary it refers to.")]
	public class SystemInfoPromptLogic : ChromeLogic
	{
		// Increment the version number when adding new stats
		const int SystemInformationVersion = 4;

		static Dictionary<string, Pair<string, string>> GetSystemInformation()
		{
			var lang = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
			return new Dictionary<string, Pair<string, string>>()
			{
				{ "id", Pair.New("Anonymous ID", Game.Settings.Debug.UUID) },
				{ "platform", Pair.New("OS Type", Platform.CurrentPlatform.ToString()) },
				{ "os", Pair.New("OS Version", Environment.OSVersion.ToString()) },
				{ "x64", Pair.New("OS is 64 bit", Environment.Is64BitOperatingSystem.ToString()) },
				{ "x64process", Pair.New("Process is 64 bit", Environment.Is64BitProcess.ToString()) },
				{ "runtime", Pair.New(".NET Runtime", Platform.RuntimeVersion) },
				{ "gl", Pair.New("OpenGL Version", Game.Renderer.GLVersion) },
				{ "windowsize", Pair.New("Window Size", "{0}x{1}".F(Game.Renderer.NativeResolution.Width, Game.Renderer.NativeResolution.Height)) },
				{ "windowscale", Pair.New("Window Scale", Game.Renderer.NativeWindowScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "uiscale", Pair.New("UI Scale", Game.Settings.Graphics.UIScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "lang", Pair.New("System Language", lang) }
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

			return "&sysinfoversion={0}&".F(SystemInformationVersion)
			       + GetSystemInformation()
				       .Select(kv => kv.Key + "=" + Uri.EscapeUriString(kv.Value.Second))
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
				var text = info.First + ": " + info.Second;
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
