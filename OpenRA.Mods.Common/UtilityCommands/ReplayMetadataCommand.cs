#region Copyright & License Information
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

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ReplayMetadataCommand : IUtilityCommand
	{
		public string Name { get { return "--replay-metadata"; } }

		[Desc("REPLAYFILE", "Print the game metadata from a replay file.")]
		public void Run(ModData modData, string[] args)
		{
			var replay = ReplayMetadata.Read(args[1]);
			var info = replay.GameInfo;

			var lines = FieldSaver.Save(info).ToLines(replay.FilePath);
			foreach (var line in lines)
				Console.WriteLine(line);

			Console.WriteLine("\tPlayers:");
			var playerCount = 0;
			foreach (var p in info.Players)
			{
				var playerLines = FieldSaver.Save(p).ToLines("{0}".F(playerCount++));
				foreach (var line in playerLines)
					Console.WriteLine("\t\t" + line);
			}
		}
	}
}
