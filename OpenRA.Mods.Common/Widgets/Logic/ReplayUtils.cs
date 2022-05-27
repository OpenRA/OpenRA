#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class ReplayUtils
	{
		static readonly Action DoNothing = () => { };

		public static bool PromptConfirmReplayCompatibility(ReplayMetadata replayMeta, Action onCancel = null)
		{
			if (onCancel == null)
				onCancel = DoNothing;

			if (replayMeta == null)
			{
				ConfirmationDialogs.ButtonPrompt("Incompatible Replay", "Replay metadata could not be read.", onCancel: onCancel);
				return false;
			}

			var version = replayMeta.GameInfo.Version;
			if (version == null)
				return IncompatibleReplayDialog("unknown version", version, onCancel);

			var mod = replayMeta.GameInfo.Mod;
			if (mod == null)
				return IncompatibleReplayDialog("unknown mod", mod, onCancel);

			if (!Game.Mods.ContainsKey(mod))
				return IncompatibleReplayDialog("unavailable mod", mod, onCancel);

			if (Game.Mods[mod].Metadata.Version != version)
				return IncompatibleReplayDialog("incompatible version", version, onCancel);

			if (replayMeta.GameInfo.MapPreview.Status != MapStatus.Available)
				return IncompatibleReplayDialog("unavailable map", replayMeta.GameInfo.MapUid,  onCancel);

			return true;
		}

		static bool IncompatibleReplayDialog(string type, string name, Action onCancel)
		{
			var error = "It was recorded with an " + type;
			error += string.IsNullOrEmpty(name) ? "." : $":\n{name}";

			ConfirmationDialogs.ButtonPrompt("Incompatible Replay", error, onCancel: onCancel);

			return false;
		}
	}
}
