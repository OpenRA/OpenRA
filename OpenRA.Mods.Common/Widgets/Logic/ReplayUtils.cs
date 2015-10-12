﻿#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
				return IncompatibleReplayDialog("outdated engine", null, onCancel);

			var version = replayMeta.GameInfo.Version;
			if (version == null)
				return IncompatibleReplayDialog("unknown version", version, onCancel);

			var mod = replayMeta.GameInfo.Mod;
			if (mod == null)
				return IncompatibleReplayDialog("unknown mod", mod, onCancel);

			var allMods = ModMetadata.AllMods;
			if (!allMods.ContainsKey(mod))
				return IncompatibleReplayDialog("unavailable mod", mod, onCancel);
			else if (allMods[mod].Version != version)
				return IncompatibleReplayDialog("incompatible version", version, onCancel);

			if (replayMeta.GameInfo.MapPreview.Status != MapStatus.Available)
				return IncompatibleReplayDialog("unavailable map", replayMeta.GameInfo.MapUid,  onCancel);

			return true;
		}

		static bool IncompatibleReplayDialog(string type, string name, Action onCancel)
		{
			var error = "It was recorded with an " + type;
			error += string.IsNullOrEmpty(name) ? "." : ":\n{0}".F(name);

			ConfirmationDialogs.CancelPrompt("Incompatible Replay", error, onCancel);

			return false;
		}
	}
}