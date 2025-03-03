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
using System.IO;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Add this trait to the world actor to enable auto-save.")]
	public class AutoSaveInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AutoSave(init.Self, this); }
	}

	public class AutoSave : ITick
	{
		const string AutoSavePattern = "autosave-";
		const string SaveFileExtension = ".orasav";
		int ticksUntilAutoSave;
		int lastSaveInverval;
		readonly bool isDisabled;

		public AutoSave(Actor self, AutoSaveInfo info)
		{
			ticksUntilAutoSave = GetTicksBetweenAutosaves(self);
			lastSaveInverval = Game.Settings.SinglePlayerSettings.AutoSaveInterval;

			isDisabled = self.World.LobbyInfo.GlobalSettings.Dedicated || self.World.LobbyInfo.NonBotClients.Count() > 1;
		}

		void ITick.Tick(Actor self)
		{
			if (isDisabled || self.World.IsReplay || self.World.IsLoadingGameSave)
				return;

			var autoSaveValue = Game.Settings.SinglePlayerSettings.AutoSaveInterval;

			if (autoSaveValue == 0)
				return;

			var autoSaveFileLimit = Game.Settings.SinglePlayerSettings.AutoSaveMaxFileCount;

			autoSaveFileLimit = autoSaveFileLimit < 3 ? 3 : autoSaveFileLimit;

			if (lastSaveInverval != autoSaveValue)
			{
				lastSaveInverval = autoSaveValue;
				ticksUntilAutoSave = GetTicksBetweenAutosaves(self);
			}

			if (--ticksUntilAutoSave > 0)
				return;

			var oldAutoSaveFiles = GetAutoSaveFiles()
				.OrderByDescending(f => f.CreationTime)
				.Skip(autoSaveFileLimit - 1);

			foreach (var oldAutoSaveFile in oldAutoSaveFiles)
				oldAutoSaveFile.Delete();

			var dateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.InvariantCulture);
			var fileName = $"{AutoSavePattern}{dateTime}{SaveFileExtension}";
			self.World.RequestGameSave(fileName);
			ticksUntilAutoSave = GetTicksBetweenAutosaves(self);
		}

		static IEnumerable<FileSystemInfo> GetAutoSaveFiles()
		{
			var mod = Game.ModData.Manifest;

			var saveFolderPath = Path.Combine(Platform.SupportDir, "Saves", mod.Id, mod.Metadata.Version);

			var autoSaveDirectoryInfo = new DirectoryInfo(saveFolderPath);

			if (!autoSaveDirectoryInfo.Exists)
				return [];

			return autoSaveDirectoryInfo.EnumerateFiles($"{AutoSavePattern}*{SaveFileExtension}");
		}

		static int GetTicksBetweenAutosaves(Actor self)
		{
			return 1000 / self.World.Timestep * Game.Settings.SinglePlayerSettings.AutoSaveInterval;
		}
	}
}
