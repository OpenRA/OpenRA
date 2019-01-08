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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ChangeTakeOffSoundAndLandingSound : UpdateRule
	{
		public override string Name { get { return "Change 'TakeOffSound' and 'LandingSound' parameters within 'Aircraft' Trait."; } }
		public override string Description
		{
			get
			{
				return "The 'TakeOffSound' and 'LandingSound' parameters within 'Aircraft' have been changed\n" +
					"to accept an array of playable sounds.\n" +
					"They were renamed to 'TakeOffSounds' and 'LandingSounds' respectively, to reflect this change.\n" +
					"Definitions of 'TakeOffSound' and 'LandingSound' will be automatically renamed.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var aircraft in actorNode.ChildrenMatching("Aircraft"))
			{
				aircraft.RenameChildrenMatching("TakeOffSound", "TakeOffSounds");
				aircraft.RenameChildrenMatching("LandingSound", "LandingSounds");
			}

			yield break;
		}
	}
}
