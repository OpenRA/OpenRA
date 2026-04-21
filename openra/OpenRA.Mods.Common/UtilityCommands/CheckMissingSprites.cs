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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class CheckMissingSprites : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-missing-sprites";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Check tileset and sequence definitions for missing sprite files.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;
			var failed = false;

			// We need two levels of YamlException handling to provide the desired behaviour:
			// Parse errors within a single tileset should skip that tileset and allow the rest to be tested
			// however, certain errors will be thrown by the outer modData.DefaultSequences, which prevent
			// any tilesets from being checked further.
			try
			{
				foreach (var (tileset, terrainInfo) in modData.DefaultTerrainInfo)
				{
					try
					{
						Console.WriteLine("Tileset: " + tileset);
						if (terrainInfo is ITemplatedTerrainInfo templatedTerrainInfo)
							foreach (var ttr in modData.DefaultRules.Actors[SystemActors.World].TraitInfos<ITiledTerrainRendererInfo>())
								failed |= ttr.ValidateTileSprites(templatedTerrainInfo, Console.WriteLine);

						var sequences = new SequenceSet(modData.DefaultFileSystem, modData, tileset, null);
						sequences.SpriteCache.LoadReservations(modData);
						foreach (var (filename, location) in sequences.SpriteCache.MissingFiles)
						{
							Console.WriteLine($"\t{location}: {filename} not found");
							failed = true;
						}
					}
					catch (YamlException e)
					{
						// The stacktrace associated with yaml errors are not very useful
						// Suppress them to make the lint output less intimidating for modders
						Console.WriteLine($"\t{e.Message}");
						failed = true;
					}
					catch (Exception e)
					{
						Console.WriteLine($"Failed with exception: {e}");
						failed = true;
					}
				}
			}
			catch (YamlException e)
			{
				// The stacktrace associated with yaml errors are not very useful
				// Suppress them to make the lint output less intimidating for modders
				Console.WriteLine($"{e.Message}");
				failed = true;
			}

			if (failed)
				Environment.Exit(1);
		}
	}
}
