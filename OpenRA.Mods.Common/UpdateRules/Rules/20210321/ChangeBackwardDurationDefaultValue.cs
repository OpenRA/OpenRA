#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	class ChangeBackwardDurationDefaultValue : UpdateRule
	{
		public override string Name => "BackwardDuration default value changed.";

		public override string Description => "BackwardDuration default value changed, old default value has to be defined in the rules now";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var mobile in actorNode.ChildrenMatching("mobile", includeRemovals: false))
			{
				var backwardDuration = mobile.LastChildMatching("BackwardDuration");
				if (backwardDuration != null)
					continue;

				var backwardDurationNode = new MiniYamlNode("BackwardDuration", FieldSaver.FormatValue(40));
				backwardDurationNode.AddNode(backwardDurationNode);
			}

			yield break;
		}
	}
}
