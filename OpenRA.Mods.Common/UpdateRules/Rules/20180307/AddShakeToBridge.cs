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
	public class AddShakeToBridge : UpdateRule
	{
		public override string Name { get { return "Screen shaking was removed from dying bridges."; } }
		public override string Description
		{
			get
			{
				return "'Bridges' (and 'GroundLevelBridges') no longer shake the screen on their own.\n" +
					"The 'ShakeOnDeath' trait is to be used instead.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.ChildrenMatching("Bridge").Any())
				AddShakeNode(actorNode);
			else if (actorNode.ChildrenMatching("GroundLevelBridge").Any())
				AddShakeNode(actorNode);

			yield break;
		}

		void AddShakeNode(MiniYamlNode actorNode)
		{
			var shake = new MiniYamlNode("ShakeOnDeath", "");
			shake.AddNode("Duration", "15");
			shake.AddNode("Intensity", "6");
			actorNode.AddNode(shake);
		}
	}
}
