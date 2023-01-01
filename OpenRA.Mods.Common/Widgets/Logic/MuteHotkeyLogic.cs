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

using System.Collections.Generic;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("MuteAudioKey")]
	public class MuteHotkeyLogic : SingleHotkeyBaseLogic
	{
		[TranslationReference]
		const string AudioMuted = "label-audio-muted";

		[TranslationReference]
		const string AudioUnmuted = "label-audio-unmuted";

		readonly ModData modData;

		[ObjectCreator.UseCtor]
		public MuteHotkeyLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "MuteAudioKey", "GLOBAL_KEYHANDLER", logicArgs)
		{
			this.modData = modData;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			Game.Settings.Sound.Mute ^= true;

			if (Game.Settings.Sound.Mute)
			{
				Game.Sound.MuteAudio();
				TextNotificationsManager.AddFeedbackLine(modData.Translation.GetString(AudioMuted));
			}
			else
			{
				Game.Sound.UnmuteAudio();
				TextNotificationsManager.AddFeedbackLine(modData.Translation.GetString(AudioUnmuted));
			}

			return true;
		}
	}
}
