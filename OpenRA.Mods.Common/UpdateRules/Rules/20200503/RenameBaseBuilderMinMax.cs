#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
	public class RenameBaseBuilderMinMax : UpdateRule
	{
		public override string Name => "Rename 'Min/MaxBaseRadius' to 'Minimum/MaximumBaseRadius'.";

		public override string Description => "BaseBuilderBotModule field names have been consolidated.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var rdc in actorNode.ChildrenMatching("BaseBuilderBotModule"))
				rdc.RenameChildrenMatching("MinBaseRadius", "MinimumBaseRadius");

			foreach (var rsc in actorNode.ChildrenMatching("BaseBuilderBotModule"))
				rsc.RenameChildrenMatching("MaxBaseRadius", "MaximumBaseRadius");

			yield break;
		}
	}
}
