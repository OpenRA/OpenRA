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
	public class AddNukeLaunchAnimation : UpdateRule
	{
		public override string Name { get { return "Add 'WithNukeLaunchAnimation' and remove 'NukePower.ActivationSequence'"; } }
		public override string Description
		{
			get
			{
				return "The 'ActivationSequence' property has been removed.\n" +
					"Use the new 'WithNukeLaunchAnimation' trait instead.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var nukePowers = actorNode.ChildrenMatching("NukePower").ToList();
			foreach (var nuke in nukePowers)
			{
				var activation = nuke.LastChildMatching("ActivationSequence");
				if (activation == null)
					continue;

				var sequence = activation.NodeValue<string>();
				nuke.RemoveNode(activation);
				actorNode.AddNode("WithNukeLaunchAnimation", "");
				if (sequence != "active")
					actorNode.LastChildMatching("WithNukeLaunchAnimation").AddNode("Sequence", sequence);
			}

			yield break;
		}
	}
}
