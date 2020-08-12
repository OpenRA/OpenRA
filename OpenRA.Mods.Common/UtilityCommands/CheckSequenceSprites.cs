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
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CheckSequenceSprites : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--check-sequence-sprites"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Check the sequence definitions for missing sprite files.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;
			var failed = false;
			foreach (var kv in modData.DefaultSequences)
			{
				Console.WriteLine("Tileset: " + kv.Key);
				foreach (var image in kv.Value.Images)
				{
					foreach (var sequence in kv.Value.Sequences(image))
					{
						var s = kv.Value.GetSequence(image, sequence) as FileNotFoundSequence;
						if (s != null)
						{
							Console.WriteLine("\tSequence `{0}.{1}` references sprite `{2}` that does not exist.", image, sequence, s.Filename);
							failed = true;
						}
					}
				}
			}

			if (failed)
				Environment.Exit(1);
		}
	}
}
