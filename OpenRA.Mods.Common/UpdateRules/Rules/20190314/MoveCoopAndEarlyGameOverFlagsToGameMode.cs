#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class MoveCoopAndEarlyGameOverFlagsToGameMode : UpdateRule
	{
		public override string Name { get { return "Move the 'Cooperative' and 'EarlyGameOver' flags to a GameMode trait."; } }
		public override string Description
		{
			get { return "The 'Cooperative' and 'EarlyGameOver' traitinfo flags have been moved\n"
				+ "from the MissionObjectives trait to the GameMode trait."; }
		}

		List<string> occurances = new List<string>();

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.Key != "Player")
				yield break;

			var mo = actorNode.LastChildMatching("MissionObjectives");
			if (mo != null)
			{
				if (mo.ChildrenMatching("Cooperative").Any() || mo.ChildrenMatching("EarlyGameOver").Any())
					occurances.Add("{0} ({1})".F(mo.Key, mo.Location.Filename));
			}
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!occurances.Any())
				yield break;

			yield return "Detected occurances of modified 'Cooperative' and/or\n" +
				"'EarlyGameOver' flags. Please move them manually to an appropriate\n" +
				"GameMode trait.\n" +
				UpdateUtils.FormatMessageList(occurances, 1);

			occurances.Clear();
		}
	}
}
