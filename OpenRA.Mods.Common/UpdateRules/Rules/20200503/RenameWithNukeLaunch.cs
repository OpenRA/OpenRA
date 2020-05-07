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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	class RenameWithNukeLaunch : UpdateRule
	{
		public override string Name { get { return "Renamed WithNukeLaunchAnimation and Overlay"; } }
		public override string Description
		{
			get
			{
				return "`WithNukeLaunchAnimation` has been renamed to `WithSupportPowerActivationAnimation` and `WithNukeLaunchOverlay` to `WithSupportPowerActivationOverlay`.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			actorNode.RenameChildrenMatching("WithNukeLaunchAnimation", "WithSupportPowerActivationAnimation", true);
			actorNode.RenameChildrenMatching("WithNukeLaunchOverlay", "WithSupportPowerActivationOverlay", true);

			yield break;
		}
	}
}
