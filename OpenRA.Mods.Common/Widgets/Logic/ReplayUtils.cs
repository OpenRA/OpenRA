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
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class ReplayUtils
	{
		[FluentReference]
		const string IncompatibleReplayTitle = "dialog-incompatible-replay.title";

		[FluentReference]
		const string IncompatibleReplayPrompt = "dialog-incompatible-replay.prompt";

		[FluentReference]
		const string IncompatibleReplayAccept = "dialog-incompatible-replay.confirm";

		[FluentReference]
		const string UnknownVersion = "dialog-incompatible-replay.prompt-unknown-version";

		[FluentReference]
		const string UnknownMod = "dialog-incompatible-replay.prompt-unknown-mod";

		[FluentReference("mod")]
		const string UnvailableMod = "dialog-incompatible-replay.prompt-unavailable-mod";

		[FluentReference("version")]
		const string IncompatibleVersion = "dialog-incompatible-replay.prompt-incompatible-version";

		[FluentReference("map")]
		const string UnvailableMap = "dialog-incompatible-replay.prompt-unavailable-map";

		static readonly Action DoNothing = () => { };

		public static bool PromptConfirmReplayCompatibility(ReplayMetadata replayMeta, ModData modData, Action onCancel = null)
		{
			onCancel ??= DoNothing;

			if (replayMeta == null)
				return IncompatibleReplayDialog(modData, onCancel, IncompatibleReplayPrompt);

			var version = replayMeta.GameInfo.Version;
			if (version == null)
				return IncompatibleReplayDialog(modData, onCancel, UnknownVersion);

			var mod = replayMeta.GameInfo.Mod;
			if (mod == null)
				return IncompatibleReplayDialog(modData, onCancel, UnknownMod);

			if (!Game.Mods.ContainsKey(mod))
				return IncompatibleReplayDialog(modData, onCancel, UnvailableMod, "mod", mod);

			if (Game.Mods[mod].Metadata.Version != version)
				return IncompatibleReplayDialog(modData, onCancel, IncompatibleVersion, "version", version);

			if (replayMeta.GameInfo.MapPreview.Status != MapStatus.Available)
				return IncompatibleReplayDialog(modData, onCancel, UnvailableMap, "map", replayMeta.GameInfo.MapUid);

			return true;
		}

		static bool IncompatibleReplayDialog(ModData modData, Action onCancel, string text, params object[] args)
		{
			ConfirmationDialogs.ButtonPrompt(
				modData, IncompatibleReplayTitle, text, textArguments: args, onCancel: onCancel, cancelText: IncompatibleReplayAccept);
			return false;
		}
	}
}
