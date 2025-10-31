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

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class OutputResolvedRulesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--resolved-rules";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2 || args.Length == 3;
		}

		[Desc("ACTOR [PATH/TO/MAP]", "Display the finalized, merged MiniYaml rules for the given actor. Input values are case-sensitive.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.ModData is set.
			var modData = Game.ModData = utility.ModData;

			var key = args[1];
			var result = Utilities.GetTopLevelNodeByKey(modData, key,
				manifest => manifest.Rules,
				map => map.RuleDefinitions,
				args.Length == 3 ? args[2] : null);

			if (result == null)
			{
				Console.WriteLine("Could not find actor '{0}' (name is case-sensitive).", key);
				Environment.Exit(1);
			}

			Console.WriteLine(result.Value.Nodes.WriteToString());
		}
	}
}
