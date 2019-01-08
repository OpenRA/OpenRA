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
	public class ChangeIntensityToDuration : UpdateRule
	{
		public override string Name { get { return "Add a 'Duration' parameter to 'ShakeOnDeath'."; } }
		public override string Description
		{
			get
			{
				return "The 'Intensity' parameter on 'ShakeOnDeath' has been used as duration\n" +
					"by accident. A new 'Duration' parameter was added to fix that.\n" +
					"Definitions of 'Intensity' will be automatically renamed to 'Duration'.\n" +
					"The old 'Intensity' parameter will now change the intensity as intended.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var sod in actorNode.ChildrenMatching("ShakeOnDeath"))
				sod.RenameChildrenMatching("Intensity", "Duration");

			yield break;
		}
	}
}
