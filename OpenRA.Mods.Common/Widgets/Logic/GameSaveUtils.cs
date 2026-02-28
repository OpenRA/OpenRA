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
using System.Globalization;
using System.IO;
using OpenRA.Network;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class GameSaveUtils
	{
		[FluentReference]
		const string TooltipSavegameDateCreated = "tooltip-savegame-date-created";

		[FluentReference]
		const string TooltipSavegameMap = "tooltip-savegame-map";

		[FluentReference]
		const string TooltipSavegameDuration = "tooltip-savegame-duration";

		[FluentReference]
		const string TooltipSavegamePlayers = "tooltip-savegame-players";

		public static TimeSpan? GetGameDuration(GameSave save)
		{
			if (save == null || save.GlobalSettings.GameTimestep <= 0 || save.LastOrdersFrame < 0)
				return null;

			return TimeSpan.FromMilliseconds((long)save.LastOrdersFrame * save.GlobalSettings.GameTimestep);
		}

		public static string FormatGameDuration(TimeSpan? duration)
		{
			if (!duration.HasValue)
				return "?";

			var d = duration.Value;
			return $"{(int)d.TotalHours:D2}:{d.Minutes:D2}:{d.Seconds:D2}";
		}

		public static string BuildSaveTooltipText(string savePath, GameSave save, ModData modData)
		{
			var mapTitle = save != null ? modData.MapCache[save.GlobalSettings.Map].Title : null;
			var creationDate = File.GetCreationTime(savePath).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			var durationText = FormatGameDuration(GetGameDuration(save));
			var lines = new[]
			{
				$"{FluentProvider.GetMessage(TooltipSavegameDateCreated)}: {creationDate}",
				$"{FluentProvider.GetMessage(TooltipSavegameMap)}: {mapTitle ?? "?"}",
				$"{FluentProvider.GetMessage(TooltipSavegameDuration)}: {durationText}",
				$"{FluentProvider.GetMessage(TooltipSavegamePlayers)}: {save?.SlotClients.Count ?? 0}"
			};
			return string.Join("\n", lines);
		}

		public static bool IsValidNewSaveName(string newName, string initialName, string directory)
		{
			if (newName == initialName)
				return false;

			if (string.IsNullOrWhiteSpace(newName))
				return false;

			if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				return false;

			if (File.Exists(Path.Combine(directory, newName)))
				return false;

			return true;
		}
	}
}
